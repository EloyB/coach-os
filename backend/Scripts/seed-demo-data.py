#!/usr/bin/env python3
"""
Seeds a demo environment with realistic data via the CoachOS API.

Reads seed-data.json (co-located) and POSTs to the API. Zero external
dependencies — stdlib only.

Usage:
    python3 seed-demo-data.py [API_BASE]

Default API_BASE is http://localhost:5142/api.
"""
from __future__ import annotations

import json
import sys
import time
import urllib.error
import urllib.request
from dataclasses import dataclass
from datetime import date, datetime, timedelta
from pathlib import Path
from typing import Any


DEFAULT_API_BASE = "http://localhost:5142/api"
SCRIPT_DIR = Path(__file__).resolve().parent
DATA_FILE = SCRIPT_DIR / "seed-data.json"


# ── HTTP ─────────────────────────────────────────────────────────────────────

@dataclass
class ApiClient:
    base: str
    token: str | None = None

    def post(self, path: str, body: dict[str, Any] | list[Any] | None = None,
             auth: bool = True) -> Any:
        return self._request("POST", path, body, auth)

    def put(self, path: str, body: dict[str, Any] | list[Any] | None = None,
            auth: bool = True) -> Any:
        return self._request("PUT", path, body, auth)

    def get(self, path: str, auth: bool = True) -> Any:
        return self._request("GET", path, None, auth)

    def _request(self, method: str, path: str,
                 body: dict | list | None, auth: bool) -> Any:
        headers = {"Content-Type": "application/json"}
        if auth and self.token:
            headers["Authorization"] = f"Bearer {self.token}"

        data = json.dumps(body).encode("utf-8") if body is not None else None

        for attempt in range(3):
            req = urllib.request.Request(
                f"{self.base}{path}", data=data, method=method, headers=headers)
            try:
                with urllib.request.urlopen(req, timeout=30) as resp:
                    raw = resp.read().decode("utf-8")
                    return json.loads(raw) if raw else None
            except urllib.error.HTTPError as e:
                if e.code == 429 and attempt < 2:
                    print(f"  Rate limited on {method} {path} "
                          f"— waiting 62s before retry {attempt + 2}/3...",
                          file=sys.stderr)
                    time.sleep(62)
                    continue
                err_body = e.read().decode("utf-8", errors="replace")
                print(f"  ERROR {e.code} on {method} {path}: {err_body}",
                      file=sys.stderr)
                return None
            except urllib.error.URLError as e:
                print(f"  ERROR on {method} {path}: {e.reason}", file=sys.stderr)
                return None
        return None


# ── Date helpers ─────────────────────────────────────────────────────────────

def add_months(d: date, months: int) -> date:
    """Naive month arithmetic that clamps to end-of-month."""
    month_total = d.month - 1 + months
    year = d.year + month_total // 12
    month = month_total % 12 + 1
    # clamp day to month length
    for day in (d.day, 30, 29, 28):
        try:
            return date(year, month, day)
        except ValueError:
            continue
    return date(year, month, 28)


def iso_date(d: date) -> str:
    return d.strftime("%Y-%m-%d")


def iso_utc(dt: datetime) -> str:
    return dt.strftime("%Y-%m-%dT%H:%M:%SZ")


# ── Domain helpers ───────────────────────────────────────────────────────────

def strip_quotes(value: Any) -> str | None:
    """API endpoints that return a bare GUID come back as a JSON string."""
    if value is None:
        return None
    if isinstance(value, str):
        return value.strip('"') or None
    return None


def generate_lessons_from_template(template: list[dict], start: date, end: date,
                                   trainer_id: str) -> list[dict]:
    """Expand a weekly template across [start, end] inclusive."""
    lessons: list[dict] = []
    week_start = start - timedelta(days=start.weekday())
    current = week_start
    while current <= end:
        for slot in template:
            lesson_date = current + timedelta(days=slot["dayOfWeek"])
            if lesson_date < start or lesson_date > end:
                continue
            lessons.append({
                "trainerId": trainer_id,
                "date": iso_date(lesson_date),
                "startTime": slot["startTime"],
                "endTime": slot["endTime"],
                "courtName": slot["courtName"],
                "maxStudents": slot["maxStudents"],
            })
        current += timedelta(days=7)
    return lessons


def template_with_trainer(template: list[dict], trainer_id: str | None) -> list[dict]:
    return [{**slot, "trainerId": trainer_id} for slot in template]


