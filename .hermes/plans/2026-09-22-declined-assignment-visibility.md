# Fix declined assignments disappearing from planning

## Goal
Ensure a participant whose proposed assignment was declined remains visible in the planning sidebar as unassigned, so a trainer can assign a new slot. Preserve the existing alternative-slot confirmation flow.

## Scope
- Add a Playwright regression test for a solo enrollment with a `Declined` assignment.
- Change the planning page's assigned-enrollment/group derived sets to ignore declined assignments, matching calendar rendering and capacity semantics.
- Add no backend/data mutation.

## TDD
1. Add the focused planning regression and run it RED against `origin/main`.
2. Apply the minimal frontend filter fix.
3. Run the focused regression GREEN.
4. Run the affected planning spec, targeted ESLint, and production frontend build.

## PR evidence
- Explain the production symptom and the declined-confirmation/alternative flow distinction.
- Include screenshot evidence only if a real app route can be rendered honestly; otherwise document that UI screenshot capture was not available.
