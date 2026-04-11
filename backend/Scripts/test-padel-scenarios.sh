#!/bin/bash
#
# Padel Club Scenarios: 6 fields, Mon-Thu 18:00-21:00 (3 slots/day)
# = 72 time slots, 4 per court = 288 total capacity
#

API_BASE="${1:-http://localhost:5142/api}"

invoke() {
    local method="$1" path="$2" body="$3" token="$4"
    local headers=(-H "Content-Type: application/json")
    [ -n "$token" ] && headers+=(-H "Authorization: Bearer $token")
    local args=(-s -X "$method" "${headers[@]}")
    [ -n "$body" ] && args+=(-d "$body")
    curl "${args[@]}" "${API_BASE}${path}" 2>/dev/null
}

# ── Auth setup ────────────────────────────────────────────────────────────────
echo ""
echo "Setting up..."
auth=$(invoke POST "/auth/register" '{
    "organizationName": "Padel Club Antwerpen",
    "firstName": "Coach",
    "lastName": "Admin",
    "email": "coach@padelclub.be",
    "password": "Padel2026!"
}')
token=$(echo "$auth" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))" 2>/dev/null)
if [ -z "$token" ]; then
    auth=$(invoke POST "/auth/login" '{"email":"coach@padelclub.be","password":"Padel2026!"}')
    token=$(echo "$auth" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))" 2>/dev/null)
fi
userId=$(echo "$auth" | python3 -c "import sys,json; print(json.load(sys.stdin).get('userId',''))" 2>/dev/null)
clubId=$(invoke POST "/tennisclubs" '{"name":"Padel Club Antwerpen","address":"Padelstraat 10, 2000 Antwerpen"}' "$token" | tr -d '"')

today=$(date +%Y-%m-%d)
endDate=$(date -v+3m +%Y-%m-%d 2>/dev/null || date -d "+3 months" +%Y-%m-%d)
deadline=$(date -v+2m -u +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || date -u -d "+2 months" +%Y-%m-%dT%H:%M:%SZ)

echo "   Authenticated. Club: Padel Club Antwerpen"

# ── Build weekly template: 6 fields × 4 days × 3 hours ───────────────────────
build_template() {
    local slots="["
    local first=true
    for day in 1 2 3 4; do  # Mon=1, Tue=2, Wed=3, Thu=4
        for hour in 18 19 20; do
            for field in $(seq 1 6); do
                [ "$first" = true ] && first=false || slots+=","
                slots+="{\"dayOfWeek\":$day,\"startTime\":\"${hour}:00\",\"endTime\":\"$((hour+1)):00\",\"trainerId\":\"$userId\",\"courtName\":\"Veld $field\",\"maxStudents\":4}"
            done
        done
    done
    slots+="]"
    echo "$slots"
}

# ── Create a series ───────────────────────────────────────────────────────────
create_series() {
    local name="$1"
    local template
    template=$(build_template)

    local seriesId
    seriesId=$(invoke POST "/lessonseries" "{
        \"tennisClubId\": \"$clubId\",
        \"name\": \"$name\",
        \"description\": \"6 velden, ma-do 18-21u\",
        \"level\": 1,
        \"price\": 200.00,
        \"startDate\": \"$today\",
        \"endDate\": \"$endDate\",
        \"registrationDeadline\": \"$deadline\",
        \"maxRegistrations\": 300,
        \"weeklyTemplate\": $template,
        \"lessons\": [{\"trainerId\":\"$userId\",\"date\":\"$today\",\"startTime\":\"18:00\",\"endTime\":\"19:00\",\"courtName\":\"Veld 1\",\"maxStudents\":4}]
    }" "$token" | tr -d '"')

    echo "$seriesId"
}

# ── Fetch slots and build a lookup ────────────────────────────────────────────
fetch_slots() {
    local seriesId="$1"
    invoke GET "/public/lessonseries/$seriesId/timeslots"
}