# ── Seed steps ───────────────────────────────────────────────────────────────

def authenticate(api: ApiClient, admin: dict) -> dict | None:
    print("1. Registering admin user...")
    auth = api.post("/auth/register", admin, auth=False)

    if not auth or not auth.get("token"):
        print("   Registration failed - user may already exist. Trying login...")
        auth = api.post("/auth/login", {
            "email": admin["email"], "password": admin["password"]}, auth=False)

    if not auth or not auth.get("token"):
        print("   Cannot authenticate. Exiting.", file=sys.stderr)
        return None

    api.token = auth["token"]
    print(f"   OK - Logged in as {auth.get('firstName')} {auth.get('lastName')}")
    return auth


def create_clubs(api: ApiClient, clubs: list[dict]) -> list[str]:
    print("\n2. Creating tennis clubs...")
    ids: list[str] = []
    for club in clubs:
        cid = strip_quotes(api.post("/tennisclubs", club))
        if cid:
            ids.append(cid)
            print(f"   Created: {club['name']}")
    return ids


def invite_trainers_and_pick_id(api: ApiClient, trainers: list[dict],
                                fallback_user_id: str) -> str:
    print("\n3. Inviting trainers...")
    for t in trainers:
        api.post("/trainers/invite", t)
        print(f"   Invited: {t['firstName']} {t['lastName']}")

    trainer_list = api.get("/trainers") or []
    active = [t for t in trainer_list if t.get("isActive")]
    print(f"   Active trainers: {len(active)}")
    return active[0]["id"] if active else fallback_user_id


def create_simple_series(api: ApiClient, series_specs: list[dict],
                         club_ids: list[str], trainer_id: str,
                         today: date, deadline_iso: str) -> list[str]:
    print("\n4. Creating lesson series (with full lesson schedules)...")
    if not club_ids:
        print("   No clubs created - skipping series.")
        return []

    ids: list[str] = []
    for spec in series_specs:
        club_idx = min(spec["clubIndex"], len(club_ids) - 1)
        start = today + timedelta(days=spec["startOffsetDays"])
        end = add_months(today, spec["endOffsetMonths"])
        template = template_with_trainer(spec["weeklyTemplate"], trainer_id)
        lessons = generate_lessons_from_template(spec["weeklyTemplate"], start, end, trainer_id)

        body = {
            "trainerId": trainer_id,
            "tennisClubId": club_ids[club_idx],
            "name": spec["name"],
            "description": spec["description"],
            "level": spec["level"],
            "price": spec["price"],
            "startDate": iso_date(start),
            "endDate": iso_date(end),
            "registrationDeadline": deadline_iso,
            "maxRegistrations": spec["maxRegistrations"],
            "weeklyTemplate": template,
            "lessons": lessons,
        }
        sid = strip_quotes(api.post("/lessonseries", body))
        if sid:
            ids.append(sid)
            print(f"   Created: {spec['name']} ({len(lessons)} lessons)")
    return ids


def simple_enrollments(api: ApiClient, students: list[dict],
                       series_ids: list[str]) -> None:
    print("\n6. Creating enrollments...")
    if not series_ids:
        print("   No series created - skipping enrollments.")
        return
    count = 0
    for student in students:
        target = series_ids[student["seriesIndex"] % len(series_ids)]
        result = api.post(
            f"/public/lessonseries/{target}/enroll",
            {
                "studentName":  student["studentName"],
                "studentEmail": student["studentEmail"],
                "studentPhone": student["studentPhone"],
                "responses":    [],
            },
            auth=False,
        )
        if result:
            count += 1
    print(f"   Created {count} enrollments")


def create_standalone_lessons(api: ApiClient, specs: list[dict],
                              trainer_id: str, today: date) -> None:
    """Creates losse lessen + uitnodigingen via /standalone-lessons."""
    print("\n9. Creating standalone lessons...")
    if not specs:
        print("   None configured - skipping.")
        return
    created = 0
    for spec in specs:
        body = {
            "date": iso_date(today + timedelta(days=spec["startOffsetDays"])),
            "startTime": spec["startTime"],
            "durationMinutes": spec["durationMinutes"],
            "courtName": spec["courtName"],
            "level": spec.get("level"),
            "trainerId": trainer_id,
            "maxParticipants": spec["maxParticipants"],
            "notes": spec.get("notes"),
            "participantEmails": spec["participantEmails"],
        }
        result = api.post("/standalone-lessons", body)
        if result is not None:
            created += 1
            label = spec.get("label") or "(unnamed)"
            print(f"   Created: {label} ({len(spec['participantEmails'])} invites)")
    print(f"   Total: {created}/{len(specs)} standalone lessons")


