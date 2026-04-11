#!/bin/bash
#
# Smoke tests for the Group Scheduling / Planning feature.
# Prerequisite: API running + seed-demo-data.sh already executed.
#

API_BASE="${1:-http://localhost:5142/api}"
PASS=0
FAIL=0

invoke_api() {
    local method="$1"
    local path="$2"
    local body="$3"
    local token="$4"

    local headers=(-H "Content-Type: application/json")
    if [ -n "$token" ]; then
        headers+=(-H "Authorization: Bearer $token")
    fi

    local args=(-s -w "\n%{http_code}" -X "$method" "${headers[@]}")
    if [ -n "$body" ]; then
        args+=(-d "$body")
    fi

    curl "${args[@]}" "${API_BASE}${path}" 2>/dev/null
}

LAST_BODY=""
check() {
    local test_name="$1"
    local expected_code="$2"
    local response="$3"
    local actual_code
    actual_code=$(echo "$response" | tail -1)
    LAST_BODY=$(echo "$response" | sed '$d')

    if [ "$actual_code" == "$expected_code" ]; then
        echo "  PASS: $test_name (HTTP $actual_code)"
        PASS=$((PASS + 1))
    else
        echo "  FAIL: $test_name (expected $expected_code, got $actual_code)"
        echo "        Body: $(echo "$LAST_BODY" | head -3)"
        FAIL=$((FAIL + 1))
    fi
}

echo ""
echo "=== Planning Feature Smoke Tests ==="
echo "API: $API_BASE"
echo ""

# --- Auth ---
echo "--- Setup: Authenticating ---"
auth_resp=$(invoke_api POST "/auth/login" '{"email": "jan@deaces.be", "password": "Demo1234!"}')
auth_body=$(echo "$auth_resp" | sed '$d')
token=$(echo "$auth_body" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))" 2>/dev/null)

if [ -z "$token" ]; then
    echo "  Cannot authenticate. Exiting."
    exit 1
fi
echo "  Authenticated as admin"