# ── Enroll with preferences (uses python for complex logic) ──────────────────
enroll_batch() {
    local seriesId="$1"
    local slotsJson="$2"
    local scenario="$3"
    local count="$4"

    python3 -c "
import json, random, sys

slots = json.loads('''$slotsJson''')
scenario = '$scenario'
count = int('$count')
api_base = '$API_BASE'
series_id = '$seriesId'

days_nl = {1:'Ma',2:'Di',3:'Wo',4:'Do'}

# Index slots by (day, hour)
slot_map = {}
for s in slots:
    key = (s['dayOfWeek'], s['startTime'])
    if key not in slot_map:
        slot_map[key] = []
    slot_map[key].append(s['id'])

# All unique (day, hour) combos
time_keys = sorted(slot_map.keys())

names = [
    'Emma Claes','Lucas Peeters','Lotte Van Damme','Noah Willems','Julie Maes',
    'Axel Dubois','Sarah Jacobs','Thomas Hermans','Marie Lambert','Bram Wouters',
    'Eline De Smet','Jef Mertens','Lisa Janssens','Daan Vermeer','Sophie Nijs',
    'Ruben Hendrickx','Eva Lemmens','Timo Jacobs','Nina Cools','Pieter Vos',
    'Charlotte Dries','Sam Pauwels','Luna Michiels','Wout Beyens','Hanne Geerts',
    'Milan Aerts','Roos Verhoeven','Stijn Vandenberghe','Amber Goossens','Jelle Maes',
    'Fien Brouwers','Lars Smeets','Noor Dierickx','Jesse Willems','Mila Van Hees',
    'Robin Peters','Lies Vandenbroucke','Sander Thijs','Maya Keunen','Tibo Claessens',
    'Anouk Pieters','Raf Devos','Lien Vermeiren','Koen Van Dyck','Jana Bogaert',
    'Brent Wouters','Silke Michiels','Yannick Peeters','Fleur De Graef','Matthias Berg',
    'Anna Stevens','Victor Nijs','Elise Vandamme','Gilles Cuypers','Nathalie Moons',
    'Dries Van Acker','Lauren De Backer','Quinten Smit','Jade Martens','Tim Claeys',
    'Sara Willems','Tom Hendrickx','Clara Maes','Joris Verbeke','Lana Pauwels',
    'Kevin Nuyts','Mieke Janssen','Niels Cools','Ines Geerts','Sven Aerts',
    'Katrien Dries','Robbe Michiels','Elien Goossens','Ward Vermeer','Celine Jacobs',
    'Dieter Mertens','Annelies Beyens','Yves Thijs','Jolien Peters','Dennis Smeets'
]

random.seed(42)  # Reproducible

enrollments = []

if scenario == 'perfect':
    # 60 students, well-distributed across all time slots
    # Each student prefers 2-3 time blocks, spread evenly
    for i in range(count):
        # Assign primary preference to spread evenly
        primary_key = time_keys[i % len(time_keys)]
        secondary_key = time_keys[(i + 4) % len(time_keys)]

        prefs = []
        for key in time_keys:
            for sid in slot_map[key]:
                if key == primary_key:
                    prefs.append({'weeklyTemplateEntryId': sid, 'preference': 2})
                elif key == secondary_key:
                    prefs.append({'weeklyTemplateEntryId': sid, 'preference': 1})
                else:
                    prefs.append({'weeklyTemplateEntryId': sid, 'preference': 3})

        enrollments.append({
            'name': names[i % len(names)],
            'email': f'student{i}@padel-{scenario}.be',
            'prefs': prefs,
            'openToGrouping': True,
            'type': 'solo',
            'members': None
        })

elif scenario == 'oversubscribed':
    # 50 students, 35 all want Mon/Tue 19:00 (the popular evening slot)
    popular_keys = [(1,'19:00'), (2,'19:00')]  # Mon & Tue 19:00

    for i in range(count):
        prefs = []
        if i < 35:
            # Popular slot seekers
            for key in time_keys:
                for sid in slot_map[key]:
                    if key in popular_keys:
                        prefs.append({'weeklyTemplateEntryId': sid, 'preference': 2})
                    elif key[1] == '19:00':
                        prefs.append({'weeklyTemplateEntryId': sid, 'preference': 1})
                    else:
                        prefs.append({'weeklyTemplateEntryId': sid, 'preference': 3})
        else:
            # Flexible students
            for key in time_keys:
                for sid in slot_map[key]:
                    pref = random.choice([1, 1, 2])
                    prefs.append({'weeklyTemplateEntryId': sid, 'preference': pref})

        enrollments.append({
            'name': names[i % len(names)],
            'email': f'student{i}@padel-{scenario}.be',
            'prefs': prefs,
            'openToGrouping': i < 35,
            'type': 'solo',
            'members': None
        })

elif scenario == 'sparse':
    # Only 8 students, some with groups — lots of empty slots
    for i in range(5):
        preferred_day = random.choice([1,2,3,4])
        preferred_hour = random.choice(['18:00','19:00','20:00'])
        prefs = []
        for key in time_keys:
            for sid in slot_map[key]:
                if key == (preferred_day, preferred_hour):
                    prefs.append({'weeklyTemplateEntryId': sid, 'preference': 2})
                elif key[0] == preferred_day:
                    prefs.append({'weeklyTemplateEntryId': sid, 'preference': 1})
                else:
                    prefs.append({'weeklyTemplateEntryId': sid, 'preference': 3})

        enrollments.append({
            'name': names[i],
            'email': f'student{i}@padel-{scenario}.be',
            'prefs': prefs,
            'openToGrouping': False,
            'type': 'solo',
            'members': None
        })

    # One group of 3
    prefs = []
    for key in time_keys:
        for sid in slot_map[key]:
            if key == (3, '19:00'):
                prefs.append({'weeklyTemplateEntryId': sid, 'preference': 2})
            elif key[0] == 3:
                prefs.append({'weeklyTemplateEntryId': sid, 'preference': 1})
            else:
                prefs.append({'weeklyTemplateEntryId': sid, 'preference': 3})

    enrollments.append({
        'name': names[10],
        'email': f'student10@padel-{scenario}.be',
        'prefs': prefs,
        'openToGrouping': False,
        'type': 'group',
        'members': [
            {'studentName': names[11], 'studentEmail': f'student11@padel-{scenario}.be'},
            {'studentName': names[12], 'studentEmail': f'student12@padel-{scenario}.be'}
        ]
    })

elif scenario == 'random':
    # 30 students with fully random preferences, some groups
    for i in range(25):
        prefs = []
        for key in time_keys:
            for sid in slot_map[key]:
                p = random.choices([1,2,3], weights=[30,30,40])[0]
                prefs.append({'weeklyTemplateEntryId': sid, 'preference': p})

        enrollments.append({
            'name': names[i % len(names)],
            'email': f'student{i}@padel-{scenario}.be',
            'prefs': prefs,
            'openToGrouping': random.random() > 0.5,
            'type': 'solo',
            'members': None
        })

    # 2 groups
    for g in range(2):
        idx = 25 + g * 4
        prefs = []
        for key in time_keys:
            for sid in slot_map[key]:
                p = random.choices([1,2,3], weights=[25,35,40])[0]
                prefs.append({'weeklyTemplateEntryId': sid, 'preference': p})

        enrollments.append({
            'name': names[idx % len(names)],
            'email': f'student{idx}@padel-{scenario}.be',
            'prefs': prefs,
            'openToGrouping': False,
            'type': 'group',
            'members': [
                {'studentName': names[(idx+1) % len(names)], 'studentEmail': f'student{idx+1}@padel-{scenario}.be'},
                {'studentName': names[(idx+2) % len(names)], 'studentEmail': f'student{idx+2}@padel-{scenario}.be'},
                {'studentName': names[(idx+3) % len(names)], 'studentEmail': f'student{idx+3}@padel-{scenario}.be'}
            ]
        })

# Print enrollments as JSON for the bash script to consume
print(json.dumps(enrollments))
" | python3 -c "
import json, subprocess, sys

enrollments = json.load(sys.stdin)
api_base = '$API_BASE'
series_id = '$seriesId'
success = 0
fail = 0

for e in enrollments:
    body = {
        'studentName': e['name'],
        'studentEmail': e['email'],
        'responses': [],
        'enrollmentType': e['type'],
        'isOpenToGrouping': e['openToGrouping'],
        'timeSlotPreferences': e['prefs'],
    }
    if e['members']:
        body['groupMembers'] = e['members']

    result = subprocess.run(
        ['curl', '-s', '-o', '/dev/null', '-w', '%{http_code}',
         '-X', 'POST', f'{api_base}/public/lessonseries/{series_id}/enroll',
         '-H', 'Content-Type: application/json',
         '-d', json.dumps(body)],
        capture_output=True, text=True
    )
    code = result.stdout.strip()
    if code == '200':
        success += 1
    else:
        fail += 1

groups = sum(1 for e in enrollments if e['type'] == 'group')
solos = len(enrollments) - groups
members = sum(len(e['members']) for e in enrollments if e['members'])
total_people = solos + groups + members

print(f'   Enrolled: {success} ok, {fail} failed ({solos} solo, {groups} groups with {members} extra members = {total_people} people)')
"
}