def create_planning_series(api: ApiClient, spec: dict, club_ids: list[str],
                           trainer_id: str, today: date,
                           deadline_iso: str) -> str | None:
    print(f"\n7. Creating {spec['name']}...")
    if not club_ids:
        print("   No clubs — skipping planning series.")
        return None

    club_idx = min(spec["clubIndex"], len(club_ids) - 1)
    start = today + timedelta(days=spec["startOffsetDays"])
    end = add_months(today, spec["endOffsetMonths"])

    # Planning series: weekly template has trainerId=null (demonstrates unassigned slots).
    template_unassigned = template_with_trainer(spec["weeklyTemplate"], None)
    lessons = generate_lessons_from_template(
        spec["weeklyTemplate"], start, end, trainer_id)

    body = {
        "tennisClubId": club_ids[club_idx],
        "name": spec["name"],
        "description": spec["description"],
        "level": spec["level"],
        "price": spec["price"],
        "startDate": iso_date(start),
        "endDate": iso_date(end),
        "registrationDeadline": deadline_iso,
        "maxRegistrations": spec["maxRegistrations"],
        "weeklyTemplate": template_unassigned,
        "lessons": lessons,
    }
    sid = strip_quotes(api.post("/lessonseries", body))
    if not sid:
        print("   Failed to create planning series.")
        return None
    print(f"   Created: {spec['name']} ({sid})")
    return sid


def setup_second_org(api_base: str, cfg: dict) -> None:
    """Registreert een tweede organisatie met eigen admin en nodigt de eerste
    admin uit als trainer in die nieuwe org. Omdat die user al globaal actief
    is, krijgt hij meteen een actief membership — handig om de org-switcher
    in de FE meteen te kunnen testen na ./reset.sh."""
    print("\n10. Setting up second org for multi-org demo...")
    second_api = ApiClient(api_base)
    auth = second_api.post("/auth/register", cfg["admin"], auth=False)
    if not auth or not auth.get("token"):
        # Already exists from a previous seed run — just log in.
        auth = second_api.post("/auth/login", {
            "email": cfg["admin"]["email"],
            "password": cfg["admin"]["password"],
        }, auth=False)
    if not auth or not auth.get("token"):
        print("   Could not register/login second admin — skipping.")
        return
    second_api.token = auth["token"]
    print(f"   Created org: {cfg['admin']['organizationName']} "
          f"(admin: {cfg['admin']['email']})")

    invite = cfg["inviteExistingTrainer"]
    second_api.post("/trainers/invite", invite)
    print(f"   Invited existing trainer {invite['email']} - direct membership")


def generate_and_confirm_planning(api: ApiClient, planning_series_id: str) -> None:
    """Run the admin planning flow so ScheduleAssignments exist for the demo.
    Produces assignments in AwaitingConfirmation state (each student has a
    per-assignment confirmation token they can act on — or simply view via
    the student portal)."""
    print("\n9. Generating planning proposal...")
    proposal = api.post(
        f"/lessonseries/{planning_series_id}/planning/generate?force=true",
        {},
    )
    if proposal is None:
        print("   Failed to generate planning proposal.")
        return

    print("   Confirming planning (locks schedule, creates student confirmation tokens)...")
    confirmed = api.post(f"/lessonseries/{planning_series_id}/planning/confirm", {})
    if confirmed is None:
        print("   WARNING: confirm returned an error (see stderr).", file=sys.stderr)
    else:
        print("   Done.")


