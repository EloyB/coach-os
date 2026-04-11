#!/usr/bin/env python3
"""
Padel Club Scenarios: 6 fields, Mon-Thu 18:00-21:00 (3 slots/day)
= 72 time slots, 4 per court = 288 total capacity
"""

import json
import random
import sys
import time
import requests

API = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5142/api"
random.seed(42)

DAYS_NL = {1: "Ma", 2: "Di", 3: "Wo", 4: "Do"}

NAMES = [
    "Emma Claes","Lucas Peeters","Lotte Van Damme","Noah Willems","Julie Maes",
    "Axel Dubois","Sarah Jacobs","Thomas Hermans","Marie Lambert","Bram Wouters",
    "Eline De Smet","Jef Mertens","Lisa Janssens","Daan Vermeer","Sophie Nijs",
    "Ruben Hendrickx","Eva Lemmens","Timo Jacobs","Nina Cools","Pieter Vos",
    "Charlotte Dries","Sam Pauwels","Luna Michiels","Wout Beyens","Hanne Geerts",
    "Milan Aerts","Roos Verhoeven","Stijn Vandenberghe","Amber Goossens","Jelle Maes",
    "Fien Brouwers","Lars Smeets","Noor Dierickx","Jesse Willems","Mila Van Hees",
    "Robin Peters","Lies Vandenbroucke","Sander Thijs","Maya Keunen","Tibo Claessens",
    "Anouk Pieters","Raf Devos","Lien Vermeiren","Koen Van Dyck","Jana Bogaert",
    "Brent Wouters","Silke Michiels","Yannick Peeters","Fleur De Graef","Matthias Berg",
    "Anna Stevens","Victor Nijs","Elise Vandamme","Gilles Cuypers","Nathalie Moons",
    "Dries Van Acker","Lauren De Backer","Quinten Smit","Jade Martens","Tim Claeys",
    "Sara Willems","Tom Hendrickx","Clara Maes","Joris Verbeke","Lana Pauwels",
    "Kevin Nuyts","Mieke Janssen","Niels Cools","Ines Geerts","Sven Aerts",
    "Katrien Dries","Robbe Michiels","Elien Goossens","Ward Vermeer","Celine Jacobs",
    "Dieter Mertens","Annelies Beyens","Yves Thijs","Jolien Peters","Dennis Smeets",
]