# ── Show planning result ──────────────────────────────────────────────────────
show_planning() {
    local seriesId="$1"
    local result
    result=$(invoke POST "/lessonseries/$seriesId/planning/generate" "" "$token")

    echo "$result" | python3 -c "
import sys, json

data = json.load(sys.stdin)
days = {1:'Ma',2:'Di',3:'Wo',4:'Do',5:'Vr',6:'Za',0:'Zo'}

assigned_count = 0
total_capacity = 0
slots_with_people = 0

# Group by day+hour
by_time = {}
for slot in data['slots']:
    key = (slot['dayOfWeek'], slot['startTime'])
    if key not in by_time:
        by_time[key] = []
    by_time[key].append(slot)

for key in sorted(by_time.keys()):
    day, time = key
    slots = by_time[key]
    end_time = slots[0]['endTime']

    # Collect filled courts
    filled = []
    empty = 0
    for s in slots:
        cap = s['maxCapacity']
        total_capacity += cap
        count = s['currentCount']
        assigned_count += count
        if count > 0:
            slots_with_people += 1
            names_list = []
            for a in s['assignments']:
                tag = 'G' if a.get('groupId') else ' '
                names_list.append(f'[{tag}] ' + ', '.join(a['studentNames']))
            fill_bar = '█' * count + '░' * (cap - count)
            filled.append(f'{s[\"courtName\"]} [{fill_bar}] {count}/{cap}: {\" | \".join(names_list)}')
        else:
            empty += 1

    print(f'   {days[day]} {time}-{end_time} ({len(slots)} velden)')
    for f in filled:
        print(f'      {f}')
    if empty > 0:
        print(f'      ({empty} veld{\"en\" if empty > 1 else \"\"} leeg)')
    print()

unassigned = data.get('unassigned', [])
print(f'   ═══════════════════════════════════════')
print(f'   Toegewezen: {assigned_count}/{total_capacity} plaatsen')
print(f'   Actieve velden: {slots_with_people}/{len(data[\"slots\"])}')
if unassigned:
    print(f'   ⚠️  Niet toegewezen: {len(unassigned)} personen')
    for u in unassigned[:10]:
        print(f'      - {u[\"studentName\"]}')
    if len(unassigned) > 10:
        print(f'      ... en nog {len(unassigned) - 10} anderen')
else:
    print(f'   ✅ Iedereen is toegewezen!')
print()
"
}

