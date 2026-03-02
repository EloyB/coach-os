# CoachOS - Tennis & Padel Lesson Planning

Simpele lessenplanning voor tennis en padel trainers in de Benelux.

## 🎯 Project Overview

**What:** SaaS platform for tennis/padel lesson planning
**Target:** Small-to-medium tennis schools (2-15 trainers) in Belgium & Netherlands
**USP:** Simple, trainer-focused (vs complex club management tools like Elit)

## 🏗️ Tech Stack

**Backend:**

- .NET 10 (LTS)
- Clean Architecture + CQRS (MediatR)
- PostgreSQL + EF Core
- JWT Authentication
- Hangfire (background jobs)

**Frontend:**

- Next.js 15 (App Router)
- TypeScript
- Tailwind CSS + Shadcn UI
- Bun (package manager)
- next-intl (i18n ready: NL → FR)

**Infrastructure:**

- Scaleway (hosting, DB, storage, email)
- Mollie (payments - BE/NL specific)
- Docker (development)

## 📐 Architecture Principles

1. **Clean Architecture** - Domain-first, infrastructure as plugin
2. **CQRS** - Commands (write) / Queries (read) separation via MediatR
3. **Multi-tenancy** - Organization-scoped data isolation
4. **i18n-ready** - Dutch now, French later (resource files setup)

## 🗂️ Project Structure

```
coachos/
├── backend/
│   ├── CoachOS.Domain/          # Entities, interfaces (no dependencies)
│   ├── CoachOS.Application/     # Business logic, CQRS handlers
│   ├── CoachOS.Infrastructure/  # DB, external services
│   ├── CoachOS.API/             # Controllers, startup
│   └── CoachOS.Tests/           # Unit & integration tests
├── frontend/
│   ├── app/                     # Next.js pages (App Router)
│   ├── components/              # React components
│   ├── lib/                     # Utils, API client
│   └── messages/                # i18n translations
└── docs/                        # Project documentation
```

## 🎨 Design Patterns

**Backend:**

- Repository pattern (via EF Core DbContext)
- Command/Query Responsibility Segregation (CQRS)
- Result pattern (no exceptions for business logic)
- Options pattern (configuration)

**Frontend:**

- Server Components (default)
- Client Components (when interactive)
- React Query (API state)
- Form validation (zod + react-hook-form)

## 🗄️ Core Domain Entities

1. **Organization** - Tennis school/club (tenant)
2. **User** - Admin, Trainer, or Student (ASP.NET Identity)
3. **Court** - Tennis/padel court
4. **LessonSeries** - Recurring lesson template (e.g., "Beginners Spring 2026")
5. **Lesson** - Single lesson instance (generated from series)
6. **Enrollment** - Student signup for lesson/series
7. **Payment** - Payment tracking (Mollie integration)
8. **Subscription** - Organization plan (Starter/Pro/Business/Enterprise)

## 💳 Pricing Tiers

- **Starter:** €29/mo - 1 trainer
- **Professional:** €79/mo - 2-5 trainers
- **Business:** €149/mo - 6-15 trainers
- **Enterprise:** €299/mo - 16+ trainers

## 🎯 MVP Features (6 months)

**Month 1-2: Foundation**

- Authentication (JWT)
- Organization setup
- Court management

**Month 3-4: Core Planning**

- Lesson series creation
- Automatic lesson generation
- Enrollment workflow

**Month 5-6: Payments & Polish**

- Mollie integration
- Deferred payments option
- Student portal
- Email notifications (Scaleway TEM)

## 🚫 NOT in MVP

- SMS notifications
- Mobile app (web-first)
- Advanced analytics
- Elit integration (export only)
- Tournament management

## 🌍 i18n Strategy

**Now:** Dutch only (NL)
**Later:** French (FR) for Wallonia
**Implementation:** next-intl (frontend) + IStringLocalizer (backend)

All UI strings in resource files from day 1 (easy to add FR later).

## 🎨 UI/UX Principles

- **Simplicity over features** (vs Elit complexity)
- **Trainer-first workflow** (not admin-heavy)
- **Tennis branding** (green theme, court imagery)
- **Mobile-responsive** (trainers use phones/tablets)

## 📚 Key Documentation

- `/docs/project-analysis.md` - Full technical & business analysis
- `/docs/market-analysis.md` - Market size, target customers
- `/docs/competition-analysis.md` - Competitive landscape Benelux

## 🏃 Development Workflow

**Start development:**

```bash
# Terminal 1: Database
docker-compose up -d

# Terminal 2: Backend
cd backend/CoachOS.API
dotnet run

# Terminal 3: Frontend
cd frontend
bun dev
```

**Create migration:**

```bash
cd backend
dotnet ef migrations add MigrationName --project CoachOS.Infrastructure --startup-project CoachOS.API
dotnet ef database update --project CoachOS.Infrastructure --startup-project CoachOS.API
```

## 🎯 Next Steps

See `/docs/roadmap.md` for week-by-week development plan.

## 📧 Contact

Developer: Eloy (Studio Swyft)
