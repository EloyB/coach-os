# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

CoachOS — tennis/padel lesson planning SaaS for Benelux (Dutch MVP, French later). Multi-tenant: every resource is scoped by `OrganizationId`.

## Commands

```bash
# Start all services
docker-compose up -d                          # PostgreSQL + pgAdmin
cd backend/CoachOS.API && dotnet run          # API on http://localhost:5142
cd frontend && bun dev                        # Frontend on http://localhost:3000

# Build
cd backend && dotnet build CoachOS.slnx
cd frontend && bun run build

# Tests
cd backend && dotnet test CoachOS.slnx
cd backend && dotnet test --filter "FullyQualifiedName~SomeTest"

# Migrations
cd backend
dotnet ef migrations add <Name> --project CoachOS.Infrastructure --startup-project CoachOS.API
dotnet ef database update --project CoachOS.Infrastructure --startup-project CoachOS.API

# Add shadcn component
cd frontend && bunx shadcn add <component>
```

## Architecture

### Backend — Clean Architecture + CQRS

Dependency direction: `API → Infrastructure → Application → Domain`

- **Domain** — pure entities extending `BaseEntity` (Id, CreatedAt, UpdatedAt), enums. Zero external dependencies.
- **Application** — CQRS handlers via MediatR, DTOs, FluentValidation validators, interfaces (`IApplicationDbContext`, `ITrainerService`, etc.). Returns `Result<T>` — never throws for business logic.
- **Infrastructure** — EF Core (`ApplicationDbContext`), ASP.NET Identity (`ApplicationUser`), email, token/auth services. Implements Application interfaces.
- **API** — thin controllers that only route to `IMediator`. Claims org/user from JWT.

**Feature folder structure:** `Application/{Feature}/Commands/{Verb}{Entity}/` and `Application/{Feature}/Queries/Get{Entity}/`.

**Key constraint:** `ApplicationUser` lives in Infrastructure (Identity dependency). Domain entities reference users by `Guid` only — no navigation properties to `ApplicationUser`. Use `IUserLookupService` from Application when handlers need user names, `ITrainerService` when UserManager is needed.

**Multi-tenancy:** Every handler must filter by `OrganizationId`. The `OrganizationId` claim comes from the JWT and is read in controllers via `User.FindFirst("organizationId")`.

### Frontend — Next.js App Router

- Server Components by default; `"use client"` only for interactivity/hooks.
- Route groups `(auth)` and `(dashboard)` are invisible in URLs.
- All API calls live in `lib/api/*.ts` using the axios `apiClient` from `lib/api-client.ts`.
- Auth tokens stored via helpers in `lib/auth.ts` (key: `"token"`, user: `"auth_user"`).
- React Query (`["trainers"]`, `["lessonSeries"]`, etc.) for client-side data with `QueryClientProvider` in `components/providers/query-provider.tsx`.
- i18n: all Dutch strings go in `messages/nl.json`; components use `useTranslations('namespace')`.

**Zod v4 + react-hook-form:** Never use `z.coerce.number()` — use `z.number()` with `valueAsNumber: true` on the `register()` call instead.

### Styling

Tailwind v4 (configured via `app/globals.css`, no `tailwind.config.ts`). Tennis brand tokens: `bg-tennis-green` (#2D5016), `text-tennis-lime` (#D0FF14), `bg-tennis-beige` (#E8DCC4). Shadcn components from `components/ui/`.

**Design language:** Split layout for auth pages (branding panel left, form right). Dashboard: tennis-green sidebar, lime active accents, warm off-white main area (#FAFAF8). Stat cards: white with 4px coloured left border.

### Database

PostgreSQL via EF Core. All entities use `Guid` PKs. `ApplicationUser` configuration is in `Infrastructure/Persistence/Configurations/`. `TrainerId` on `LessonSeries` is a plain `Guid` with no FK constraint (ApplicationUser is in Identity, outside domain scope) — handle orphan prevention in application logic.

## Critical Patterns

**New backend feature checklist:**
1. Domain entity → EF configuration → `DbSet` in `ApplicationDbContext` + `IApplicationDbContext` → migration
2. DTOs in `Application/{Feature}/`
3. Commands/Queries each get their own folder with Command/Handler/Validator files
4. Thin controller, reads `OrganizationId` from claims

**Never:**
- Business logic in controllers
- `var` in C# (use explicit types)
- `any` in TypeScript
- Hardcoded Dutch strings (always `messages/nl.json`)
- Cascade deletes (use `DeleteBehavior.Restrict`)
- `ApplicationDbContext` directly in Application handlers — use `IApplicationDbContext`

**Courts:** No dedicated Courts feature in MVP. Court name is a plain text field on `LessonSeries`. The `Court` entity and DB table exist but are not used yet — post-MVP.

**Payments:** Mollie only (not Stripe). Support Bancontact + iDEAL. Dates dd/MM/yyyy, currency EUR, timezone CET/CEST.

## Working Style

- Ask clarifying questions one at a time — never list multiple questions at once.
- Never run `git commit`, `git push`, or create PRs. The user handles all version control.