def api(method, path, body=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    for attempt in range(5):
        r = requests.request(method, f"{API}{path}", json=body, headers=headers)
        if r.status_code != 429:
            return r
        wait = 2 ** attempt
        time.sleep(wait)
    return r

# ── Setup ─────────────────────────────────────────────────────────────────────
print("\nSetting up...")
r = api("POST", "/auth/register", {
    "organizationName": "Padel Club Antwerpen",
    "firstName": "Coach", "lastName": "Admin",
    "email": "coach@padelclub.be", "password": "Padel2026!"
})
if r.status_code != 200:
    r = api("POST", "/auth/login", {"email": "coach@padelclub.be", "password": "Padel2026!"})
auth = r.json()
token = auth["token"]
user_id = auth["userId"]

r = api("POST", "/tennisclubs", {"name": "Padel Club Antwerpen", "address": "Padelstraat 10"}, token)
club_id = r.text.strip('"')

from datetime import date, timedelta, datetime
today = date.today().isoformat()
end_date = (date.today() + timedelta(days=90)).isoformat()
deadline = (datetime.utcnow() + timedelta(days=60)).strftime("%Y-%m-%dT%H:%M:%SZ")

print(f"   Authenticated. Club: Padel Club Antwerpen\n")

def build_template():
    slots = []
    for day in [1, 2, 3, 4]:
        for hour in [18, 19, 20]:
            for field in range(1, 3):
                slots.append({
                    "dayOfWeek": day,
                    "startTime": f"{hour}:00",
                    "endTime": f"{hour+1}:00",
                    "trainerId": user_id,
                    "courtName": f"Veld {field}",
                    "maxStudents": 4,
                })
    return slots

def create_series(name):
    r = api("POST", "/lessonseries", {
        "tennisClubId": club_id,
        "name": name,
        "description": "6 velden, ma-do 18-21u",
        "level": 1, "price": 200.0,
        "startDate": today, "endDate": end_date,
        "registrationDeadline": deadline,
        "maxRegistrations": 300,
        "weeklyTemplate": build_template(),
        "lessons": [{"trainerId": user_id, "date": today, "startTime": "18:00", "endTime": "19:00", "courtName": "Veld 1", "maxStudents": 4}],
    }, token)
    if r.status_code not in (200, 201):
        print(f"   ERROR creating series: {r.status_code} {r.text[:200]}")
        sys.exit(1)
    return r.text.strip('"')

def fetch_slots(series_id):
    r = api("GET", f"/public/lessonseries/{series_id}/timeslots")
    return r.json()

def enroll(series_id, name, email, prefs, enroll_type="solo", open_to_grouping=False, group_members=None):
    body = {
        "studentName": name, "studentEmail": email, "responses": [],
        "enrollmentType": enroll_type, "isOpenToGrouping": open_to_grouping,
        "timeSlotPreferences": prefs,
    }
    if group_members:
        body["groupMembers"] = group_members
    r = api("POST", f"/public/lessonseries/{series_id}/enroll", body)
    return r.status_code == 200

def generate_and_show(series_id, scenario_name):
    r = api("POST", f"/lessonseries/{series_id}/planning/generate", token=token)
    if r.status_code != 200:
        print(f"   Generate failed: {r.text[:200]}")
        return
    data = r.json()

    print(f"   Planning status: {data['planningStatus']}\n")

    by_time = {}
    for slot in data["slots"]:
        key = (slot["dayOfWeek"], slot["startTime"])
        by_time.setdefault(key, []).append(slot)

    total_assigned = 0
    total_cap = 0
    active_fields = 0

    for key in sorted(by_time.keys()):
        day, start_time = key
        slots = by_time[key]
        end_time = slots[0]["endTime"]

        filled = []
        empty = 0
        for s in slots:
            cap = s["maxCapacity"]
            total_cap += cap
            count = s["currentCount"]
            total_assigned += count
            if count > 0:
                active_fields += 1
                names_parts = []
                for a in s["assignments"]:
                    tag = "G" if a.get("groupId") else " "
                    names_parts.append(f"[{tag}] " + ", ".join(a["studentNames"]))
                bar = "█" * count + "░" * (cap - count)
                filled.append(f'{s["courtName"]} [{bar}] {count}/{cap}: {" | ".join(names_parts)}')
            else:
                empty += 1

        print(f'   {DAYS_NL.get(day, "?")} {start_time}-{end_time} ({len(slots)} velden)')
        for f in filled:
            print(f"      {f}")
        if empty > 0:
            print(f'      ({empty} veld{"en" if empty > 1 else ""} leeg)')
        print()

    unassigned = data.get("unassigned", [])
    print(f"   ═══════════════════════════════════════")
    print(f"   Toegewezen: {total_assigned}/{total_cap} plaatsen")
    print(f"   Actieve velden: {active_fields}/{len(data['slots'])}")
    if unassigned:
        print(f"   ⚠️  Niet toegewezen: {len(unassigned)} personen")
        for u in unassigned[:15]:
            print(f'      - {u["studentName"]}')
        if len(unassigned) > 15:
            print(f"      ... en nog {len(unassigned) - 15} anderen")
    else:
        print("   ✅ Iedereen is toegewezen!")
    print()


def build_prefs(slots, slot_map, time_keys, preference_fn):
    prefs = []
    for key in time_keys:
        for sid in slot_map[key]:
            p = preference_fn(key, sid)
            prefs.append({"weeklyTemplateEntryId": sid, "preference": p})
    return prefs


# ══════════════════════════════════════════════════════════════════════════════
def run_scenario(name, description, enroll_fn):
    print(f"╔{'═'*62}╗")
    print(f"║  {name:<60}║")
    print(f"║  {description:<60}║")
    print(f"╚{'═'*62}╝\n")

    sid = create_series(name)
    print(f"   Series: {sid}")

    slots = fetch_slots(sid)
    print(f"   Slots: {len(slots)}")

    slot_map = {}
    for s in slots:
        key = (s["dayOfWeek"], s["startTime"])
        slot_map.setdefault(key, []).append(s["id"])
    time_keys = sorted(slot_map.keys())

    ok, fail, total_people = enroll_fn(sid, slots, slot_map, time_keys)
    print(f"   Enrolled: {ok} ok, {fail} failed ({total_people} people total)")
    print(f"   Capacity: {len(slots)} slots × 4 = {len(slots) * 4} plaatsen\n")

    generate_and_show(sid, name)


# ── SCENARIO 1: PERFECT ──────────────────────────────────────────────────────
def scenario_perfect(sid, slots, slot_map, time_keys):
    ok = fail = 0
    for i in range(20):
        primary = time_keys[i % len(time_keys)]
        secondary = time_keys[(i + 4) % len(time_keys)]

        def pref_fn(key, _sid, p=primary, s=secondary):
            if key == p: return 2
            if key == s: return 1
            return 3

        prefs = build_prefs(slots, slot_map, time_keys, pref_fn)
        if enroll(sid, NAMES[i % len(NAMES)], f"s{i}@perfect.be", prefs, open_to_grouping=True):
            ok += 1
        else:
            fail += 1
    return ok, fail, 60

run_scenario("SCENARIO 1: PERFECT FIT", "20 spelers, goed verdeeld over alle momenten", scenario_perfect)


# ── SCENARIO 2: OVERSUBSCRIBED ────────────────────────────────────────────────
def scenario_oversubscribed(sid, slots, slot_map, time_keys):
    ok = fail = 0
    popular = [(1, "19:00"), (2, "19:00")]

    for i in range(20):
        if i < 14:
            def pref_fn(key, _sid, pop=popular):
                if key in pop: return 2
                if key[1] == "19:00": return 1
                return 3
        else:
            def pref_fn(key, _sid):
                return random.choice([1, 1, 2])

        prefs = build_prefs(slots, slot_map, time_keys, pref_fn)
        if enroll(sid, NAMES[i % len(NAMES)], f"s{i}@oversub.be", prefs, open_to_grouping=(i < 14)):
            ok += 1
        else:
            fail += 1
    return ok, fail, 20

run_scenario("SCENARIO 2: OVERSUBSCRIBED", "20 spelers, 14 willen allemaal ma/di 19:00", scenario_oversubscribed)


# ── SCENARIO 3: SPARSE ───────────────────────────────────────────────────────
def scenario_sparse(sid, slots, slot_map, time_keys):
    ok = fail = 0
    total = 0

    for i in range(5):
        pref_day = random.choice([1, 2, 3, 4])
        pref_hour = random.choice(["18:00", "19:00", "20:00"])

        def pref_fn(key, _sid, pd=pref_day, ph=pref_hour):
            if key == (pd, ph): return 2
            if key[0] == pd: return 1
            return 3

        prefs = build_prefs(slots, slot_map, time_keys, pref_fn)
        if enroll(sid, NAMES[i], f"s{i}@sparse.be", prefs):
            ok += 1
        else:
            fail += 1
        total += 1

    # Group of 3 on Wednesday 19:00
    def pref_fn(key, _sid):
        if key == (3, "19:00"): return 2
        if key[0] == 3: return 1
        return 3

    prefs = build_prefs(slots, slot_map, time_keys, pref_fn)
    members = [
        {"studentName": NAMES[11], "studentEmail": "s11@sparse.be"},
        {"studentName": NAMES[12], "studentEmail": "s12@sparse.be"},
    ]
    if enroll(sid, NAMES[10], "s10@sparse.be", prefs, "group", group_members=members):
        ok += 1
    else:
        fail += 1
    total += 3

    return ok, fail, total

run_scenario("SCENARIO 3: SPARSE", "8 spelers (5 solo + 1 groep van 3), veel lege velden  ", scenario_sparse)


# ── SCENARIO 4: RANDOM ───────────────────────────────────────────────────────
def scenario_random(sid, slots, slot_map, time_keys):
    ok = fail = 0
    total = 0

    for i in range(15):
        def pref_fn(key, _sid):
            return random.choices([1, 2, 3], weights=[30, 30, 40])[0]

        prefs = build_prefs(slots, slot_map, time_keys, pref_fn)
        if enroll(sid, NAMES[i], f"s{i}@random.be", prefs, open_to_grouping=random.random() > 0.5):
            ok += 1
        else:
            fail += 1
        total += 1

    # 1 group of 4
    for g in range(1):
        idx = 25 + g * 4
        def pref_fn(key, _sid):
            return random.choices([1, 2, 3], weights=[25, 35, 40])[0]

        prefs = build_prefs(slots, slot_map, time_keys, pref_fn)
        members = [
            {"studentName": NAMES[(idx+j) % len(NAMES)], "studentEmail": f"s{idx+j}@random.be"}
            for j in range(1, 4)
        ]
        if enroll(sid, NAMES[idx % len(NAMES)], f"s{idx}@random.be", prefs, "group", group_members=members):
            ok += 1
        else:
            fail += 1
        total += 4

    return ok, fail, total

run_scenario("SCENARIO 4: RANDOM", "19 spelers (15 solo + 1 groep van 4), random voorkeuren", scenario_random)


# ── SCENARIO 5: STAMPVOL — everything full, auto-groups, overflow ─────────────
def scenario_stampvol(sid, slots, slot_map, time_keys):
    """
    2 fields × 4 days × 3 hours = 24 slots × 4 = 96 capacity.
    We enroll 105 people: 1 pre-formed group of 3 + 25 open solos + 2 closed solos = 30 enrollments = 105 people... no.
    Actually: 1 group of 3, plus enough solos to overflow.
    24 slots × 4 = 96 spots. Enroll 105 people (102 solo + 1 group of 3).
    Most are open to grouping so auto-grouping kicks in.
    Everyone wants only 2 of the 4 days → guaranteed overflow.
    """
    ok = fail = 0
    total = 0

    # 1 pre-formed group of 3, only wants Monday evening
    def pref_fn_group(key, _sid):
        if key[0] == 1: return 2   # Monday preferred
        return 3                    # everything else unavailable

    prefs = build_prefs(slots, slot_map, time_keys, pref_fn_group)
    members = [
        {"studentName": "Liam Dubois", "studentEmail": "liam@stampvol.be"},
        {"studentName": "Nora Dubois", "studentEmail": "nora@stampvol.be"},
    ]
    if enroll(sid, "Papa Dubois", "papa@stampvol.be", prefs, "group", group_members=members):
        ok += 1
    else:
        fail += 1
    total += 3

    # 27 solos who ONLY want Monday or Tuesday (12 slots × 4 = 48 spots, but 27+3=30 people)
    for i in range(27):
        def pref_fn(key, _sid):
            if key[0] in (1, 2):  # Mon or Tue
                if key[1] == "19:00": return 2  # everyone prefers 19:00
                return 1
            return 3  # Wed/Thu unavailable

        prefs = build_prefs(slots, slot_map, time_keys, pref_fn)
        if enroll(sid, NAMES[i], f"s{i}@stampvol.be", prefs, open_to_grouping=True):
            ok += 1
        else:
            fail += 1
        total += 1

    # 20 solos who ONLY want Wednesday or Thursday (same pattern — 48 spots, 20 people)
    for i in range(20):
        def pref_fn(key, _sid):
            if key[0] in (3, 4):  # Wed or Thu
                if key[1] == "20:00": return 2  # prefer 20:00
                return 1
            return 3  # Mon/Tue unavailable

        prefs = build_prefs(slots, slot_map, time_keys, pref_fn)
        if enroll(sid, NAMES[30 + i], f"s{30+i}@stampvol.be", prefs, open_to_grouping=True):
            ok += 1
        else:
            fail += 1
        total += 1

    # 8 solos who ONLY want Monday 19:00 (4 spots on 2 fields = 8 spots, but group already took 3)
    # → at most 5 fit, 3 should overflow
    for i in range(8):
        def pref_fn(key, _sid):
            if key == (1, "19:00"): return 2
            return 3  # everything else unavailable

        prefs = build_prefs(slots, slot_map, time_keys, pref_fn)
        if enroll(sid, NAMES[60 + i], f"s{60+i}@stampvol.be", prefs, open_to_grouping=False):
            ok += 1
        else:
            fail += 1
        total += 1

    return ok, fail, total

run_scenario(
    "SCENARIO 5: STAMPVOL",
    "58 spelers, iedereen wil ma/di of wo/do, 8 willen enkel ma 19:00",
    scenario_stampvol
)
