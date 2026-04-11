#!/bin/bash
#
# Complex planning scenario: creates a series with 3 time slots,
# enrolls 10 students (mix of solo, open-to-grouping, and groups),
# each with different availability preferences, then generates the plan.
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

echo ""
echo "╔══════════════════════════════════════════════════════╗"
echo "║  Complex Planning Scenario                          ║"
echo "╚══════════════════════════════════════════════════════╝"
echo ""

# ── 1. Auth ──────────────────────────────────────────────────────────────────
echo "1. Authenticating..."
auth=$(invoke POST "/auth/register" '{
    "organizationName": "TC Het Voorbeeld",
    "firstName": "Admin",
    "lastName": "Beheerder",
    "email": "admin@voorbeeld.be",
    "password": "Complex1234!"
}')
token=$(echo "$auth" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))" 2>/dev/null)
if [ -z "$token" ]; then
    auth=$(invoke POST "/auth/login" '{"email":"admin@voorbeeld.be","password":"Complex1234!"}')
    token=$(echo "$auth" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))" 2>/dev/null)
fi
echo "   Token: ${token:0:20}..."

# ── 2. Tennis club ───────────────────────────────────────────────────────────
echo ""
echo "2. Creating tennis club..."
clubId=$(invoke POST "/tennisclubs" '{"name":"TC Het Voorbeeld","address":"Sportlaan 1, 2000 Antwerpen"}' "$token" | tr -d '"')
echo "   Club: $clubId"

# ── 3. Get trainer ID ────────────────────────────────────────────────────────
userId=$(echo "$auth" | python3 -c "import sys,json; print(json.load(sys.stdin).get('userId',''))" 2>/dev/null)

# ── 4. Create lesson series with 3 time slots ────────────────────────────────
echo ""
echo "3. Creating lesson series with 3 weekly time slots..."
today=$(date +%Y-%m-%d)
endDate=$(date -v+3m +%Y-%m-%d 2>/dev/null || date -d "+3 months" +%Y-%m-%d)
deadline=$(date -v+2m -u +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || date -u -d "+2 months" +%Y-%m-%dT%H:%M:%SZ)