def planning_enrollments(api: ApiClient, planning_series_id: str,
                         enrollments: list[dict]) -> None:
    print("   Fetching time slots...")
    slots = api.get(f"/public/lessonseries/{planning_series_id}/timeslots",
                    auth=False) or []
    slots.sort(key=lambda s: (s["dayOfWeek"], s["startTime"], s.get("courtName") or ""))
    slot_ids = [s["id"] for s in slots]
    print(f"   Found {len(slot_ids)} time slots")

    print("\n8. Creating planning enrollments...")
    for e in enrollments:
        prefs = e.get("slotPreferences", [])
        time_slot_prefs = [
            {"weeklyTemplateEntryId": slot_ids[i], "preference": pref}
            for i, pref in enumerate(prefs)
            if i < len(slot_ids)
        ]
        body = {
            "studentName":  e["studentName"],
            "studentEmail": e["studentEmail"],
            "studentPhone": e.get("studentPhone"),
            "enrollmentType": e.get("enrollmentType", "solo"),
            "isOpenToGrouping": e.get("isOpenToGrouping", False),
            "timeSlotPreferences": time_slot_prefs,
            "responses": [],
        }
        if e.get("groupMembers"):
            body["groupMembers"] = [
                {**m, "responses": []} for m in e["groupMembers"]
            ]
        result = api.post(f"/public/lessonseries/{planning_series_id}/enroll",
                          body, auth=False)
        label = e.get("label") or e["studentName"]
        member_count = len(body.get("groupMembers", []))
        students = member_count + 1 if member_count else 1
        if result is not None:
            print(f"   OK  {label} ({students} student(s))")
        else:
            print(f"   ERR {label} — enrollment failed (see stderr)", file=sys.stderr)


def create_camps(api: ApiClient, club_ids: list[str], trainer_id: str,
                 today: date, deadline_iso: str) -> list[str]:
    """Creates demo camps (one paid multi-day with per-day trainers + a custom
    form field, one free) and a handful of public enrollments (solo + group).
    Paid-camp enrollments stay PendingPayment (no real Mollie in seed); free-camp
    enrollments come back Confirmed. Emails are distinct per participant because
    each camp has a unique (CampId, ParticipantEmail) active index."""
    print("\n11. Creating camps...")
    if not club_ids:
        print("   No clubs created - skipping camps.")
        return []

    camp_ids: list[str] = []

    # ── Paid camp: 3 consecutive days, day-trainers, custom form ──────────────
    paid_start = today + timedelta(days=21)
    paid_body = {
        "name": "Paaskamp Gevorderden",
        "description": "Drie dagen intensief trainen tijdens de paasvakantie.",
        "tennisClubId": club_ids[0],
        "level": 3,
        "price": 120,
        "startDate": iso_date(paid_start),
        "endDate": iso_date(paid_start + timedelta(days=2)),
        "registrationDeadline": deadline_iso,
        "maxParticipants": 20,
        "days": [
            {"date": iso_date(paid_start), "startTime": "09:00", "endTime": "16:00",
             "trainers": [{"trainerId": trainer_id, "startTime": "09:00", "endTime": "12:00"}]},
            {"date": iso_date(paid_start + timedelta(days=1)), "startTime": "09:00", "endTime": "16:00",
             "trainers": []},
            {"date": iso_date(paid_start + timedelta(days=2)), "startTime": "10:00", "endTime": "15:00",
             "trainers": []},
        ],
    }
    paid_id = strip_quotes(api.post("/camps", paid_body))
    if paid_id:
        camp_ids.append(paid_id)
        print(f"   Created paid camp: {paid_body['name']} ({len(paid_body['days'])} days)")

        # Custom enrollment form with one extra (optional) field.
        form_result = api.put(
            f"/camps/{paid_id}/form",
            {"fields": [
                {"label": "Allergieen", "type": 1, "isRequired": False, "order": 0},
            ]},
        )
        if form_result is not None:
            print("   Added custom form field to paid camp")
        else:
            print("   WARNING: could not set paid-camp form (see stderr).",
                  file=sys.stderr)
    else:
        print("   WARNING: paid camp creation failed (see stderr).", file=sys.stderr)

    # ── Free camp: 2 days, no day-trainers required ──────────────────────────
    free_start = today + timedelta(days=35)
    free_body = {
        "name": "Gratis Padel Proefkamp",
        "description": "Twee dagen gratis kennismaken met padel.",
        "tennisClubId": club_ids[0],
        "level": 1,
        "price": 0,
        "startDate": iso_date(free_start),
        "endDate": iso_date(free_start + timedelta(days=1)),
        "registrationDeadline": deadline_iso,
        "maxParticipants": 16,
        "days": [
            {"date": iso_date(free_start), "startTime": "13:00", "endTime": "17:00",
             "trainers": [{"trainerId": trainer_id, "startTime": "13:00", "endTime": "17:00"}]},
            {"date": iso_date(free_start + timedelta(days=1)), "startTime": "13:00", "endTime": "17:00",
             "trainers": []},
        ],
    }
    free_id = strip_quotes(api.post("/camps", free_body))
    if free_id:
        camp_ids.append(free_id)
        print(f"   Created free camp: {free_body['name']} ({len(free_body['days'])} days)")
    else:
        print("   WARNING: free camp creation failed (see stderr).", file=sys.stderr)

    # ── Public enrollments (no auth) ─────────────────────────────────────────
    print("\n12. Creating camp enrollments...")

    def enroll(camp_id: str, label: str, body: dict) -> None:
        result = api.post(f"/public/camps/{camp_id}/enroll", body, auth=False)
        if result is not None:
            print(f"   OK  {label}")
        else:
            print(f"   ERR {label} - camp enrollment failed (see stderr)",
                  file=sys.stderr)

    # Betaalde camp: inschrijven triggert direct een Mollie-betaling. In de seed
    # is er geen Mollie-koppeling, dus inschrijvingen zouden falen en orphan
    # PendingPayment-rijen achterlaten. De betaalde camp wordt wel aangemaakt
    # (voor de beheer-UI demo); inschrijvingen seeden we enkel op het gratis kamp.

    if free_id:
        enroll(free_id, "Gratis kamp - Noor (solo)", {
            "participantName": "Noor Janssens",
            "participantEmail": "noor.janssens@example.com",
            "participantPhone": "+32470777888",
            "responses": [],
            "enrollmentType": "solo",
        })
        enroll(free_id, "Gratis kamp - Milan (solo)", {
            "participantName": "Milan Claes",
            "participantEmail": "milan.claes@example.com",
            "participantPhone": "+32470999000",
            "responses": [],
            "enrollmentType": "solo",
        })

    return camp_ids


