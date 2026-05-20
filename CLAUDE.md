# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

CoachOS — tennis/padel lesson planning SaaS for Benelux (Dutch MVP, French later). Multi-tenant: every resource is scoped by `OrganizationId`.

## Commands

```bash
# Start all services
docker-compose up -d                          # PostgreSQL + pgAdmin
cd backend/CoachOS.API && dotnet run          # API on http://localhost:5142
cd frontend && bun dev                        # Frontend on http://localhost:5317

# Mollie OAuth lokaal testen (optioneel)
# Vul MOLLIE_CLIENT_ID + MOLLIE_CLIENT_SECRET in .env (zie .env.example);
# rebuild dan de backend container zodat de env vars in worden gepikt:
#   docker-compose up -d --build backend
# Mollie heeft localhost:5142/api/oauth/mollie/callback al als redirect URI.

# Build
cd backend && dotnet build CoachOS.slnx
cd frontend && bun run build

# Tests
cd backend && dotnet test CoachOS.slnx
cd backend && dotnet test --filter "FullyQualifiedName~SomeTest"
cd frontend && bun run test:e2e               # Playwright E2E

# Migrations
cd backend
dotnet ef migrations add <Name> --project CoachOS.Infrastructure --startup-project CoachOS.API
dotnet ef database update --project CoachOS.Infrastructure --startup-project CoachOS.API

# Reset & seed (definitieve E2E-check — destructief, wipet DB volume)
cd backend
bash Scripts/reset-db.sh --no-frontend         # docker compose down -v + up -d (zonder frontend)
# Wacht tot /health 200 terugkeert, dan:
bash Scripts/seed-demo-data.sh                 # registreert admin, creëert clubs/series/enrollments/planning
# PowerShell equivalenten: Scripts/reset-db.ps1 en Scripts/seed-demo-data.ps1

# Add shadcn component
cd frontend && bunx shadcn add <component>
```

## Architecture

### Backend — Clean Architecture + Service Pattern

Dependency direction: `API → Infrastructure → Application → Domain`

- **Domain** — pure entities extending `BaseEntity` (Id, CreatedAt, UpdatedAt), enums, repository/service interfaces, `Result<T>` / `Error` / `ErrorCodes`. Zero external dependencies.
- **Application** — business logic in service classes (`I{Feature}Service` + `{Feature}Service`), DTOs, FluentValidation validators, Mapperly mapper (`ApplicationMapper`). Services return `Result<T>` — never throw for business errors.
- **Infrastructure** — EF Core (`ApplicationDbContext`), repository implementations, ASP.NET Identity (`ApplicationUser`), email, JWT/auth/token services.
- **API** — minimal-API endpoints implementing `IEndpoint` in `Endpoints/{Feature}/`. Each endpoint routes to a service and maps `Result<T>` to HTTP via `ResultExtensions`. Uses `ValidationFilter<T>` for request validation.

**Feature folder structure:** `Application/{Feature}/` contains `I{Feature}Service.cs`, `{Feature}Service.cs`, `DTOs/`, and `Validators/`. Repository interfaces live in `Domain/Interfaces/I{Entity}Repository.cs`; implementations in `Infrastructure/Repositories/`. See [backend/CLAUDE.md](backend/CLAUDE.md) for the full 10-step recipe.

**No MediatR, no CQRS `Commands/Queries/` folders** — the project uses a plain service pattern backed by repositories.

**Key constraint:** `ApplicationUser` lives in Infrastructure (Identity dependency). Domain entities reference users by `Guid` only — no navigation properties to `ApplicationUser`. Use `IUserLookupService` when services need user names, `ITrainerService` when `UserManager` is needed.

**Multi-tenancy:** Every service must filter by `OrganizationId`. Endpoints extract it from the JWT via `ctx.GetOrganizationId()` (see `HttpContextExtensions`) and pass it into the service. `ITenantContext` is available for ambient access where passing it explicitly is awkward.

### Frontend — Next.js App Router

- Server Components by default; `"use client"` only for interactivity/hooks.
- Route groups `(auth)`, `(dashboard)`, `(public)`, `(student)` are invisible in URLs.
- All API calls live in `lib/api/*.ts` using the axios `apiClient` from `lib/api-client.ts`.
- Auth tokens stored via helpers in `lib/auth.ts` (key: `"token"`, user: `"auth_user"`).
- React Query (`["trainers"]`, `["lessonSeries"]`, etc.) for client-side data with `QueryClientProvider` in `components/providers/query-provider.tsx`.
- i18n: `next-intl` with Dutch strings in `messages/nl.json`; components use `useTranslations('namespace')`. Request config in `i18n/request.ts`.
- E2E tests in `frontend/e2e/*.spec.ts` (Playwright); run with `bun run test:e2e`.

**Zod v4 + react-hook-form:** Never use `z.coerce.number()` — use `z.number()` with `valueAsNumber: true` on the `register()` call instead.

### Styling

