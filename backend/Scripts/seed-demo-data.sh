#!/bin/bash
#
# Seeds a demo environment with realistic data via the CoachOS API.
# Prerequisites: API running on http://localhost:5142
# Creates: 1 org, 1 admin, 2 trainers, 2 clubs, 3 lesson series, 24 lessons, 10 enrollments.
#
# Usage:
#   ./seed-demo-data.sh
#   ./seed-demo-data.sh http://localhost:5142/api
#

API_BASE="${1:-http://localhost:5142/api}"

invoke_api() {
    local method="$1"
    local path="$2"
    local body="$3"
    local token="$4"

    local headers=(-H "Content-Type: application/json")
    if [ -n "$token" ]; then
        headers+=(-H "Authorization: Bearer $token")
    fi

    local args=(-s -X "$method" "${headers[@]}")
    if [ -n "$body" ]; then
        args+=(-d "$body")
    fi

    curl "${args[@]}" "${API_BASE}${path}" 2>/dev/null
}

echo ""
echo "=== CoachOS Demo Seed ==="
echo "API: $API_BASE"
echo ""

# 1. Register admin
echo "1. Registering admin user..."
auth=$(invoke_api POST "/auth/register" '{
    "organizationName": "TC De Aces",
    "firstName": "Jan",
    "lastName": "Janssen",
    "email": "jan@deaces.be",
    "password": "Demo1234!"
}')

token=$(echo "$auth" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))" 2>/dev/null)

if [ -z "$token" ]; then
    echo "   Registration failed - user may already exist. Trying login..."
    auth=$(invoke_api POST "/auth/login" '{
        "email": "jan@deaces.be",
        "password": "Demo1234!"
    }')
    token=$(echo "$auth" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))" 2>/dev/null)
fi

if [ -z "$token" ]; then
    echo "   Cannot authenticate. Exiting."
    exit 1
fi

firstName=$(echo "$auth" | python3 -c "import sys,json; print(json.load(sys.stdin).get('firstName',''))" 2>/dev/null)
lastName=$(echo "$auth" | python3 -c "import sys,json; print(json.load(sys.stdin).get('lastName',''))" 2>/dev/null)
userId=$(echo "$auth" | python3 -c "import sys,json; print(json.load(sys.stdin).get('userId',''))" 2>/dev/null)
echo "   OK - Logged in as $firstName $lastName"

# 2. Create tennis clubs
echo ""
echo "2. Creating tennis clubs..."

clubIds=()

id=$(invoke_api POST "/tennisclubs" '{"name": "TC De Aces", "address": "Sportlaan 12, 2000 Antwerpen"}' "$token")
# Strip quotes from GUID response
id=$(echo "$id" | tr -d '"')
if [ -n "$id" ] && [ "$id" != "null" ]; then
    clubIds+=("$id")
    echo "   Created: TC De Aces"
fi

id=$(invoke_api POST "/tennisclubs" '{"name": "Padel Center Brussel", "address": "Louizalaan 45, 1050 Brussel"}' "$token")
id=$(echo "$id" | tr -d '"')
if [ -n "$id" ] && [ "$id" != "null" ]; then
    clubIds+=("$id")
    echo "   Created: Padel Center Brussel"
fi

# 3. Invite trainers
echo ""
echo "3. Inviting trainers..."

invoke_api POST "/trainers/invite" '{"firstName": "Sophie", "lastName": "De Vries", "email": "sophie@deaces.be"}' "$token" > /dev/null
echo "   Invited: Sophie De Vries"

invoke_api POST "/trainers/invite" '{"firstName": "Pieter", "lastName": "Mertens", "email": "pieter@deaces.be"}' "$token" > /dev/null
echo "   Invited: Pieter Mertens"

