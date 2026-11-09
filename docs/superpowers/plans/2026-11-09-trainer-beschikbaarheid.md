# Trainer-beschikbaarheid Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Een trainer beheert zelf zijn beschikbaarheid per weekdag op een eigen instellingenpagina; trainer en admin delen één lijst per trainer.

**Architecture:** Bestaande `TrainerAvailability`-tabel en service blijven. Nieuwe `/me/trainer-availabilities`-endpoints (rol Trainer) halen de trainer uit het token en delegeren naar nieuwe service-methoden met een eigenaarcheck. Frontend: nieuwe route `/dashboard/trainer-settings` met een weekoverzicht (desktop) en dagenlijst (mobiel), gedeelde layoutlogica in een geteste lib.

**Tech Stack:** .NET 10 (service pattern, `Result<T>`, FluentValidation, NUnit + Moq + FluentAssertions), Next.js 15, React Query, next-intl, Playwright, vitest (`bun run test:unit`).

**Spec:** `docs/superpowers/specs/2026-11-09-trainer-beschikbaarheid-design.md`

## Global Constraints

- Backend `CLAUDE.md`: geen `var`, services geven `Result<T>` terug, filter altijd op `organizationId`, geen business logic in endpoints, Dutch foutteksten, `DeleteBehavior.Restrict`.
- Frontend: geen hardcoded Nederlands (alles in `messages/nl.json`), geen `any`, API-calls in `lib/api/*.ts`, Tailwind, Zod v4 gebruikt `valueAsNumber` (geen `z.coerce`).
- Weekdag: 0 = maandag ... 6 = zondag. Tijden `HH:mm`. Overlap per trainer per dag niet toegestaan, over alle clubs heen.
- De bestaande admin-endpoints `/trainer-availabilities` en de admin-dialoog blijven ongewijzigd.
- Een trainer mag nooit tijdvakken van een andere trainer zien of wijzigen (anti-doel); een vreemd id geeft `NotFound`.
- Branch `feat/trainer-beschikbaarheid`. Niet pushen en geen PR zonder opdracht.

## Review Focus

- Vreemd tijdvak-id (van een andere trainer of andere organisatie) bij PUT en DELETE moet `NotFound` geven en niets wijzigen.
- Een tijdvak wijzigen naar een bereik dat enkel met zichzelf overlapt moet slagen.
- Bestaande tijdvakken buiten 07:00-23:00 (door de admin ingevuld) moeten zichtbaar blijven in het weekoverzicht.
- Een admin die `/dashboard/trainer-settings` opent wordt teruggestuurd; een trainer ziet "Instellingen" naar de trainerpagina en niet naar de admin-instellingen.
- Seed: na reset + seed kan een gewone trainer inloggen en ziet de pagina zijn eigen tijdvakken.

---

### Task 1: Service, repository en validatie voor eigen tijdvakken

**Files:**
- Modify: `backend/CoachOS.Domain/Interfaces/ITrainerAvailabilityRepository.cs`, `backend/CoachOS.Infrastructure/Repositories/TrainerAvailabilityRepository.cs`
- Modify: `backend/CoachOS.Application/TrainerAvailabilities/ITrainerAvailabilityService.cs`, `TrainerAvailabilityService.cs`
- Create: `backend/CoachOS.Application/TrainerAvailabilities/DTOs/SaveOwnTrainerAvailabilityRequest.cs`
- Create: `backend/CoachOS.Application/TrainerAvailabilities/Validators/SaveOwnTrainerAvailabilityRequestValidator.cs`
- Modify: `backend/CoachOS.Application/Mappings/ApplicationMapper.cs` (geen nieuwe mapping nodig als de service een `CreateTrainerAvailabilityRequest` bouwt)
- Test: `backend/CoachOS.Tests/Services/TrainerAvailabilityServiceTests.cs` (uitbreiden), `backend/CoachOS.Tests/Validators/SaveOwnTrainerAvailabilityRequestValidatorTests.cs` (nieuw)

