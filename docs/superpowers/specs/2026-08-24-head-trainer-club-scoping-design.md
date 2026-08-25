# Hoofdtrainer per club — scoping design

**Datum:** 2026-08-24
**Status:** Goedgekeurd, klaar voor implementatieplan
**Vervangt:** het org-brede hoofdtrainer-model uit `2026-08-24-head-trainer-role-design.md` (nog niet gemerged; geen data-migratie nodig).

## Probleem

De eerste hoofdtrainer-iteratie gaf een trainer **org-brede** read-only toegang tot inschrijvingen en planning via één bool `OrganizationMembership.IsHeadTrainer`. Een organisatie kan echter lesgeven in **meerdere clubs** (`TennisClub`); `LessonSerie` (en camps, beschikbaarheden) horen elk bij één club. Een hoofdtrainer hoort enkel de zaken van **zijn eigen club(s)** te zien, en indien nodig van meerdere clubs.

Bijkomend blootgelegd gat in de bestaande iteratie: de lessenlijst filtert met
`trainerId = ctx.IsTrainer() ? ctx.GetUserId() : null`. Een hoofdtrainer valt onder `IsTrainer()` en ziet daardoor in de lijst enkel z'n **eigen** reeksen, terwijl hij via de policy wél planning/inschrijvingen van andere reeksen kan openen. Inconsistent.

## Doel