seriesId=$(invoke POST "/lessonseries" "{
    \"tennisClubId\": \"$clubId\",
    \"name\": \"Voorjaarslessen 2026 - Beginners\",
    \"description\": \"Basisvaardigheden voor nieuwe spelers. 3 groepen van max 4.\",
    \"level\": 1,
    \"price\": 150.00,
    \"startDate\": \"$today\",
    \"endDate\": \"$endDate\",
    \"registrationDeadline\": \"$deadline\",
    \"maxRegistrations\": 20,
    \"weeklyTemplate\": [
        {\"dayOfWeek\": 1, \"startTime\": \"09:00\", \"endTime\": \"10:00\", \"trainerId\": \"$userId\", \"courtName\": \"Baan 1\", \"maxStudents\": 4},
        {\"dayOfWeek\": 3, \"startTime\": \"14:00\", \"endTime\": \"15:00\", \"trainerId\": \"$userId\", \"courtName\": \"Baan 2\", \"maxStudents\": 4},
        {\"dayOfWeek\": 5, \"startTime\": \"16:00\", \"endTime\": \"17:00\", \"trainerId\": \"$userId\", \"courtName\": \"Baan 1\", \"maxStudents\": 4}
    ],
    \"lessons\": [
        {\"trainerId\": \"$userId\", \"date\": \"$today\", \"startTime\": \"09:00\", \"endTime\": \"10:00\", \"courtName\": \"Baan 1\", \"maxStudents\": 4}
    ]
}" "$token" | tr -d '"')
echo "   Series: $seriesId"

# ── 5. Fetch time slots ──────────────────────────────────────────────────────
echo ""
echo "4. Fetching time slots..."
slots=$(invoke GET "/public/lessonseries/$seriesId/timeslots")
echo "$slots" | python3 -c "
import sys, json
data = json.load(sys.stdin)
days = ['Zo','Ma','Di','Wo','Do','Vr','Za']
for s in data:
    print(f'   Slot {s[\"id\"][:8]}... | {days[s[\"dayOfWeek\"]]} {s[\"startTime\"]}-{s[\"endTime\"]} | {s[\"courtName\"]} | max {s[\"maxStudents\"]}')
"

slot1=$(echo "$slots" | python3 -c "import sys,json; print(json.load(sys.stdin)[0]['id'])" 2>/dev/null)
slot2=$(echo "$slots" | python3 -c "import sys,json; print(json.load(sys.stdin)[1]['id'])" 2>/dev/null)
slot3=$(echo "$slots" | python3 -c "import sys,json; print(json.load(sys.stdin)[2]['id'])" 2>/dev/null)

# ── 6. Enroll students ───────────────────────────────────────────────────────
echo ""
echo "5. Enrolling students..."
echo ""
echo "   Legend: P=Preferred  A=Available  X=Unavailable"
echo "   ┌──────────────────────┬─────────┬─────────┬─────────┐"
echo "   │ Student              │ Ma 9:00 │ Wo 14:00│ Vr 16:00│"
echo "   ├──────────────────────┼─────────┼─────────┼─────────┤"

# Solo 1: Emma — prefers Monday, available Wednesday, can't Friday
invoke POST "/public/lessonseries/$seriesId/enroll" "{
    \"studentName\": \"Emma Claes\", \"studentEmail\": \"emma@test.be\", \"responses\": [],
    \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
    \"timeSlotPreferences\": [
        {\"weeklyTemplateEntryId\": \"$slot1\", \"preference\": 2},
        {\"weeklyTemplateEntryId\": \"$slot2\", \"preference\": 1},
        {\"weeklyTemplateEntryId\": \"$slot3\", \"preference\": 3}
    ]
}" > /dev/null
echo "   │ Emma Claes (solo/open)│    P    │    A    │    X    │"

# Solo 2: Lucas — prefers Monday, available Wednesday, can't Friday
invoke POST "/public/lessonseries/$seriesId/enroll" "{
    \"studentName\": \"Lucas Peeters\", \"studentEmail\": \"lucas@test.be\", \"responses\": [],
    \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
    \"timeSlotPreferences\": [
        {\"weeklyTemplateEntryId\": \"$slot1\", \"preference\": 2},
        {\"weeklyTemplateEntryId\": \"$slot2\", \"preference\": 1},
        {\"weeklyTemplateEntryId\": \"$slot3\", \"preference\": 3}
    ]
}" > /dev/null
echo "   │ Lucas Peeters(solo/op)│    P    │    A    │    X    │"

# Solo 3: Lotte — prefers Wednesday, available Friday
invoke POST "/public/lessonseries/$seriesId/enroll" "{
    \"studentName\": \"Lotte Van Damme\", \"studentEmail\": \"lotte@test.be\", \"responses\": [],
    \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
    \"timeSlotPreferences\": [
        {\"weeklyTemplateEntryId\": \"$slot1\", \"preference\": 3},
        {\"weeklyTemplateEntryId\": \"$slot2\", \"preference\": 2},
        {\"weeklyTemplateEntryId\": \"$slot3\", \"preference\": 1}
    ]
}" > /dev/null
echo "   │ Lotte Van Dam(solo/op)│    X    │    P    │    A    │"

# Solo 4: Noah — prefers Friday, available Wednesday
invoke POST "/public/lessonseries/$seriesId/enroll" "{
    \"studentName\": \"Noah Willems\", \"studentEmail\": \"noah@test.be\", \"responses\": [],
    \"enrollmentType\": \"solo\", \"isOpenToGrouping\": false,
    \"timeSlotPreferences\": [
        {\"weeklyTemplateEntryId\": \"$slot1\", \"preference\": 3},
        {\"weeklyTemplateEntryId\": \"$slot2\", \"preference\": 1},
        {\"weeklyTemplateEntryId\": \"$slot3\", \"preference\": 2}
    ]
}" > /dev/null
echo "   │ Noah Willems (solo)   │    X    │    A    │    P    │"

# Solo 5: Julie — available everywhere, prefers Friday
invoke POST "/public/lessonseries/$seriesId/enroll" "{
    \"studentName\": \"Julie Maes\", \"studentEmail\": \"julie@test.be\", \"responses\": [],
    \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
    \"timeSlotPreferences\": [
        {\"weeklyTemplateEntryId\": \"$slot1\", \"preference\": 1},
        {\"weeklyTemplateEntryId\": \"$slot2\", \"preference\": 1},
        {\"weeklyTemplateEntryId\": \"$slot3\", \"preference\": 2}
    ]
}" > /dev/null
echo "   │ Julie Maes (solo/open)│    A    │    A    │    P    │"

# Group 1: Familie Dubois — parent + 2 kids, prefer Monday
invoke POST "/public/lessonseries/$seriesId/enroll" "{
    \"studentName\": \"Axel Dubois\", \"studentEmail\": \"axel@test.be\", \"responses\": [],
    \"enrollmentType\": \"group\", \"isOpenToGrouping\": false,
    \"groupMembers\": [
        {\"studentName\": \"Marie Dubois\", \"studentEmail\": \"marie.d@test.be\"},
        {\"studentName\": \"Tom Dubois\", \"studentEmail\": \"tom.d@test.be\"}
    ],
    \"timeSlotPreferences\": [
        {\"weeklyTemplateEntryId\": \"$slot1\", \"preference\": 2},
        {\"weeklyTemplateEntryId\": \"$slot2\", \"preference\": 1},
        {\"weeklyTemplateEntryId\": \"$slot3\", \"preference\": 3}
    ]
}" > /dev/null
echo "   │ Groep Dubois (3 pers) │    P    │    A    │    X    │"

# Solo 6: Sarah — only Wednesday
invoke POST "/public/lessonseries/$seriesId/enroll" "{
    \"studentName\": \"Sarah Jacobs\", \"studentEmail\": \"sarah@test.be\", \"responses\": [],
    \"enrollmentType\": \"solo\", \"isOpenToGrouping\": false,
    \"timeSlotPreferences\": [
        {\"weeklyTemplateEntryId\": \"$slot1\", \"preference\": 3},
        {\"weeklyTemplateEntryId\": \"$slot2\", \"preference\": 2},
        {\"weeklyTemplateEntryId\": \"$slot3\", \"preference\": 3}
    ]
}" > /dev/null
echo "   │ Sarah Jacobs (solo)   │    X    │    P    │    X    │"

# Solo 7: Thomas — prefers Monday, no other option
invoke POST "/public/lessonseries/$seriesId/enroll" "{
    \"studentName\": \"Thomas Hermans\", \"studentEmail\": \"thomas@test.be\", \"responses\": [],
    \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
    \"timeSlotPreferences\": [
        {\"weeklyTemplateEntryId\": \"$slot1\", \"preference\": 2},
        {\"weeklyTemplateEntryId\": \"$slot2\", \"preference\": 3},
        {\"weeklyTemplateEntryId\": \"$slot3\", \"preference\": 3}
    ]
}" > /dev/null
echo "   │ Thomas Herman(solo/op)│    P    │    X    │    X    │"

echo "   └──────────────────────┴─────────┴─────────┴─────────┘"
echo ""
echo "   Total: 10 students (7 solo + 1 group of 3)"
echo "   Capacity: 3 slots × 4 = 12 spots"

# ── 7. Generate planning ─────────────────────────────────────────────────────
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "6. Generating planning proposal..."
echo "═══════════════════════════════════════════════════════════"
echo ""

result=$(invoke POST "/lessonseries/$seriesId/planning/generate" "" "$token")

echo "$result" | python3 -c "
import sys, json

data = json.load(sys.stdin)
days = ['Zo','Ma','Di','Wo','Do','Vr','Za']

print(f'   Planning status: {data[\"planningStatus\"]}')
print()

for slot in data['slots']:
    day = days[slot['dayOfWeek']]
    count = slot['currentCount']
    cap = slot['maxCapacity']
    fill = '█' * count + '░' * (cap - count)
    court = slot.get('courtName', '?')

    print(f'   ┌─ {day} {slot[\"startTime\"]}-{slot[\"endTime\"]} ({court}) ─ [{fill}] {count}/{cap}')

    if slot['assignments']:
        for a in slot['assignments']:
            names = ', '.join(a['studentNames'])
            tag = '👥 Groep' if a.get('groupId') else '👤 Solo'
            status = a['status']
            print(f'   │  {tag}: {names} ({status})')
    else:
        print(f'   │  (leeg)')
    print(f'   └───────────────────────────────────────────')
    print()

if data['unassigned']:
    print(f'   ⚠️  Niet toegewezen ({len(data[\"unassigned\"])}):')
    for u in data['unassigned']:
        print(f'      - {u[\"studentName\"]}: {u[\"reason\"]}')
else:
    print('   ✅ Iedereen is toegewezen!')
print()
"

# ── 8. Show the admin planning overview ───────────────────────────────────────
echo "═══════════════════════════════════════════════════════════"
echo "7. Fetching planning overview (same as FE would see)..."
echo "═══════════════════════════════════════════════════════════"
echo ""
overview=$(invoke GET "/lessonseries/$seriesId/planning" "" "$token")
echo "$overview" | python3 -m json.tool

echo ""
echo "═══════════════════════════════════════════════════════════"
echo "Done. Series ID: $seriesId"
echo "═══════════════════════════════════════════════════════════"