**Interfaces:**
- Consumes: bestaande `ITrainerAvailabilityRepository.GetByIdAsync(id, organizationId, ct)`, `ITennisClubRepository.ExistsAsync`, `IUserLookupService.IsActiveTrainerAsync`, `ApplicationMapper.ToTrainerAvailability/ToTrainerAvailabilityDto`.
- Produces:
  - `record SaveOwnTrainerAvailabilityRequest(Guid? TennisClubId, int DayOfWeek, string StartTime, string EndTime)`
  - `ITrainerAvailabilityRepository.HasOverlapAsync(Guid trainerId, Guid organizationId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, Guid? excludeId = null, CancellationToken ct = default)` (excludeId vóór ct; update alle bestaande aanroepers en mocks)
  - `ITrainerAvailabilityRepository.GetByTrainerAsync(Guid organizationId, Guid trainerId, CancellationToken ct = default)` -> `Task<IReadOnlyList<TrainerAvailability>>` (actief, met `TennisClub`, gesorteerd op weekdag en starttijd)
  - Service: `GetForTrainerAsync(Guid organizationId, Guid trainerId, ct)` -> `Result<List<TrainerAvailabilityDto>>`; `CreateForTrainerAsync(Guid organizationId, Guid trainerId, SaveOwnTrainerAvailabilityRequest request, ct)` -> `Result<Guid>`; `UpdateForTrainerAsync(Guid id, Guid organizationId, Guid trainerId, SaveOwnTrainerAvailabilityRequest request, ct)` -> `Result`; `DeleteForTrainerAsync(Guid id, Guid organizationId, Guid trainerId, ct)` -> `Result`.

