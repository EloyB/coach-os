# Demo Readiness Report

Status report on what's built, what was fixed, and what remains before CoachOS can be demoed to potential clients.

## Quick Start (Docker)

```bash
# Start all services
docker compose up

# Run migrations (first time or after DB reset)
dotnet ef database update --project backend/CoachOS.Infrastructure --startup-project backend/CoachOS.API

# Seed demo data
.\backend\Scripts\seed-demo-data.ps1
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:5317 |
| API + Swagger | http://localhost:5142/swagger |
| Email inbox | http://localhost:3001 |
| pgAdmin (opt-in) | http://localhost:5050 |

**Demo login:** `jan@deaces.be` / `Demo1234!`

To include pgAdmin: `docker compose --profile tools up`

## What's Built (Fully Functional)

### Backend (.NET 10, Clean Architecture)

| Feature | Endpoints | Status |
|---------|-----------|--------|
| **Authentication** | Register, Login (JWT) | Done |
| **Dashboard** | Summary with counts + upcoming lessons | Done |
| **Lesson Series** | CRUD + add/delete lessons | Done |
| **Trainer Management** | Invite, accept, deactivate, remove, reassign | Done |
| **Tennis Clubs** | CRUD with "in use" protection | Done |
| **Public Enrollment** | Anonymous enrollment with dynamic form builder | Done |
| **Email** | Trainer invites, enrollment confirmations (SMTP) | Done |

- 28 API endpoints across 7 feature areas
- Multi-tenant (OrganizationId on every query)
- FluentValidation on all request DTOs
- Result\<T\> error handling (no exceptions for business logic)
- 8 EF Core migrations applied
- 37 unit tests

### Frontend (Next.js 16, TypeScript)

| Page | Route | Status |
|------|-------|--------|
| Home | `/` | Redirects to `/login` |
| Login | `/login` | Done |
| Register | `/register` | Done |
| Trainer Invite | `/invite/[token]` | Done |
| Dashboard | `/dashboard` | Done (live API data) |
| Lesson Series List | `/dashboard/lessons` | Done |
| Create Lesson Series | `/dashboard/lessons/new` | Done |
| Lesson Series Detail | `/dashboard/lessons/[id]` | Done |
| Trainer Management | `/dashboard/trainers` | Done |
| Settings (Tennis Clubs) | `/dashboard/settings` | Done |
| Public Enrollment | `/enroll/[seriesId]` | Done |

- React Query for data fetching with loading skeletons
- React Hook Form + Zod validation
- Full Dutch i18n (137+ translation keys)
- Tennis branding (green/lime/beige palette)
- Responsive layout with sidebar + mobile bottom nav
- Route protection middleware (cookie-based auth check)
- Dynamic user info in topbar (name, role, initials)

## What Was Fixed (This Branch)

### Issue #9 — CORS origin mismatch
- **Result:** Verified already correct (port 5317 matches frontend dev server). Closed.

### Issue #10 — Route protection middleware
- **Before:** No server-side route protection. Anyone could navigate to `/dashboard/*` without auth.
- **After:** `middleware.ts` redirects unauthenticated users to `/login` and authenticated users away from `/login`/`/register`. Uses a `has_token` cookie synced with localStorage.

### Issue #11 — Home page redirect
- **Before:** `/` showed Next.js boilerplate ("edit page.tsx")
- **After:** `/` redirects to `/login`

### Issue #12 — Dashboard summary API + frontend
- **Before:** Dashboard showed hardcoded zeros for all stats.
- **After:** `GET /api/dashboard` returns real counts (active series, lessons this week, enrollments, trainers, clubs) plus upcoming lessons. Frontend wired with React Query and loading skeletons.

### Issue #13 — Student login redirect
- **Before:** Students redirected to non-existent `/my-lessons` route (404).
- **After:** All roles redirect to `/dashboard` after login.

### Issue #14 — Seed data script
- Added `backend/Scripts/seed-demo-data.ps1` that creates a full demo environment via API calls.
- Creates: 1 organization, 1 admin, 2 invited trainers, 2 tennis clubs, 3 lesson series, 24 lessons, 10 enrollments.

### Additional fixes
- **Topbar user info:** Shows actual user name, translated role label, and initials instead of hardcoded "Coach / Beheerder".
- **Admin as trainer:** Admins can now assign themselves to lesson series (previously only the Trainer role was valid).
- **Docker Compose:** Full-stack setup with backend, frontend, postgres, and smtp4dev.

## What Remains (Nice-to-Have for Polish)

- Success toasts after create/delete operations
- Custom 404/500 error pages
- Pagination on list endpoints
- Student portal (`/my-lessons` page with enrolled series)
- Forgot password flow

## Running the E2E Tests

51 Playwright tests across 8 spec files, all using API mocking (no running backend needed).

```bash
cd frontend

# Headless (CI)
npm run test:e2e

# Watch tests in a browser
npm run test:e2e:headed

# View HTML report from last run
npm run test:e2e:report
```

| Spec | Tests | Covers |
|------|-------|--------|
| auth | 10 | Login/register forms, validation, success/error |
| middleware | 6 | Route guards, redirects, public routes |
| dashboard | 7 | Stats from API, upcoming lessons, quick actions, nav |
| lessons | 8 | List, create, detail, empty states |
| trainers | 5 | List, invite, success state |
| settings | 5 | Club list, add, delete, validation |
| enrollment | 5 | Public enrollment, custom fields |
| navigation | 5 | Sidebar links, logout |

## Seed Data Overview

The seed script (`seed-demo-data.ps1`) creates:

| Entity | Count | Details |
|--------|-------|---------|
| Organization | 1 | TC De Aces |
| Admin user | 1 | Jan Janssen (jan@deaces.be) |
| Invited trainers | 2 | Sophie De Vries, Pieter Mertens |
| Tennis clubs | 2 | TC De Aces (Antwerpen), Padel Center Brussel |
| Lesson series | 3 | Beginners, Gevorderd, Padel Introductie |
| Lessons | 24 | 8 per series, spread over 8 weeks |
| Enrollments | 10 | Spread across all series, Dutch names |

## Architecture Summary

```
Backend (Clean Architecture)
  API → Infrastructure → Application → Domain

  Services: Dashboard, LessonSeries, TennisClub, Enrollment, Auth, Trainer
  Repositories: LessonSeries, Lesson, TennisClub, Enrollment, EnrollmentForm

Frontend (Next.js App Router)
  (auth)/ — login, register, invite
  (dashboard)/ — protected routes with sidebar layout
  (public)/ — enrollment (no auth required)

Docker Compose
  postgres:17 → backend (.NET 10) → frontend (Next.js 16)
  smtp4dev (email testing)
```