Tailwind v4 (configured via `app/globals.css`, no `tailwind.config.ts`). Tennis brand tokens: `bg-tennis-green` (#2D5016), `text-tennis-lime` (#D0FF14), `bg-tennis-beige` (#E8DCC4). Shadcn components from `components/ui/`.

**Design language:** Split layout for auth pages (branding panel left, form right). Dashboard: tennis-green sidebar, lime active accents, warm off-white main area (#FAFAF8). Stat cards: white with 4px coloured left border.

### Database

PostgreSQL via EF Core. All entities use `Guid` PKs. `ApplicationUser` configuration is in `Infrastructure/Persistence/Configurations/`. `TrainerId` on `LessonSeries` is a plain `Guid` with no FK constraint (ApplicationUser is in Identity, outside domain scope) — handle orphan prevention in application logic.

## Critical Patterns

**New backend feature checklist** (short version — full walkthrough in [backend/CLAUDE.md](backend/CLAUDE.md)):

1. Domain entity in `Domain/Entities/` → `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations/` → `DbSet<T>` on `ApplicationDbContext` → `dotnet ef migrations add ...`
2. Repository interface in `Domain/Interfaces/I{Entity}Repository.cs` → implementation in `Infrastructure/Repositories/`
3. DTOs in `Application/{Feature}/DTOs/` + FluentValidation validators in `Application/{Feature}/Validators/`
4. Mapperly mapping methods added to `Application/Mappings/ApplicationMapper.cs`
5. `I{Feature}Service` + `{Feature}Service` in `Application/{Feature}/`, returning `Result<T>`, always filtering by `organizationId`
6. `IEndpoint` implementation in `API/Endpoints/{Feature}/` using `.RequireAuthorization()`, `AddEndpointFilter<ValidationFilter<T>>()`, `ctx.GetOrganizationId()`, and `result.ToErrorResult()` for failures
7. Update seed scripts in `backend/Scripts/` if the public contract changed

**Never:**

- Business logic in endpoints (route to services only)
- `any` in TypeScript
- Hardcoded Dutch strings (always `messages/nl.json` on FE, `IStringLocalizer` on BE)
- Cascade deletes (use `DeleteBehavior.Restrict`)
- Throwing exceptions for business failures — return `Result<T>.Failure(...)`
- Accessing `ApplicationDbContext` from endpoints — go through the service → repository
- Fluent configuration directly in `ApplicationDbContext.OnModelCreating` — use an `IEntityTypeConfiguration<T>` class

**Courts:** No dedicated Courts feature in MVP. Court name is a plain text field on `LessonSeries`. The `Court` entity and DB table exist but are not used yet — post-MVP.

**Payments:** Mollie only (not Stripe). Support Bancontact + iDEAL. Dates dd/MM/yyyy, currency EUR, timezone CET/CEST.

## Seed Scripts

When modifying the database schema (entities, migrations), API request/response DTOs, validation rules, or business logic that affects public endpoints — **always check and update the seed scripts** in `backend/Scripts/` (`seed-demo-data.sh`, `setup.sh`) to match. The seed scripts call the API to create demo data, so any contract change will silently break seeding.

## Reset-flow: definitieve E2E-check

Een feature is **pas "done" als een volledige reset + seed end-to-end groen loopt.** Unit tests bewijzen correctheid van losse services — reset bewijst dat migrations, contracts, validators, transactie-volgorde en seed-script allemaal met elkaar kloppen.

**Standaard flow na significant backend werk:**

```bash
cd backend
bash Scripts/reset-db.sh --no-frontend      # 1. Wipet postgres_data volume + rebuild containers
# Wacht tot http://localhost:5142/health → 200 (API draait auto-migrate bij startup)
bash Scripts/seed-demo-data.sh              # 2. API-based seed: registratie, clubs, series, enrollments, planning
```

**Wat de reset test:**

- Migrations passen toe op lege DB (geen drift)
- `RegisterAsync` / admin creatie werkt (1e org)
- `TennisClub` / `LessonSerie` CRUD + validators
- Trainer-uitnodiging + membership flow
- Enrollment submit (solo + group) inclusief capacity/duplicate checks in transactie
- Planning generatie + scheduling algorithm
- `ConfirmScheduleAsync` incl. token creatie + email rendering
- Tweede org + multi-org membership (`org-switcher`)

**Wanneer reset verplicht:**

- EF migratie toegevoegd
- Contract van publieke endpoint (enrollment, magic-link, confirmation) gewijzigd
- Transactieboundary of service-orchestratie aangepast
- Email template tokens / `MjmlTemplateRenderer` gewijzigd
- Before opening PR met backend changes

Als seed faalt: drift in DTO/validator/migratie → eerst `seed-data.json` + `seed-demo-data.py` bijwerken, niet de validators verzwakken.

## Working Style

- Ask clarifying questions one at a time — never list multiple questions at once.
- Never run `git push` or create PRs. The user handles pushing and PR creation.
- `git commit` is allowed — make logical, atomic commits with clear conventional-commit messages.

## graphify

This project has a graphify knowledge graph at `graphify-out/`.

Rules:

- Before answering architecture or codebase questions, read `graphify-out/GRAPH_REPORT.md` for god nodes and community structure
- If `graphify-out/wiki/index.md` exists, navigate it instead of reading raw files
- After modifying code files in this session, run `python3 -c "from graphify.watch import _rebuild_code; from pathlib import Path; _rebuild_code(Path('.'))"` to keep the graph current