# Get trainer ID (admin is also a trainer)
trainerList=$(invoke_api GET "/trainers" "" "$token")
trainerId=$(echo "$trainerList" | python3 -c "
import sys, json
data = json.load(sys.stdin)
active = [t for t in data if t.get('isActive')]
print(active[0]['id'] if active else '')
" 2>/dev/null)

if [ -z "$trainerId" ]; then
    trainerId="$userId"
fi

activeCount=$(echo "$trainerList" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(len([t for t in data if t.get('isActive')]))
" 2>/dev/null)
echo "   Active trainers: $activeCount"

# 4. Create lesson series
echo ""
echo "4. Creating lesson series..."

if [ ${#clubIds[@]} -eq 0 ]; then
    echo "   No clubs created - skipping series."
    exit 0
fi

clubId="${clubIds[0]}"
if [ ${#clubIds[@]} -gt 1 ]; then
    clubId2="${clubIds[1]}"
else
    clubId2="$clubId"
fi

today=$(date +%Y-%m-%d)
endDate=$(date -v+3m +%Y-%m-%d 2>/dev/null || date -d "+3 months" +%Y-%m-%d)
startDate2=$(date -v+7d +%Y-%m-%d 2>/dev/null || date -d "+7 days" +%Y-%m-%d)
endDate2=$(date -v+2m +%Y-%m-%d 2>/dev/null || date -d "+2 months" +%Y-%m-%d)
deadline=$(date -v+3m -u +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || date -u -d "+3 months" +%Y-%m-%dT%H:%M:%SZ)

seriesIds=()

id=$(invoke_api POST "/lessonseries" "{
    \"trainerId\": \"$trainerId\", \"tennisClubId\": \"$clubId\",
    \"name\": \"Voorjaarslessen Beginners\", \"description\": \"Tennistraining voor beginners. Leer de basisvaardigheden.\",
    \"level\": 1, \"price\": 120.00, \"startDate\": \"$today\", \"endDate\": \"$endDate\",
    \"registrationDeadline\": \"$deadline\", \"maxRegistrations\": 12,
    \"weeklyTemplate\": [{\"dayOfWeek\": 0, \"startTime\": \"09:00\", \"endTime\": \"10:00\", \"trainerId\": \"$trainerId\", \"courtName\": \"Baan 1\", \"maxStudents\": 4}],
    \"lessons\": [{\"trainerId\": \"$trainerId\", \"date\": \"$today\", \"startTime\": \"09:00\", \"endTime\": \"10:00\", \"courtName\": \"Baan 1\", \"maxStudents\": 4}]
}" "$token")
id=$(echo "$id" | tr -d '"')
if [ -n "$id" ] && [ "$id" != "null" ]; then
    seriesIds+=("$id")
    echo "   Created: Voorjaarslessen Beginners"
fi

id=$(invoke_api POST "/lessonseries" "{
    \"trainerId\": \"$trainerId\", \"tennisClubId\": \"$clubId\",
    \"name\": \"Competitietraining Gevorderd\", \"description\": \"Intensieve training voor competitiespelers.\",
    \"level\": 4, \"price\": 180.00, \"startDate\": \"$today\", \"endDate\": \"$endDate\",
    \"registrationDeadline\": \"$deadline\", \"maxRegistrations\": 8,
    \"weeklyTemplate\": [{\"dayOfWeek\": 2, \"startTime\": \"10:30\", \"endTime\": \"12:00\", \"trainerId\": \"$trainerId\", \"courtName\": \"Baan 2\", \"maxStudents\": 4}],
    \"lessons\": [{\"trainerId\": \"$trainerId\", \"date\": \"$today\", \"startTime\": \"10:30\", \"endTime\": \"12:00\", \"courtName\": \"Baan 2\", \"maxStudents\": 4}]
}" "$token")
id=$(echo "$id" | tr -d '"')
if [ -n "$id" ] && [ "$id" != "null" ]; then
    seriesIds+=("$id")
    echo "   Created: Competitietraining Gevorderd"
fi

id=$(invoke_api POST "/lessonseries" "{
    \"trainerId\": \"$trainerId\", \"tennisClubId\": \"$clubId2\",
    \"name\": \"Padel Introductie\", \"description\": \"Kennismaken met padel. Regels en basistechnieken.\",
    \"level\": 1, \"price\": 95.00, \"startDate\": \"$startDate2\", \"endDate\": \"$endDate2\",
    \"registrationDeadline\": \"$deadline\", \"maxRegistrations\": 16,
    \"weeklyTemplate\": [{\"dayOfWeek\": 4, \"startTime\": \"14:00\", \"endTime\": \"15:00\", \"trainerId\": \"$trainerId\", \"courtName\": \"Padel 1\", \"maxStudents\": 4}],
    \"lessons\": [{\"trainerId\": \"$trainerId\", \"date\": \"$startDate2\", \"startTime\": \"14:00\", \"endTime\": \"15:00\", \"courtName\": \"Padel 1\", \"maxStudents\": 4}]
}" "$token")
id=$(echo "$id" | tr -d '"')
if [ -n "$id" ] && [ "$id" != "null" ]; then
    seriesIds+=("$id")
    echo "   Created: Padel Introductie"
fi

# 5. Add lessons to each series
echo ""
echo "5. Adding lessons to series..."

courts=("Baan 1" "Baan 2" "Baan 3" "Padel 1")
startTimes=("09:00" "10:30" "14:00" "16:00")
endTimes=("10:00" "12:00" "15:00" "17:00")

for sid in "${seriesIds[@]}"; do
    for week in $(seq 0 7); do
        days=$(( (week * 7) + 1 ))
        lessonDate=$(date -v+"${days}d" +%Y-%m-%d 2>/dev/null || date -d "+${days} days" +%Y-%m-%d)
        courtIdx=$(( week % ${#courts[@]} ))
        timeIdx=$(( week % ${#startTimes[@]} ))
        court="${courts[$courtIdx]}"
        startTime="${startTimes[$timeIdx]}"
        endTime="${endTimes[$timeIdx]}"

        invoke_api POST "/lessonseries/$sid/lessons" "{
            \"trainerId\": \"$trainerId\",
            \"date\": \"$lessonDate\",
            \"startTime\": \"$startTime\",
            \"endTime\": \"$endTime\",
            \"courtName\": \"$court\",
            \"maxStudents\": 4
        }" "$token" > /dev/null
    done
    echo "   Added 8 lessons to series"
done

# 6. Create enrollments
echo ""
echo "6. Creating enrollments..."

studentNames=("Emma Claes" "Lucas Peeters" "Lotte Van Damme" "Noah Willems" "Julie Maes" "Axel Dubois" "Sarah Jacobs" "Thomas Hermans" "Marie Lambert" "Bram Wouters")
studentEmails=("emma.claes@gmail.com" "lucas.peeters@hotmail.com" "lotte.vd@outlook.com" "noah.w@gmail.com" "julie.maes@yahoo.com" "axel.dubois@gmail.com" "sarah.j@hotmail.com" "thomas.h@outlook.com" "marie.lambert@gmail.com" "bram.wouters@hotmail.com")
studentPhones=("+32471234567" "+32472345678" "+32473456789" "+32474567890" "+32475678901" "+32476789012" "+32477890123" "+32478901234" "+32479012345" "+32470123456")

enrollCount=0
for i in $(seq 0 9); do
    seriesIdx=$(( i % ${#seriesIds[@]} ))
    targetSeries="${seriesIds[$seriesIdx]}"

    result=$(invoke_api POST "/public/lessonseries/$targetSeries/enroll" "{
        \"studentName\": \"${studentNames[$i]}\",
        \"studentEmail\": \"${studentEmails[$i]}\",
        \"studentPhone\": \"${studentPhones[$i]}\",
        \"responses\": []
    }")
    if [ -n "$result" ] && [ "$result" != "null" ]; then
        enrollCount=$((enrollCount + 1))
    fi
done
echo "   Created $enrollCount enrollments"

# 7. Create a realistic planning-ready series
echo ""
echo "7. Creating Zomerlessen 2026..."

# Generate lessons JSON from weekly template
# Realistic club schedule: weekday evenings + Wednesday/Saturday afternoon
lessonsJson=$(python3 << PYEOF2
import json
from datetime import datetime, timedelta

start = datetime.strptime("$today", "%Y-%m-%d")
end = datetime.strptime("$endDate", "%Y-%m-%d")

template = [
    (1, "18:00", "19:00", "Baan 1", 4),   # Di 18:00 — after work/school
    (1, "18:00", "19:00", "Baan 2", 4),   # Di 18:00 — parallel court
    (1, "19:00", "20:00", "Baan 1", 4),   # Di 19:00 — evening slot
    (2, "14:00", "15:00", "Baan 1", 4),   # Wo 14:00 — Wed afternoon (kids)
    (2, "14:00", "15:00", "Baan 2", 4),   # Wo 14:00 — parallel court
    (2, "15:00", "16:00", "Baan 1", 4),   # Wo 15:00 — second wave
    (3, "18:00", "19:00", "Baan 1", 4),   # Do 18:00 — after work
    (3, "19:00", "20:00", "Baan 1", 4),   # Do 19:00 — evening
    (5, "10:00", "11:00", "Baan 1", 4),   # Za 10:00 — Saturday morning
    (5, "10:00", "11:00", "Baan 2", 4),   # Za 10:00 — parallel court
    (5, "11:00", "12:00", "Baan 1", 4),   # Za 11:00 — late morning
]

lessons = []
dow = start.weekday()
week_start = start - timedelta(days=dow)
current = week_start

while current <= end:
    for day_of_week, st, et, court, ms in template:
        lesson_date = current + timedelta(days=day_of_week)
        if lesson_date < start or lesson_date > end:
            continue
        lessons.append({
            "date": lesson_date.strftime("%Y-%m-%d"),
            "startTime": st,
            "endTime": et,
            "courtName": court,
            "maxStudents": ms,
        })
    current += timedelta(days=7)

print(json.dumps(lessons))
PYEOF2
)

planningSeriesId=$(invoke_api POST "/lessonseries" "{
    \"tennisClubId\": \"$clubId\",
    \"name\": \"Zomerlessen 2026\",
    \"description\": \"Tennislessen voor jeugd en volwassenen. 3 maanden, 2 banen, meerdere momenten per week.\",
    \"level\": 1, \"price\": 180.00,
    \"startDate\": \"$today\", \"endDate\": \"$endDate\",
    \"registrationDeadline\": \"$deadline\", \"maxRegistrations\": 40,
    \"weeklyTemplate\": [
        {\"dayOfWeek\": 1, \"startTime\": \"18:00\", \"endTime\": \"19:00\", \"trainerId\": null, \"courtName\": \"Baan 1\", \"maxStudents\": 4},
        {\"dayOfWeek\": 1, \"startTime\": \"18:00\", \"endTime\": \"19:00\", \"trainerId\": null, \"courtName\": \"Baan 2\", \"maxStudents\": 4},
        {\"dayOfWeek\": 1, \"startTime\": \"19:00\", \"endTime\": \"20:00\", \"trainerId\": null, \"courtName\": \"Baan 1\", \"maxStudents\": 4},
        {\"dayOfWeek\": 2, \"startTime\": \"14:00\", \"endTime\": \"15:00\", \"trainerId\": null, \"courtName\": \"Baan 1\", \"maxStudents\": 4},
        {\"dayOfWeek\": 2, \"startTime\": \"14:00\", \"endTime\": \"15:00\", \"trainerId\": null, \"courtName\": \"Baan 2\", \"maxStudents\": 4},
        {\"dayOfWeek\": 2, \"startTime\": \"15:00\", \"endTime\": \"16:00\", \"trainerId\": null, \"courtName\": \"Baan 1\", \"maxStudents\": 4},
        {\"dayOfWeek\": 3, \"startTime\": \"18:00\", \"endTime\": \"19:00\", \"trainerId\": null, \"courtName\": \"Baan 1\", \"maxStudents\": 4},
        {\"dayOfWeek\": 3, \"startTime\": \"19:00\", \"endTime\": \"20:00\", \"trainerId\": null, \"courtName\": \"Baan 1\", \"maxStudents\": 4},
        {\"dayOfWeek\": 5, \"startTime\": \"10:00\", \"endTime\": \"11:00\", \"trainerId\": null, \"courtName\": \"Baan 1\", \"maxStudents\": 4},
        {\"dayOfWeek\": 5, \"startTime\": \"10:00\", \"endTime\": \"11:00\", \"trainerId\": null, \"courtName\": \"Baan 2\", \"maxStudents\": 4},
        {\"dayOfWeek\": 5, \"startTime\": \"11:00\", \"endTime\": \"12:00\", \"trainerId\": null, \"courtName\": \"Baan 1\", \"maxStudents\": 4}
    ],
    \"lessons\": $lessonsJson
}" "$token")
planningSeriesId=$(echo "$planningSeriesId" | tr -d '"')

if [ -z "$planningSeriesId" ] || [ "$planningSeriesId" = "null" ]; then
    echo "   Failed to create planning series."
else
    echo "   Created: Zomerlessen 2026 ($planningSeriesId)"

    # Fetch time slot IDs
    echo "   Fetching time slots..."
    slotsJson=$(invoke_api GET "/public/lessonseries/$planningSeriesId/timeslots" "" "")
    slotIds=$(echo "$slotsJson" | python3 -c "
import sys, json
data = json.load(sys.stdin)
for s in sorted(data, key=lambda x: (x['dayOfWeek'], x['startTime'], x.get('courtName',''))):
    print(s['id'])
" 2>/dev/null)

    IFS=$'\n' read -r -d '' -a s <<< "$slotIds"
    echo "   Found ${#s[@]} time slots"

    # Slots (sorted by day+time+court):
    #  0: Di 18:00 B1    3: Wo 14:00 B1    6: Do 18:00 B1    8: Za 10:00 B1
    #  1: Di 18:00 B2    4: Wo 14:00 B2    7: Do 19:00 B1    9: Za 10:00 B2
    #  2: Di 19:00 B1    5: Wo 15:00 B1                     10: Za 11:00 B1

    echo ""
    echo "8. Creating enrollments..."

    # ── FAMILY: De Boer (parent + 2 kids) ──
    # Wed afternoon only (kids have school), open to merging with other kids
    echo "   Family De Boer (group of 3, Wed afternoon, open to merge)..."
    invoke_api POST "/public/lessonseries/$planningSeriesId/enroll" "{
        \"studentName\": \"Sofie De Boer\", \"studentEmail\": \"sofie.deboer@gmail.com\", \"studentPhone\": \"+32478112233\",
        \"enrollmentType\": \"group\", \"isOpenToGrouping\": true,
        \"timeSlotPreferences\": [
            {\"weeklyTemplateEntryId\": \"${s[0]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[1]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[2]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[3]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[4]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[5]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[6]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[7]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[8]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[9]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[10]}\", \"preference\": 3}
        ],
        \"groupMembers\": [
            {\"studentName\": \"Fien De Boer\", \"studentEmail\": \"sofie.deboer+fien@gmail.com\", \"studentPhone\": null, \"responses\": []},
            {\"studentName\": \"Stan De Boer\", \"studentEmail\": \"sofie.deboer+stan@gmail.com\", \"studentPhone\": null, \"responses\": []}
        ],
        \"responses\": []
    }" > /dev/null

    # ── COUPLE: Peeters-Janssens (2 adults) ──
    # Work full-time, only evenings + Saturday, exclusive (want to play together, not with strangers)
    echo "   Couple Peeters-Janssens (group of 2, evenings only, exclusive)..."
    invoke_api POST "/public/lessonseries/$planningSeriesId/enroll" "{
        \"studentName\": \"Bart Peeters\", \"studentEmail\": \"bart.peeters@outlook.be\", \"studentPhone\": \"+32479223344\",
        \"enrollmentType\": \"group\", \"isOpenToGrouping\": false,
        \"timeSlotPreferences\": [
            {\"weeklyTemplateEntryId\": \"${s[0]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[1]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[2]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[3]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[4]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[5]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[6]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[7]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[8]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[9]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[10]}\", \"preference\": 2}
        ],
        \"groupMembers\": [
            {\"studentName\": \"Lisa Janssens\", \"studentEmail\": \"lisa.janssens@outlook.be\", \"studentPhone\": \"+32479223345\", \"responses\": []}
        ],
        \"responses\": []
    }" > /dev/null

    # ── SOLO: Emma Van Acker ──
    # Student, flexible, prefers Wed afternoon + Saturday, happy to be grouped
    echo "   Emma Van Acker (solo, open, Wed+Sat)..."
    invoke_api POST "/public/lessonseries/$planningSeriesId/enroll" "{
        \"studentName\": \"Emma Van Acker\", \"studentEmail\": \"emma.vanacker@student.be\", \"studentPhone\": \"+32468001122\",
        \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
        \"timeSlotPreferences\": [
            {\"weeklyTemplateEntryId\": \"${s[0]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[1]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[2]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[3]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[4]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[5]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[6]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[7]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[8]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[9]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[10]}\", \"preference\": 1}
        ],
        \"responses\": []
    }" > /dev/null

    # ── SOLO: Thomas Wouters ──
    # Also a student, prefers Wed afternoon — should auto-merge with Emma
    echo "   Thomas Wouters (solo, open, Wed+Sat — auto-merge candidate)..."
    invoke_api POST "/public/lessonseries/$planningSeriesId/enroll" "{
        \"studentName\": \"Thomas Wouters\", \"studentEmail\": \"thomas.w@student.be\", \"studentPhone\": \"+32468003344\",
        \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
        \"timeSlotPreferences\": [
            {\"weeklyTemplateEntryId\": \"${s[0]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[1]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[2]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[3]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[4]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[5]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[6]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[7]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[8]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[9]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[10]}\", \"preference\": 2}
        ],
        \"responses\": []
    }" > /dev/null

    # ── SOLO: Noor Hendrickx ──
    # Works part-time, prefers Tuesday evening + Thursday, open to grouping
    echo "   Noor Hendrickx (solo, open, Tue+Thu evenings)..."
    invoke_api POST "/public/lessonseries/$planningSeriesId/enroll" "{
        \"studentName\": \"Noor Hendrickx\", \"studentEmail\": \"noor.h@telenet.be\", \"studentPhone\": \"+32473556677\",
        \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
        \"timeSlotPreferences\": [
            {\"weeklyTemplateEntryId\": \"${s[0]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[1]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[2]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[3]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[4]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[5]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[6]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[7]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[8]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[9]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[10]}\", \"preference\": 3}
        ],
        \"responses\": []
    }" > /dev/null

    # ── SOLO: Pieter Vermeersch ──
    # Retiree, very flexible, happy on any slot, open to grouping
    echo "   Pieter Vermeersch (solo, open, flexible retiree)..."
    invoke_api POST "/public/lessonseries/$planningSeriesId/enroll" "{
        \"studentName\": \"Pieter Vermeersch\", \"studentEmail\": \"pieter.verm@skynet.be\", \"studentPhone\": \"+32475889900\",
        \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
        \"timeSlotPreferences\": [
            {\"weeklyTemplateEntryId\": \"${s[0]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[1]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[2]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[3]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[4]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[5]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[6]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[7]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[8]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[9]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[10]}\", \"preference\": 1}
        ],
        \"responses\": []
    }" > /dev/null

    # ── SOLO: Sarah Dubois ──
    # Nurse, irregular schedule, only Thursday + Saturday available, NOT open to grouping
    echo "   Sarah Dubois (solo, exclusive, Thu+Sat only)..."
    invoke_api POST "/public/lessonseries/$planningSeriesId/enroll" "{
        \"studentName\": \"Sarah Dubois\", \"studentEmail\": \"sarah.dubois@uzleuven.be\", \"studentPhone\": \"+32471990011\",
        \"enrollmentType\": \"solo\", \"isOpenToGrouping\": false,
        \"timeSlotPreferences\": [
            {\"weeklyTemplateEntryId\": \"${s[0]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[1]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[2]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[3]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[4]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[5]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[6]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[7]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[8]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[9]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[10]}\", \"preference\": 1}
        ],
        \"responses\": []
    }" > /dev/null

    # ── SOLO: Kevin Mertens ──
    # Works shifts, marked everything unavailable — CONFLICT, admin must call him
    echo "   Kevin Mertens (solo, exclusive, ALL unavailable = CONFLICT)..."
    invoke_api POST "/public/lessonseries/$planningSeriesId/enroll" "{
        \"studentName\": \"Kevin Mertens\", \"studentEmail\": \"kevin.m@proximus.be\", \"studentPhone\": \"+32476001122\",
        \"enrollmentType\": \"solo\", \"isOpenToGrouping\": false,
        \"timeSlotPreferences\": [
            {\"weeklyTemplateEntryId\": \"${s[0]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[1]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[2]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[3]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[4]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[5]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[6]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[7]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[8]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[9]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[10]}\", \"preference\": 3}
        ],
        \"responses\": []
    }" > /dev/null

    # ── SOLO: Ines Claes ──
    # Late enrollee, didn't fill in preferences — CONFLICT (no prefs at all)
    echo "   Ines Claes (solo, open, NO preferences = CONFLICT)..."
    invoke_api POST "/public/lessonseries/$planningSeriesId/enroll" "{
        \"studentName\": \"Ines Claes\", \"studentEmail\": \"ines.claes@hotmail.com\", \"studentPhone\": \"+32479112233\",
        \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
        \"timeSlotPreferences\": [],
        \"responses\": []
    }" > /dev/null

    # ── SOLO: Jules Van Damme ──
    # Saturday morning only, open to grouping — limited options
    echo "   Jules Van Damme (solo, open, Saturday only)..."
    invoke_api POST "/public/lessonseries/$planningSeriesId/enroll" "{
        \"studentName\": \"Jules Van Damme\", \"studentEmail\": \"jules.vd@gmail.com\", \"studentPhone\": \"+32478334455\",
        \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
        \"timeSlotPreferences\": [
            {\"weeklyTemplateEntryId\": \"${s[0]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[1]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[2]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[3]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[4]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[5]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[6]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[7]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[8]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[9]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[10]}\", \"preference\": 2}
        ],
        \"responses\": []
    }" > /dev/null

    # ── SOLO: Marie-Claire Vos ──
    # Prefers Tuesday evening, open to grouping — auto-merge candidate with Noor
    echo "   Marie-Claire Vos (solo, open, Tue evening — auto-merge candidate)..."
    invoke_api POST "/public/lessonseries/$planningSeriesId/enroll" "{
        \"studentName\": \"Marie-Claire Vos\", \"studentEmail\": \"mc.vos@gmail.com\", \"studentPhone\": \"+32474556677\",
        \"enrollmentType\": \"solo\", \"isOpenToGrouping\": true,
        \"timeSlotPreferences\": [
            {\"weeklyTemplateEntryId\": \"${s[0]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[1]}\", \"preference\": 2},
            {\"weeklyTemplateEntryId\": \"${s[2]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[3]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[4]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[5]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[6]}\", \"preference\": 1},
            {\"weeklyTemplateEntryId\": \"${s[7]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[8]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[9]}\", \"preference\": 3},
            {\"weeklyTemplateEntryId\": \"${s[10]}\", \"preference\": 3}
        ],
        \"responses\": []
    }" > /dev/null

    echo ""
    echo "   Done — 13 enrollments:"
    echo "     2 groups: De Boer family (3, Wed+Sat, open) + Peeters-Janssens couple (2, evenings, exclusive)"
    echo "     9 solos: 6 open to grouping, 3 exclusive"
    echo "   Expected auto-merges: Emma+Thomas (Wed/Sat), Noor+Marie-Claire (Tue evening)"
    echo "   Expected conflicts: Kevin (all unavailable), Ines (no preferences)"
    echo "   Capacity pressure: Tue 18:00 popular, some slots oversaturated"
fi

# Done
echo ""
echo "=== Seed Complete ==="
echo ""
echo "Login credentials:"
echo "  Email:    jan@deaces.be"
echo "  Password: Demo1234!"
echo ""
echo "URLs:"
echo "  Frontend:  http://localhost:5317"
echo "  API:       http://localhost:5142/swagger"
echo "  Email:     http://localhost:3001"
echo ""
echo "Planning test series: Zomerlessen 2026"
echo "  6 time slots (Ma/Wo/Vr), 2 trainers, 2 courts"
echo "  2 groups (1 open to merge, 1 exclusive)"
echo "  7 solos (5 open to grouping, 2 exclusive)"
echo ""