- Admin kan een trainer aanduiden als **hoofdtrainer van één of meerdere specifieke clubs**.
- Een hoofdtrainer krijgt read-only elevatie (inschrijvingen + planning) **enkel voor reeksen van die club(s)**.
- De lessenlijst toont een hoofdtrainer **alle reeksen van zijn club(s)** (∪ z'n eigen reeksen), zodat lijst en toegang consistent zijn.
- Gaat in na opnieuw inloggen (JWT-gebaseerd).

## Niet-scope (bewust)

- Gewone trainers blijven **trainer-scoped** (zien enkel eigen reeksen), niet club-scoped. Enkel de hoofdtrainer-*elevatie* is club-gebonden.
- Geen wijziging aan write-rechten: alle writes (planning genereren/bevestigen, inschrijving aanpassen/annuleren/markeer-betaald, trainerbeheer) blijven admin-only.
- Geen data-migratie van de oude bool (branch niet gemerged).
- De overige admin-controls op de lesreeks-detailpagina (reeks bewerken, lessen, formulier, verwijderen) blijven voorlopig zichtbaar voor hoofdtrainers (403 bij gebruik) — aparte, later te behandelen taak.

## Datamodel

Verwijderen:
- `OrganizationMembership.IsHeadTrainer` (bool).
- Migratie `AddIsHeadTrainerToMembership` (branch-only; wordt gesquasht).

Toevoegen:
- Nieuwe entity `HeadTrainerClub : BaseEntity`
  - `Guid OrganizationMembershipId` — FK → `OrganizationMembership` (`DeleteBehavior.Restrict`).
  - `Guid TennisClubId` — FK → `TennisClub` (`DeleteBehavior.Restrict`).
  - Unieke index op `(OrganizationMembershipId, TennisClubId)`.
- Nav-collectie `OrganizationMembership.HeadTrainerClubs : ICollection<HeadTrainerClub>`.
- `IEntityTypeConfiguration<HeadTrainerClub>` in `Infrastructure/Persistence/Configurations/`.
- `DbSet<HeadTrainerClub>` op `ApplicationDbContext`.
- Eén nieuwe migratie `AddHeadTrainerClubs` (drop kolom `IsHeadTrainer`, create tabel `HeadTrainerClubs`).

Een grant = (membership van een Trainer, club binnen dezelfde org). Lege verzameling grants = geen hoofdtrainer.

## Authorization — twee lagen

**Laag 1 — grove policy** `AuthorizationPolicies.EnrollmentsPlanningRead`:
`Admin || heeft ≥1 headTrainerClub-claim`. Enkel poortwachter "mag deze user überhaupt deze endpoints raken". Vervangt de bestaande `IsHeadTrainer == "true"`-assertion.

**Laag 2 — fijne check in de service** (de kern): elke verhoogde read resolvet de reeks en verifieert `serie.TennisClubId ∈ caller-clubs`, anders `Result.Failure` met een Forbidden-error. Admin bypasst de check.

Endpoints geven de caller-context expliciet mee:
- `ctx.IsAdmin()` (bestaand of toe te voegen helper) en
- `ctx.GetHeadTrainerClubIds()` (nieuwe `HttpContextExtensions`-helper die de `headTrainerClub`-claims leest).

Geraakte verhoogde reads (allemaal per-reeks club-check toevoegen):
- `GET /lessonseries/{id}/planning`
- `GET /lessonseries/{id}/enrollments/planning`
- `GET /lessonseries/{id}/.../non-responders`
- `GET /lessonseries/{id}/.../export`

Servicemethodes krijgen parameters `bool isAdmin, IReadOnlyCollection<Guid> headTrainerClubIds` (of een klein `CallerScope`-record) en enforced de club-check vóór ze data teruggeven.

## JWT / claims

- Weg: bool-claim `isHeadTrainer` (`CoachOsClaims.IsHeadTrainer`).
- Nieuw: **meerdere** claims `headTrainerClub`, één per club-id, in `TokenService.GenerateToken(user, membership)` — geïtereerd over `membership.HeadTrainerClubs`.
- `CoachOsClaims.HeadTrainerClub = "headTrainerClub"`.
- `AuthResponseDto.headTrainerClubIds : string[]` (uit de actieve membership).
- Gaat in na opnieuw inloggen — bewust, afgesproken.

## Lessenlijst — union (dicht het gat)

`ILessonSerieService.GetAllAsync(orgId, trainerId, headTrainerClubIds)`:
- **Admin** → alle reeksen van de org.
- **Hoofdtrainer** → `TrainerId == self` **OF** `TennisClubId ∈ headTrainerClubIds`.
- **Gewone trainer** → `TrainerId == self`.

`GetLessonSerieEndpoint` geeft `trainerId` (zoals nu) + `headTrainerClubIds` (uit claims) mee.

## Admin-beheer

- Vervang `PUT /trainers/{id}/head-trainer { isHeadTrainer: bool }` door
  **`PUT /trainers/{id}/head-trainer-clubs { clubIds: string[] }`** (admin-only).
- Lege `clubIds` = intrekken (alle grants verwijderen).
- `ITrainerService.SetHeadTrainerClubsAsync(trainerId, orgId, clubIds, ct)`:
  - membership moet bestaan met `Role == Trainer` in deze org (anders NotFound),
  - elke club-id moet tot deze org horen (anders Validation/NotFound),
  - vervangt de grant-set atomisch (verwijder bestaande, voeg nieuwe toe).
- `TrainerDto.headTrainerClubIds : string[]` (gevuld in `GetTrainersAsync`).
- Nieuw request-record `SetHeadTrainerClubsRequest { List<Guid> ClubIds }` + FluentValidation.

## Frontend

**`lib/auth.ts`**
- `AuthUser.headTrainerClubIds?: string[]`.
- `isHeadTrainerViewer()` = `role === "Trainer" && (headTrainerClubIds?.length ?? 0) > 0`.
- Read-only gating op planning/inschrijvingen blijft onveranderd (reactief via useState/useEffect).

**`lib/api/auth.ts` / `lib/api/trainers.ts`**
- `AuthResponse.headTrainerClubIds?: string[]`.
- `TrainerDto.headTrainerClubIds: string[]`.
- `setHeadTrainerClubs(id, clubIds)` → `apiClient.put('/trainers/${id}/head-trainer-clubs', { clubIds })`.
- Alle `setAuthUser`-calls (login, invite, org-switcher) zetten `headTrainerClubIds`.

**Trainers-pagina — slimme kroon**
- Haal de clubs van de org op (bestaande clubs-API of via de trainers-response).
- **Org met 1 club** → kroon is een toggle: klik grant/revoke die ene club (1 klik, zoals nu).
- **Org met >1 club** → kroon opent een popover met een checkbox per club; opslaan roept `setHeadTrainerClubs` met de aangevinkte set.
- Badge: kroon "Hoofdtrainer"; bij multi-club toon clubnaam of aantal.

## Seed + reset

- `SetHeadTrainerAsync`-aanroepen in seed vervangen door het nieuwe `head-trainer-clubs`-endpoint (promoot een demo-trainer tot hoofdtrainer van één demo-club).
- `backend/Scripts/seed-demo-data.sh` (+ `.ps1`/`.py` waar van toepassing) bijwerken.
- Migratie toegevoegd → **reset + seed moet end-to-end groen** (definitieve E2E-check):
  `bash Scripts/reset-db.sh --no-frontend` → wachten op `/health` 200 → `bash Scripts/seed-demo-data.sh`.

## Verificatie

- Backend API: hoofdtrainer-van-club-X → reads op reeks van club X = 200; reads op reeks van club Y = 403; writes = 403; gewone trainer = 403 op elevatie; admin = alles.
- Lessenlijst: hoofdtrainer ziet eigen reeksen ∪ alle reeksen van club X; niet die van club Y (tenzij eigen).
- Frontend: kroon-toggle (1 club) en popover (>1 club); read-only planning/inschrijvingen; badge.
- `dotnet build` + `dotnet test` groen; `tsc` groen; reset + seed groen.