# --- Get a series ID ---
series_resp=$(invoke_api GET "/lessonseries" "" "$token")
series_body=$(echo "$series_resp" | sed '$d')
seriesId=$(echo "$series_body" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(data[0]['id'] if data else '')
" 2>/dev/null)

if [ -z "$seriesId" ]; then
    echo "  No series found. Exiting."
    exit 1
fi
echo "  Using series: $seriesId"
echo ""

# --- Test 1: GET public time slots ---
echo "--- Test 1: GET /public/lessonseries/{id}/timeslots ---"
resp=$(invoke_api GET "/public/lessonseries/$seriesId/timeslots")
check "Public time slots" "200" "$resp"
slotCount=$(echo "$LAST_BODY" | python3 -c "import sys,json; print(len(json.load(sys.stdin)))" 2>/dev/null)
echo "  Slots returned: $slotCount"

slotId=$(echo "$LAST_BODY" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(data[0]['id'] if data else '')
" 2>/dev/null)
echo ""

# --- Test 2: Enroll solo with preferences ---
echo "--- Test 2: POST /public/lessonseries/{id}/enroll (solo + preferences) ---"
resp=$(invoke_api POST "/public/lessonseries/$seriesId/enroll" "{
    \"studentName\": \"Test Solo Student\",
    \"studentEmail\": \"solo.test@smoke.com\",
    \"responses\": [],
    \"enrollmentType\": \"solo\",
    \"isOpenToGrouping\": true,
    \"timeSlotPreferences\": [{\"weeklyTemplateEntryId\": \"$slotId\", \"preference\": 2}]
}")
check "Solo enrollment with preferences" "200" "$resp"
echo ""

# --- Test 3: Enroll another solo (open to grouping) for manual group test ---
echo "--- Test 2b: Enrolling second solo for group test ---"
resp=$(invoke_api POST "/public/lessonseries/$seriesId/enroll" "{
    \"studentName\": \"Test Solo Two\",
    \"studentEmail\": \"solo2.test@smoke.com\",
    \"responses\": [],
    \"enrollmentType\": \"solo\",
    \"isOpenToGrouping\": true,
    \"timeSlotPreferences\": [{\"weeklyTemplateEntryId\": \"$slotId\", \"preference\": 2}]
}")
check "Second solo enrollment" "200" "$resp"
echo ""

# --- Test 3: Enroll group with preferences ---
echo "--- Test 3: POST /public/lessonseries/{id}/enroll (group) ---"
resp=$(invoke_api POST "/public/lessonseries/$seriesId/enroll" "{
    \"studentName\": \"Group Leader\",
    \"studentEmail\": \"group.leader@smoke.com\",
    \"responses\": [],
    \"enrollmentType\": \"group\",
    \"isOpenToGrouping\": false,
    \"groupMembers\": [
        {\"studentName\": \"Member One\", \"studentEmail\": \"member1@smoke.com\"},
        {\"studentName\": \"Member Two\", \"studentEmail\": \"member2@smoke.com\"}
    ],
    \"timeSlotPreferences\": [{\"weeklyTemplateEntryId\": \"$slotId\", \"preference\": 2}]
}")
check "Group enrollment with preferences" "200" "$resp"
echo ""

# --- Test 4: GET enrollments with preferences (admin) ---
echo "--- Test 4: GET /lessonseries/{id}/enrollments/planning ---"
resp=$(invoke_api GET "/lessonseries/$seriesId/enrollments/planning" "" "$token")
check "Enrollments with preferences" "200" "$resp"
enrollCount=$(echo "$LAST_BODY" | python3 -c "import sys,json; print(len(json.load(sys.stdin)))" 2>/dev/null)
echo "  Enrollments with preferences: $enrollCount"

soloIds=$(echo "$LAST_BODY" | python3 -c "
import sys, json
data = json.load(sys.stdin)
solos = [e['id'] for e in data if not e.get('enrollmentGroupId') and e.get('isOpenToGrouping')]
print(','.join(solos[:2]) if len(solos) >= 2 else '')
" 2>/dev/null)
echo ""

# --- Test 5: POST generate planning proposal ---
echo "--- Test 5: POST /lessonseries/{id}/planning/generate ---"
resp=$(invoke_api POST "/lessonseries/$seriesId/planning/generate" "" "$token")
check "Generate planning proposal" "200" "$resp"
slotAssignments=$(echo "$LAST_BODY" | python3 -c "
import sys, json
data = json.load(sys.stdin)
total = sum(len(s.get('assignments', [])) for s in data.get('slots', []))
unassigned = len(data.get('unassigned', []))
print(f'{total} assigned, {unassigned} unassigned')
" 2>/dev/null)
echo "  Result: $slotAssignments"
echo ""

# --- Test 6: GET planning overview ---
echo "--- Test 6: GET /lessonseries/{id}/planning ---"
resp=$(invoke_api GET "/lessonseries/$seriesId/planning" "" "$token")
check "Get planning overview" "200" "$resp"
planningStatus=$(echo "$LAST_BODY" | python3 -c "import sys,json; print(json.load(sys.stdin).get('planningStatus',''))" 2>/dev/null)
echo "  Planning status: $planningStatus"

assignmentId=$(echo "$LAST_BODY" | python3 -c "
import sys, json
data = json.load(sys.stdin)
for slot in data.get('slots', []):
    for a in slot.get('assignments', []):
        if a.get('enrollmentId'):
            print(a['assignmentId'])
            exit()
print('')
" 2>/dev/null)

secondSlotId=$(echo "$LAST_BODY" | python3 -c "
import sys, json
data = json.load(sys.stdin)
slots = data.get('slots', [])
print(slots[0]['weeklyTemplateEntryId'] if slots else '')
" 2>/dev/null)
echo ""

# --- Test 7: PUT update assignment ---
echo "--- Test 7: PUT /lessonseries/{id}/planning/assignments/{assignmentId} ---"
if [ -n "$assignmentId" ] && [ -n "$secondSlotId" ]; then
    resp=$(invoke_api PUT "/lessonseries/$seriesId/planning/assignments/$assignmentId" "{
        \"weeklyTemplateEntryId\": \"$secondSlotId\"
    }" "$token")
    check "Update assignment" "200" "$resp"
else
    echo "  SKIP: No assignment to move"
fi
echo ""

# --- Test 8: POST create manual group ---
echo "--- Test 8: POST /lessonseries/{id}/planning/groups ---"
manualGroupId=""
if [ -n "$soloIds" ]; then
    id1=$(echo "$soloIds" | cut -d',' -f1)
    id2=$(echo "$soloIds" | cut -d',' -f2)
    if [ -n "$id1" ] && [ -n "$id2" ]; then
        resp=$(invoke_api POST "/lessonseries/$seriesId/planning/groups" "{
            \"enrollmentIds\": [\"$id1\", \"$id2\"]
        }" "$token")
        check "Create manual group" "201" "$resp"
        manualGroupId=$(echo "$LAST_BODY" | tr -d '"')
    else
        echo "  SKIP: Not enough solo enrollments for grouping"
    fi
else
    echo "  SKIP: No solo enrollments available for grouping"
fi
echo ""

# --- Test 9: DELETE dissolve group ---
echo "--- Test 9: DELETE /lessonseries/{id}/planning/groups/{groupId} ---"
if [ -n "$manualGroupId" ] && [ "$manualGroupId" != "null" ]; then
    resp=$(invoke_api DELETE "/lessonseries/$seriesId/planning/groups/$manualGroupId" "" "$token")
    check "Dissolve group" "204" "$resp"
else
    echo "  SKIP: No group to dissolve"
fi
echo ""

# --- Test 10: POST confirm schedule ---
echo "--- Test 10: POST /lessonseries/{id}/planning/confirm ---"
resp=$(invoke_api POST "/lessonseries/$seriesId/planning/confirm" "" "$token")
check "Confirm schedule" "200" "$resp"

# Verify lessons were generated
echo ""
echo "--- Verification: GET /lessonseries/{id} (check lesson count) ---"
resp=$(invoke_api GET "/lessonseries/$seriesId" "" "$token")
verifyBody=$(echo "$resp" | sed '$d')
lessonCount=$(echo "$verifyBody" | python3 -c "import sys,json; print(json.load(sys.stdin).get('lessonCount', 0))" 2>/dev/null)
echo "  Total lessons after confirm: $lessonCount"
echo ""

# --- Summary ---
echo "=============================="
echo "  PASSED: $PASS"
echo "  FAILED: $FAIL"
echo "=============================="
echo ""

if [ "$FAIL" -gt 0 ]; then
    exit 1
fi