# ── Main ─────────────────────────────────────────────────────────────────────

def main() -> int:
    api_base = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_API_BASE
    data = json.loads(DATA_FILE.read_text(encoding="utf-8"))

    print("\n=== CoachOS Demo Seed ===")
    print(f"API: {api_base}\n")

    api = ApiClient(api_base)

    # Bootstrap super admin via dev-only endpoint (alleen beschikbaar in Development).
    # Idempotent: als de user al bestaat wordt enkel de IsSuperAdmin flag gezet.
    if "superAdmin" in data:
        sa = data["superAdmin"]
        print("0. Bootstrapping super admin...")
        result = api.post("/dev/super-admin/bootstrap", sa, auth=False)
        if result is None:
            print("   [!] Super admin bootstrap mislukt (dev-only endpoint niet bereikbaar?).")
        else:
            print(f"   [OK] Super admin: {sa['email']}")

    auth = authenticate(api, data["admin"])
    if auth is None:
        return 1

    today = date.today()
    deadline_iso = iso_utc(datetime.combine(add_months(today, 3),
                                            datetime.min.time()))

    club_ids = create_clubs(api, data["clubs"])
    trainer_id = invite_trainers_and_pick_id(
        api, data["trainers"], auth["userId"])

    simple_ids = create_simple_series(
        api, data["simpleSeries"], club_ids, trainer_id, today, deadline_iso)
    simple_enrollments(api, data["simpleEnrollments"], simple_ids)

    planning_id = create_planning_series(
        api, data["planningSeries"], club_ids, trainer_id, today, deadline_iso)
    if planning_id:
        planning_enrollments(api, planning_id, data["planningEnrollments"])
        generate_and_confirm_planning(api, planning_id)

    create_standalone_lessons(
        api, data.get("standaloneLessons", []), trainer_id, today)

    create_camps(api, club_ids, trainer_id, today, deadline_iso)

    if "secondOrg" in data:
        setup_second_org(api_base, data["secondOrg"])

    print("\n=== Seed Complete ===\n")
    print("Login credentials:")
    print(f"  Email:    {data['admin']['email']}")
    print(f"  Password: {data['admin']['password']}")
    if "superAdmin" in data:
        print(f"\n  Super admin: {data['superAdmin']['email']}")
        print(f"  Password:    {data['superAdmin']['password']}")
    if "secondOrg" in data:
        print(f"\n  Second org admin: {data['secondOrg']['admin']['email']}")
        print(f"  Jan is lid van beide orgs - org-switcher zichtbaar in topbar")
    print("\nURLs:")
    print("  Frontend:  http://localhost:5317")
    print("  API:       http://localhost:5142/swagger")
    print("  Email:     http://localhost:3001")
    print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
