# Demo Readiness Report

Status report on what's built, what was fixed, and what remains before CoachOS can be demoed to potential clients.

## What's Built (Fully Functional)

### Backend (.NET 10, Clean Architecture)

| Feature | Endpoints | Status |
|---------|-----------|--------|
| **Authentication** | Register, Login (JWT) | Done |
| **Lesson Series** | CRUD + add/delete lessons | Done |
| **Trainer Management** | Invite, accept, deactivate, remove, reassign | Done |
| **Tennis Clubs** | CRUD with "in use" protection | Done |
| **Public Enrollment** | Anonymous enrollment with dynamic form builder | Done |
| **Email** | Trainer invites, enrollment confirmations (SMTP) | Done |

- 27 API endpoints across 6 feature areas
- Multi-tenant (OrganizationId on every query)
- FluentValidation on all request DTOs
- Result\<T\> error handling (no exceptions for business logic)
- 8 EF Core migrations applied
- 3 unit test classes

### Frontend (Next.js 16, TypeScript)

| Page | Route | Status |
|------|-------|--------|
| Login | `/login` | Done |
| Register | `/register` | Done |
| Trainer Invite | `/invite/[token]` | Done |
| Dashboard | `/dashboard` | Done (stats hardcoded) |
| Lesson Series List | `/dashboard/lessons` | Done |
| Create Lesson Series | `/dashboard/lessons/new` | Done |
| Lesson Series Detail | `/dashboard/lessons/[id]` | Done |
| Trainer Management | `/dashboard/trainers` | Done |
| Settings (Tennis Clubs) | `/dashboard/settings` | Done |
| Public Enrollment | `/enroll/[seriesId]` | Done |

- React Query for data fetching
- React Hook Form + Zod validation
- Full Dutch i18n (137+ translation keys)
- Tennis branding (green/lime/beige palette)
- Responsive layout with sidebar + mobile bottom nav

## What Was Fixed (This Branch)

### Issue #11 — Home page redirect
- **Before:** `/` showed Next.js boilerplate ("edit page.tsx")
- **After:** `/` redirects to `/login`

### Issue #10 — Route protection middleware
- **Before:** No server-side route protection. Anyone could navigate to `/dashboard/*` without auth.
- **After:** `middleware.ts` redirects unauthenticated users to `/login` and authenticated users away from `/login`/`/register`. Uses a `has_token` cookie synced with localStorage.

### E2E Test Suite (Playwright)
- 50 tests across 8 test files covering all existing flows
- All tests use API mocking (no running backend needed)
- Tests cover: auth, middleware, dashboard, lessons CRUD, trainers, settings, enrollment, navigation

## What Remains (Open Issues)

### Blockers
| Issue | Description | Effort |
|-------|-------------|--------|
| [#9](https://github.com/EloyB/coach-os/issues/9) | CORS origin mismatch | 5 min |

> Note: CORS was verified to already be correct (port 5317 matches frontend dev server). Issue can be closed.

### High Priority (Demo Quality)
| Issue | Description | Effort |
|-------|-------------|--------|
| [#12](https://github.com/EloyB/coach-os/issues/12) | Dashboard summary API + frontend wiring | 1-2 hrs |
| [#13](https://github.com/EloyB/coach-os/issues/13) | `/my-lessons` page for student role | 30 min - 2 hrs |
| [#14](https://github.com/EloyB/coach-os/issues/14) | Seed data script for demo environment | 1 hr |

### Nice-to-Have (Polish)
- Success toasts after create/delete operations
- Custom 404/500 error pages
- Dashboard stat cards wired to real data
- Pagination on list endpoints

## Running the E2E Tests

```bash
cd frontend

# Headless (CI)
npm run test:e2e

# Watch tests in a browser
npm run test:e2e:headed

# View HTML report from last run
npm run test:e2e:report
```

## Architecture Summary

```
Backend (Clean Architecture)
  API → Infrastructure → Application → Domain

Frontend (Next.js App Router)
  (auth)/ — login, register, invite
  (dashboard)/ — protected routes with sidebar layout
  (public)/ — enrollment (no auth required)
```
