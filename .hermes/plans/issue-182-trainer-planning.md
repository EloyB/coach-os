# Issue #182 — read-only trainerplanning

- Issue: https://github.com/EloyB/coach-os/issues/182
- Branch: `feat/issue-182-trainer-planning`
- Scope: actieve trainers kunnen alle planningen van hun organisatie read-only bekijken.

## Implementation

- Backend read-only endpoint `GET /api/trainer/planning`.
- Organisatie-scope via de JWT `OrganizationId` claim; Admin en Trainer mogen lezen.
- Alle planningstatussen en alle organisatiereeksen inbegrepen.
- Trainer-DTO bevat leerlingnamen, maar geen e-mailadressen, telefoonnummers of voorkeuren.
- Bestaande admin-mutatie-endpoints blijven ongewijzigd.
- Frontendpagina `/dashboard/planning` met desktop- en mobiele navigatie.

## Verification

- Failing backend regression test first, then green implementation.
- Full backend test suite.
- Frontend production build.
- Targeted ESLint on changed frontend files.
- Real Playwright screenshot of `/dashboard/planning` with mocked API responses; mock usage explicitly documented in the PR.