- [ ] **Step 1: Schrijf falende servicetests** in `TrainerAvailabilityServiceTests` (zelfde fixture en `SetUp`): `GetForTrainerAsync_ReturnsOnlyOwnAvailabilities` (repo `GetByTrainerAsync` aangeroepen met de trainer-id, DTO's teruggegeven); `CreateForTrainerAsync_Valid_AddsWithTrainerIdFromArgument` (ontvangen entity heeft `TrainerId == _trainerId`); `CreateForTrainerAsync_UnknownClub_ReturnsNotFound`; `CreateForTrainerAsync_Overlap_ReturnsConflict`; `UpdateForTrainerAsync_Valid_UpdatesFields` (entity start/eind/club/dag gewijzigd, `SaveChangesAsync` aangeroepen, overlapcheck gebeurt met `excludeId == id`); `UpdateForTrainerAsync_OtherTrainersAvailability_ReturnsNotFoundAndChangesNothing` (repo geeft entity met andere `TrainerId`; `SaveChangesAsync` nooit aangeroepen); `UpdateForTrainerAsync_UnknownId_ReturnsNotFound`; `UpdateForTrainerAsync_Overlap_ReturnsConflict`; `DeleteForTrainerAsync_Own_SetsIsActiveFalse`; `DeleteForTrainerAsync_OtherTrainersAvailability_ReturnsNotFoundAndKeepsActive`.
- [ ] **Step 2: Schrijf falende validatortests** `SaveOwnTrainerAvailabilityRequestValidatorTests`: geldig slaagt; weekdag 7 faalt "Ongeldige weekdag"; `"25:00"` faalt "Ongeldige starttijd (HH:mm)"; einde gelijk aan of voor begin faalt "Eindtijd moet na starttijd zijn"; `TennisClubId = Guid.Empty` faalt "Ongeldige club"; `null` club slaagt.
- [ ] **Step 3: Run** `cd backend && dotnet test CoachOS.Tests --filter "FullyQualifiedName~TrainerAvailability"`. Expected: compile-fout of FAIL (methoden bestaan niet).
- [ ] **Step 4: Implementeer** de DTO, de validator (zelfde regels als `CreateTrainerAvailabilityRequestValidator`, zonder `TrainerId`), de repository-wijzigingen (`excludeId` in de overlap-query: `&& (excludeId == null || a.Id != excludeId)`; `GetByTrainerAsync`) en de vier servicemethoden. `Create`: gedeelde privé-methode met de bestaande `CreateAsync` zodat clubcheck, trainercheck en overlap niet gedupliceerd worden. `Update`/`Delete`: haal via `repo.GetByIdAsync(id, organizationId, ct)`, `NotFound` ("Beschikbaarheid niet gevonden") als null of `TrainerId != trainerId`. Update de bestaande `CreateAsync` zodat hij `excludeId` niet meegeeft en pas bestaande testmocks aan voor de nieuwe parameter. Registreer de validator als de DI de validators niet automatisch scant (controleer `DependencyInjection.cs`).
- [ ] **Step 5: Run** `cd backend && dotnet test CoachOS.slnx`. Expected: alles groen, inclusief de nieuwe tests.
- [ ] **Step 6: Commit** `feat(trainer): eigen beschikbaarheid beheren in service en repository`.

### Task 2: Endpoints voor de ingelogde trainer

**Files:**
- Create: `backend/CoachOS.API/Endpoints/TrainerAvailabilities/GetOwnTrainerAvailabilitiesEndpoint.cs`, `CreateOwnTrainerAvailabilityEndpoint.cs`, `UpdateOwnTrainerAvailabilityEndpoint.cs`, `DeleteOwnTrainerAvailabilityEndpoint.cs`
- Check: `backend/CoachOS.API/Endpoints/EndpointMappingExtensions.cs` (endpoints worden gescand via `IEndpoint`; controleer of een expliciete registratie nodig is)
- Test: `backend/CoachOS.Tests/` (als er een endpoint-/integratietestpatroon bestaat, volg dat; anders valt de dekking onder de servicetests en de handmatige check in Task 6)

**Interfaces:**
- Consumes: de service-methoden uit Task 1; `ctx.GetOrganizationId()`, `ctx.GetUserId()`, `result.ToErrorResult()`, `ValidationFilter<SaveOwnTrainerAvailabilityRequest>`.
- Produces: `GET /api/me/trainer-availabilities` (200 `TrainerAvailabilityDto[]`), `POST` (201 met id), `PUT /{id:guid}` (204), `DELETE /{id:guid}` (204); allemaal `.RequireAuthorization(policy => policy.RequireRole("Trainer"))`, `.WithTags("TrainerAvailabilities")`.

- [ ] **Step 1: Implementeer de vier endpoints** volgens het patroon van `CreateTrainerAvailabilityEndpoint` (geen business logic; `AddEndpointFilter<ValidationFilter<SaveOwnTrainerAvailabilityRequest>>()` op POST en PUT).
- [ ] **Step 2: Build en run** `cd backend && dotnet build CoachOS.slnx && dotnet test CoachOS.slnx`. Expected: 0 fouten, alle tests groen.
- [ ] **Step 3: Commit** `feat(trainer): /me/trainer-availabilities endpoints voor trainers`.

### Task 3: API-client en layoutlogica (frontend, geteste lib)

**Files:**
- Modify: `frontend/lib/api/trainerAvailabilities.ts`
- Create: `frontend/lib/trainer-availability-layout.ts`, `frontend/lib/trainer-availability-layout.test.ts`

**Interfaces:**
- Produces in de API-client: `SaveOwnTrainerAvailabilityRequest { tennisClubId: string | null; dayOfWeek: number; startTime: string; endTime: string }`; `getOwnTrainerAvailabilities(): Promise<TrainerAvailabilityDto[]>`; `createOwnTrainerAvailability(request): Promise<string>`; `updateOwnTrainerAvailability(id: string, request): Promise<void>`; `deleteOwnTrainerAvailability(id: string): Promise<void>`.
- Produces in de lib (pure functies): `toMinutes(time: string): number`; `toTime(minutes: number): string`; `snapToQuarter(minutes: number): number`; `computeAxisRange(availabilities: { startTime: string; endTime: string }[]): { startHour: number; endHour: number }` (standaard 7-23, breder naar de dichtstbijzijnde hele uur bij tijdvakken buiten het bereik); `blockPosition(startTime, endTime, range): { topPercent: number; heightPercent: number }`; `minutesFromOffset(offsetY: number, heightPx: number, range): number` (gesnapt, begrensd binnen het bereik); `overlaps(a, b): boolean` (halfopen, raken is geen overlap); `groupByDay(availabilities): Record<number, TrainerAvailabilityDto[]>` (0-6 altijd aanwezig, gesorteerd op start); `clubColorIndex(clubId: string | null, clubIds: string[]): number` (null = "elke club" = index 0 voor groen, clubs volgen).

- [ ] **Step 1: Schrijf falende vitest-tests** `trainer-availability-layout.test.ts`: `snapToQuarter(7)` -> 0, `(8)` -> 15, `(52)` -> 45; `toMinutes("17:30")` -> 1050; `toTime(1050)` -> "17:30"; `computeAxisRange([])` -> `{7, 23}`; `computeAxisRange([{ startTime: "06:30", endTime: "08:00" }])` -> startHour 6; `computeAxisRange([{ startTime: "20:00", endTime: "23:45" }])` -> endHour 24; `blockPosition("09:00", "12:00", {7, 23})` -> top 12.5, hoogte 18.75; `minutesFromOffset` snapt en begrenst; `overlaps` raken ("09:00-10:00" en "10:00-11:00") is false, `09:30-10:30` is true; `groupByDay` bevat alle 7 sleutels en sorteert; `clubColorIndex(null, [...])` -> 0, eerste club -> 1.
- [ ] **Step 2: Run** `cd frontend && bun run test:unit`. Expected: FAIL (module bestaat niet).
- [ ] **Step 3: Implementeer** de lib en de vier API-functies (naar `/me/trainer-availabilities`).
- [ ] **Step 4: Run** `bun run test:unit`. Expected: alles groen.
- [ ] **Step 5: Commit** `feat(trainer): api-client en layoutlogica voor beschikbaarheid`.

### Task 4: Route, navigatie, middleware en UI

**Files:**
- Modify: `frontend/lib/nav-items.ts` (veld `trainerOnly?: boolean` en een tweede item "Instellingen" met `href: "/dashboard/trainer-settings"`, `trainerOnly: true`), `frontend/components/layouts/dashboard-sidebar.tsx` en `dashboard-bottom-nav.tsx` (filter: adminOnly alleen voor Admin, trainerOnly alleen voor Trainer), `frontend/middleware.ts` (trainer-only pad: niet-trainers naar `/dashboard`), `frontend/messages/nl.json` (namespace `trainerSettings`)
- Create: `frontend/app/(dashboard)/dashboard/trainer-settings/page.tsx`, `_components/availability-section.tsx`, `availability-week-grid.tsx`, `availability-day-list.tsx`, `availability-form-dialog.tsx`

**Interfaces:**
- Consumes: de API-client en lib uit Task 3, `getTennisClubs` uit `@/lib/api/tennisClubs`, `getAxiosErrorMessages`, bestaande `Dialog`/`NativeSelect`/`inputClass`.
- Produces: pagina `/dashboard/trainer-settings`. `AvailabilitySection` haalt eigen tijdvakken en clubs op (React Query keys `["ownTrainerAvailabilities"]` en `["tennisClubs"]`), toont `AvailabilityWeekGrid` vanaf `md` en `AvailabilityDayList` eronder, beheert de dialoogstatus en roept create/update/delete aan. `AvailabilityFormDialog` props: `{ mode: "create" | "edit"; initial: { id?: string; dayOfWeek: number; startTime: string; endTime: string; tennisClubId: string | null }; clubs; onSave(values); onDelete?(); onClose(); error?: string[]; isSaving: boolean }`.

- [ ] **Step 1: Nav en middleware.** Voeg het trainer-item en de filters toe. Middleware: `TRAINER_ONLY_DASHBOARD_PATHS = ["/dashboard/trainer-settings"]`; als de `user_role`-cookie bestaat en niet `Trainer` is, redirect naar `/dashboard`.
- [ ] **Step 2: Teksten.** Voeg de namespace `trainerSettings` toe (paginatitel, sectie "Beschikbaarheid", uitleg, dagnamen, "Elke club", van/tot/club/dag, "Tijdvak toevoegen", opslaan/verwijderen/annuleren, lege dag "Niet beschikbaar", legenda, algemene foutmelding).
- [ ] **Step 3: Componenten.** Weekoverzicht: 7 kolommen met tijdas (uurlijnen), blokken op basis van `blockPosition`, klik op leeg stuk opent de dialoog (start = gesnapt klikpunt, einde = start + 60 min, begrensd), slepen over een kolom (pointer events, drempel 6 px) vult start en einde, klik op een blok opent bewerken, kleur per club via `clubColorIndex`, legenda. Dagenlijst (zichtbaar onder `md`): per dag de tijdvakken als knoppen met club, "Niet beschikbaar" bij leeg, knop "Tijdvak toevoegen" per dag. Dialoog: velden club (`NativeSelect`, standaard "Elke club"), dag (alleen bij toevoegen), van, tot (`input type="time"`, `step=900`), opslaan, verwijderen bij bewerken, API-fouten rood in de dialoog. Foutberichten en succes via React Query-invalidatie van `["ownTrainerAvailabilities"]`.
- [ ] **Step 4: Run** `cd frontend && bunx tsc --noEmit && bun run test:unit && bun run build`. Expected: geen type- of buildfouten, tests groen. Los lint-fouten in de nieuwe bestanden op.
- [ ] **Step 5: Commit** `feat(trainer): instellingenpagina met beschikbaarheid voor trainers`.

### Task 5: Playwright-tests (gemockte API)

**Files:**
- Create: `frontend/e2e/trainer-beschikbaarheid.spec.ts`
- Modify: `frontend/e2e/middleware.spec.ts` (een test voor de trainer-only route)

**Interfaces:**
- Consumes: de pagina uit Task 4; mock-aanpak zoals in `camp-dagdelen.spec.ts` (`**/api`-routes met CORS-headers, host-agnostisch); sessie via `localStorage` (`token`, `auth_user`) en cookies `has_token=1` en `user_role=Trainer`.

- [ ] **Step 1: Schrijf de tests.** (a) Desktop 1280 px: pagina toont het weekoverzicht en een bestaand blok van de trainer; klik op bewerken, wijzig de eindtijd, verwacht een `PUT` met de nieuwe `endTime`. (b) Toevoegen via de dialoog stuurt een `POST` met `tennisClubId: null` als "Elke club" gekozen is en met de club-id bij een gekozen club. (c) Overlapfout (409 met melding) wordt in de dialoog getoond. (d) Verwijderen stuurt `DELETE`. (e) Mobiel 390 px: de lijst per weekdag is zichtbaar en het weekoverzicht niet; "Tijdvak toevoegen" werkt. (f) Een tijdvak buiten 07:00-23:00 (06:00-08:00) blijft zichtbaar. (g) Middleware: een Admin (`user_role=Admin`) die `/dashboard/trainer-settings` opent, belandt op `/dashboard`.
- [ ] **Step 2: Run** `cd frontend && bunx playwright test e2e/trainer-beschikbaarheid.spec.ts e2e/middleware.spec.ts -g "trainer"`. Expected: alle nieuwe tests groen. Bestaande, niet-gerelateerde mislukkingen in andere specs staan niet in dit scope (benoemen als ze optreden).
- [ ] **Step 3: Commit** `test(trainer): e2e voor de beschikbaarheidspagina`.

### Task 6: Seed, testgids, reset en end-to-end controle

**Files:**
- Modify: `backend/Scripts/seed-data.json` (veld `plainTrainer` met `trainerIndex: 1`, `password`), `backend/Scripts/seed-demo-data.py` (activeer een gewone trainer en zet eigen tijdvakken via `/me/trainer-availabilities` met zijn login; toon de credentials in "Login credentials")
- Create: `docs/testing/trainer-beschikbaarheid/TESTGIDS.md`

**Interfaces:**
- Consumes: bestaande `fetch_invite_token_from_mailpit`, `/trainers/accept-invite`, login-flow van `ApiClient`.
- Produces: na de seed een admin `jan@deaces.be` / `Demo1234!`, een gewone trainer `pieter@deaces.be` / `Demo1234!` (en de hoofdtrainer Sophie), met een paar tijdvakken voor Pieter.

- [ ] **Step 1: Seed uitbreiden.** Hergebruik de activatiecode van de hoofdtrainer in een helper `activate_trainer(api, trainer_spec, password)`. Faalt zacht (zoals de bestaande functie). Maak voor de gewone trainer tijdvakken aan via zijn token op `/me/trainer-availabilities` (verschillende dagen, één met een club en één zonder).
- [ ] **Step 2: Testgids schrijven** (handmatige scenario's: trainer vult in, admin ziet het op de Trainers-pagina, trainer ziet wijziging van admin, overlap, mobiel, een vreemd id via curl geeft 404, admin op de trainerpagina).
- [ ] **Step 3: Backend opnieuw bouwen en seed draaien.** `cd /Users/eloyboone/Documents/Studio-Swyft/Projecten/coach-os/backend && bash Scripts/reset-db.sh --no-frontend`, wachten tot `http://localhost:5142/health` 200 geeft, daarna `bash Scripts/seed-demo-data.sh`. Expected: seed zonder fouten, de login-credentials voor admin en gewone trainer staan in de uitvoer. (De lokale database bevat nog de migratie van `feat/kamp-dagdelen`; de reset wist die, wat de bedoeling is op `main`-gebaseerde code.)
- [ ] **Step 4: Controleren.** Log in als de gewone trainer via de API (`/auth/login`), `GET /api/me/trainer-availabilities` geeft de eigen tijdvakken, een tijdvak-id van een andere trainer geeft 404 bij PUT en DELETE, een admin-token op `/api/me/trainer-availabilities` geeft 403. Controleer in de browser (Playwright) dat de trainerpagina laadt en de tijdvakken toont.
- [ ] **Step 5: Frontend draaiend houden** (`bun dev` op 5317) en volledige checks: `cd backend && dotnet test CoachOS.slnx`, `cd frontend && bun run test:unit && bun run build`.
- [ ] **Step 6: Commit** `chore(trainer): seed met gewone trainer en testgids`.