# ══════════════════════════════════════════════════════════════════════════════
# SCENARIO 1: Perfect — 60 students, well-distributed
# ══════════════════════════════════════════════════════════════════════════════
echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  SCENARIO 1: PERFECT FIT                                   ║"
echo "║  60 spelers, goed verdeeld over alle momenten              ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
sid=$(create_series "Scenario 1: Perfect Fit")
echo "   Series: $sid"
slotsJson=$(fetch_slots "$sid")
slotCount=$(echo "$slotsJson" | python3 -c "import sys,json; print(len(json.load(sys.stdin)))")
echo "   Slots: $slotCount"
enroll_batch "$sid" "$slotsJson" "perfect" "60"
echo ""
show_planning "$sid"

# ══════════════════════════════════════════════════════════════════════════════
# SCENARIO 2: Oversubscribed — 50 students, 35 want same popular slots
# ══════════════════════════════════════════════════════════════════════════════
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  SCENARIO 2: OVERSUBSCRIBED                                ║"
echo "║  50 spelers, 35 willen allemaal ma/di 19:00                ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
sid=$(create_series "Scenario 2: Oversubscribed")
echo "   Series: $sid"
slotsJson=$(fetch_slots "$sid")
enroll_batch "$sid" "$slotsJson" "oversubscribed" "50"
echo ""
show_planning "$sid"

# ══════════════════════════════════════════════════════════════════════════════
# SCENARIO 3: Sparse — 8 students (5 solo + 1 group of 3)
# ══════════════════════════════════════════════════════════════════════════════
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  SCENARIO 3: SPARSE                                        ║"
echo "║  8 spelers (5 solo + 1 groep van 3), veel lege velden      ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
sid=$(create_series "Scenario 3: Sparse")
echo "   Series: $sid"
slotsJson=$(fetch_slots "$sid")
enroll_batch "$sid" "$slotsJson" "sparse" "0"
echo ""
show_planning "$sid"

# ══════════════════════════════════════════════════════════════════════════════
# SCENARIO 4: Random — 33 students (25 solo + 2 groups of 4)
# ══════════════════════════════════════════════════════════════════════════════
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  SCENARIO 4: RANDOM                                        ║"
echo "║  33 spelers (25 solo + 2 groepen van 4), random voorkeuren  ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
sid=$(create_series "Scenario 4: Random")
echo "   Series: $sid"
slotsJson=$(fetch_slots "$sid")
enroll_batch "$sid" "$slotsJson" "random" "0"
echo ""
show_planning "$sid"
