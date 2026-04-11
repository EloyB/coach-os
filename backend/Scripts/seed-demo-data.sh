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

enrollCount=0
for i in $(seq 0 9); do
    seriesIdx=$(( i % ${#seriesIds[@]} ))
    targetSeries="${seriesIds[$seriesIdx]}"

    result=$(invoke_api POST "/public/lessonseries/$targetSeries/enroll" "{
        \"studentName\": \"${studentNames[$i]}\",
        \"studentEmail\": \"${studentEmails[$i]}\",
        \"responses\": []
    }")
    if [ -n "$result" ] && [ "$result" != "null" ]; then
        enrollCount=$((enrollCount + 1))
    fi
done
echo "   Created $enrollCount enrollments"

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
