# ICS calendar download fix

## Goal
Make the confirmation-page calendar download work on Android and iOS instead of downloading the frontend HTML 404 page.

## Scope
- Keep `/api` in the frontend-generated calendar URL.
- Return the backend ICS payload as an attachment with an explicit `calendar.ics` filename.
- Add a backend regression test for calendar download headers and body.
- Verify frontend lint/build and the targeted backend test.

## TDD
1. Add the backend regression test and run it RED.
2. Implement the smallest backend response change and run it GREEN.
3. Correct the frontend URL and run the relevant verification.

## Out of scope
Optional iCalendar metadata hardening (`METHOD`, `CALSCALE`, line folding) is not required for the reported `.ics.html` failure and is intentionally separate.
