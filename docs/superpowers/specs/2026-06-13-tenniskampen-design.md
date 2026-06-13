# Tenniskampen / stages module — Design

**Status:** Goedgekeurd design, klaar voor implementatieplan (writing-plans).
**Datum:** 2026-06-13
**Aanpak:** A — aparte, zelfstandige Camp-module (reeks-code blijft ongemoeid; alleen de generieke Mollie-betaalrails worden additief uitgebreid).

## Context & doel

CoachOS kent vandaag losse lessen en lesreeksen (terugkerende wekelijkse lessen). Een **kamp/stage** is iets anders: één aaneengesloten periode van X dagen waarvoor je je éénmalig inschrijft, geen terugkerende les. Klanten willen dit als een aparte module kunnen aanmaken en publiek laten inschrijven via een formulier, met **onmiddellijke betaling**: na inschrijven wordt de speler direct naar de betaling gestuurd én krijgt hij een bevestigingsmail met dezelfde betaallink. Er is **geen aparte spelerbevestiging** (zoals de magic-link/planning-flow van reeksen).

Aanleiding: tenniskampen/stages zijn een courante vorm naast reeksen; de huidige reeks-datamodel (wekelijks template + scheduling-algoritme) past er niet op.

## Scope

**In scope (v1):**
- Beheer van kampen (CRUD) met dagen en per-dag-trainers
- Publiek inschrijfformulier (vaste + eigen velden), individueel én als groep
- Onmiddellijke Mollie-betaling na inschrijving + bevestigingsmail met betaallink
- Gratis kampen (prijs 0): betaalstap overslaan, meteen bevestigd
- Eigen "Kampen"-sectie in het dashboard, publieke inschrijf- en bedankpagina

**Bewust buiten scope (zie onderaan):** trainer-self-service, koppeling met trainerbeschikbaarheid-waarschuwingen, wachtlijsten, kortingscodes, gedeeltelijke/aanbetalingen, per-dag inschrijven (je schrijft altijd voor het hele kamp in).

## Architectuuraanpak

Aanpak **A**: het volledige Camp-domein (entiteiten + business-logica + formulierdefinitie) staat los van de reeks-code. De zware, generieke machinerie wordt hergebruikt:
- **Mollie-betaalrails** (payment aanmaken, webhook-sync, thank-you-poll) — hergebruikt via één additieve, gedrag-behoudende wijziging op `Payment`.
- **E-mail-infra** (`IEmailService`, MJML-renderer) — hergebruikt met nieuwe camp-templates.
- **Form-builder UI-component** en de `FormFieldType`-enum — hergebruikt; de form-*data*-entiteiten zijn camp-eigen.

Om duplicatie te beperken zonder de entiteiten te koppelen, wordt de **formuliervalidatie-logica** uit `EnrollmentService` in een gedeelde helper geëxtraheerd die zowel reeks- als camp-inschrijving gebruikt.

## Datamodel

Nieuwe Camp-domein-entiteiten (`Domain/Entities/`), elk met `OrganizationId` en onder de global tenant query-filter:

### `Camp`
- `OrganizationId`, `TennisClubId` (locatie), `Level` (`LessonLevel?`, optioneel niveau/leeftijdsindicatie)
- `Name`, `Description`
- `Price` (decimal, EUR; één prijs voor het hele kamp), `StartDate` (DateOnly), `EndDate` (DateOnly), `RegistrationDeadline` (DateTime)
- `MaxParticipants` (int?, null = onbeperkt), `IsActive` (bool, soft delete)
- Navigatie: `Days`, `Enrollments`, `EnrollmentForm`

### `CampDay` (één rij per dag in het bereik)
- `CampId`, `OrganizationId`, `Date` (DateOnly)
- `StartTime`, `EndTime` (TimeOnly) — de **kampuren** die de deelnemer ziet
- Navigatie: `TrainerAssignments` (`CampDayTrainer[]`)

### `CampDayTrainer` (trainer-aanwezigheid met eigen uren)
- `CampDayId`, `OrganizationId`, `TrainerId` (plain `Guid`, **geen FK** — zelfde patroon als `LessonSlotBase.TrainerId`)
- `StartTime`, `EndTime` (TimeOnly) — het aanwezigheidsvenster van die trainer op die dag
- Regel: het trainer-venster ligt **binnen** de kampuren van die dag (`StartTime >= CampDay.StartTime`, `EndTime <= CampDay.EndTime`, `EndTime > StartTime`)
- "Meerdere trainers per kamp" = de verzameling verschillende `TrainerId`'s over alle dagen; "per trainer welke dagen + uren" = de rijen voor die trainer

### `CampEnrollment` (mirror van `Enrollment`, eigen tabel)
- `OrganizationId`, `CampId`, `ParticipantName`, `ParticipantEmail`, `ParticipantPhone`
- `Status` (hergebruik `EnrollmentStatus`: o.a. `PendingPayment` → `Confirmed`, `Cancelled`), `EnrolledAt`
- `CampEnrollmentGroupId` (Guid?, voor groepsinschrijving)
- Navigatie: `FormResponses` (`CampFormResponse[]`), `Group`

### `CampEnrollmentGroup` (mirror van `EnrollmentGroup`)
- `OrganizationId`, `CampId`; navigatie `Members` (`CampEnrollment[]`)

### Formulier-definitie (camp-eigen)
- `CampEnrollmentForm`: `CampId`, `OrganizationId`; navigatie `Fields`
- `CampFormField`: `CampEnrollmentFormId`, `Label`, `Type` (hergebruik `FormFieldType`), `IsRequired`, `Order`, `Options` (JSON-string voor meerkeuze)
- `CampFormResponse`: `CampEnrollmentId`, `CampFormFieldId`, `Value`

### Gedeelde wijziging — `Payment` (de enige aanraking van bestaande code)
- `EnrollmentId` wordt **nullable**; nieuw veld `CampEnrollmentId` (Guid?, nullable)
- Invariant (in service afgedwongen): **precies één** van `EnrollmentId` / `CampEnrollmentId` is gezet
- Migratie: `EnrollmentId` → nullable + kolom `CampEnrollmentId` toevoegen. Additief; bestaande betalingen behouden hun `EnrollmentId`. Reeks-flow ongewijzigd.

Alle nieuwe entiteiten volgen het CoachOS-recept: `IEntityTypeConfiguration<T>` (handmatig geregistreerd in `OnModelCreating`), `DeleteBehavior.Restrict`, tenant query-filter, EF-migratie.

## Backend — services & endpoints

### Beheer (geauthenticeerd) — `ICampService` / `CampService`
- `GetAllAsync(orgId)` → lijst `CampDto` (incl. dag-aantal, ingeschreven-aantal)
- `GetByIdAsync(id, orgId)` → `CampDetailDto` (dagen, trainer-toewijzingen, formulier)
- `CreateAsync(orgId, CreateCampRequest)` → maakt `Camp` + `CampDay`'s + `CampDayTrainer`'s in één transactie
- `UpdateAsync(id, orgId, UpdateCampRequest)`, `DeleteAsync(id, orgId)` (soft delete)
- `SaveFormAsync(campId, orgId, fields)` — form-builder opslaan
- `GetEnrollmentsAsync(campId, orgId)` → deelnemers + formulier-antwoorden

`CreateCampRequest` bundelt alles: kampvelden + `days: [{ date, startTime, endTime, trainers: [{ trainerId, startTime, endTime }] }]`. Trainers gevalideerd via `IUserLookupService.IsActiveTrainerAsync`. Validatie van trainer-venster binnen kampuren in de validator/service.

Endpoints (`API/Endpoints/Camps/`, `.RequireAuthorization()`):
- `GET/POST /camps`, `GET/PUT/DELETE /camps/{id}`, `PUT /camps/{id}/form`, `GET /camps/{id}/enrollments`

### Publiek (anoniem) — `ICampEnrollmentService`
Endpoints (`API/Endpoints/Public/`, AllowAnonymous, rate-limited zoals reeksen):
- `GET /public/camps/{id}` → publieke kampinfo (dagen + uren, club, prijs, niveau, plekken vrij)
- `GET /public/camps/{id}/form` → formulierdefinitie
- `POST /public/camps/{id}/enroll` → de kern-flow (hieronder)
- `GET /public/camp-enrollments/{id}/payment-status` → voor de thank-you-poll

## Inschrijf + betaal-flow

`SubmitCampEnrollmentAsync(campId, request)`:
1. Laad kamp (actief, bestaat); check `RegistrationDeadline`.
2. Valideer formulier-antwoorden (gedeelde helper geëxtraheerd uit `EnrollmentService`).
3. **SERIALIZABLE transactie**:
   - Capaciteitscheck: tel deelnemers in `Confirmed` + `PendingPayment` inschrijvingen vs `MaxParticipants`, rekening houdend met groepsgrootte.
   - Dubbele-email-check (zoals reeksen).
   - Maak `CampEnrollment` (+ `CampEnrollmentGroup` + leden bij groep) + `CampFormResponse`'s; status `PendingPayment` (prijs > 0) of `Confirmed` (prijs 0).
   - Opslaan, commit.
4. **Na commit**:
   - Prijs > 0 → `PaymentService.CreatePaymentForCampEnrollmentAsync(campEnrollmentId, orgId)` (bedrag = `Price × aantal deelnemers`) → `CheckoutUrl`; verstuur **bevestigingsmail mét betaallink**.
   - Prijs 0 → status al `Confirmed`; verstuur gewone bevestigingsmail.
5. Retour: `{ campEnrollmentId, checkoutUrl? }`.

Frontend: `checkoutUrl` aanwezig → redirect naar Mollie; anders (gratis) → direct naar bedankpagina.

### Betaling (additief op bestaande infra)
- `PaymentService.CreatePaymentForCampEnrollmentAsync`: mirror van de reeks-variant; laadt `CampEnrollment` + `Camp` (prijs), zet `Payment.CampEnrollmentId`, `redirectUrl` → camp thank-you-pagina, `webhookUrl` → bestaande Mollie-webhook.
- `SyncPaymentFromMollieAsync` (webhook) uitgebreid: bij status "betaald" en `CampEnrollmentId` gezet → `CampEnrollment.Status = Confirmed` + "betaling ontvangen"-mail. Idempotent zoals nu.

## E-mail

`IEmailService` uitgebreid met camp-methodes; nieuwe MJML-templates in `Infrastructure/Email/Templates/`:
- `camp-enrollment-payment.mjml` — bevestiging van inschrijving + betaallink (verstuurd bij submit, prijs > 0)
- `camp-enrollment-confirmed.mjml` — "betaling ontvangen / inschrijving definitief" (verstuurd na webhook, of meteen bij een gratis kamp)

Tokens o.a.: deelnemernaam, kampnaam, periode (start–eind), club, betaallink (alleen payment-template).

## Frontend

### Beheer (dashboard) — nieuw nav-item "Kampen" naast "Lessen"
- `/dashboard/camps` — lijst (kaarten: naam, periode, club, ingeschreven/capaciteit)
- `/dashboard/camps/new` en `/dashboard/camps/[id]` — één pagina met secties (geen wizard):
  1. **Basis**: naam, omschrijving, club, niveau, prijs, inschrijfdeadline, max. deelnemers, start/einddatum
  2. **Dagen & trainers** (zie mockup, dag-centrisch): zodra het datumbereik gekozen is genereert de app de dagrijen. Per dagkaart: de **kampuren** (start/eind) bovenaan; daaronder per aanwezige trainer een eigen **start–einduur** (standaard = kampuren, vrij aanpasbaar, geklemd binnen de kampuren). "+ trainer toevoegen" kiest uit de actieve trainers. Geen "halve dag"-badges.
  3. **Inschrijfformulier**: hergebruik van de bestaande form-builder-component
  4. **Inschrijvingen**: uitklapbare lijst met deelnemers + formulier-antwoorden
- API-client: `lib/api/camps.ts` (beheer + publiek)

### Publiek
- `/camp/[campId]` — anoniem inschrijfformulier (mirror van `/enroll/[seriesId]`): kampinfo (periode, daguren per dag, club, prijs, plekken vrij), vaste + eigen velden, solo/groep-toggle met groepsleden. Bij verzenden: `checkoutUrl` → redirect naar Mollie; gratis kamp → bedankpagina.
- `/camp-enrollment/thank-you?campEnrollmentId=…` — betaalstatus-poll (mirror van `/enrollment/thank-you`), gebruikt `GET /public/camp-enrollments/{id}/payment-status`.

## Seed

`backend/Scripts/seed-demo-data.py` + `seed-data.json` uitgebreid:
- Eén betalend demo-kamp (meerdaags, met per-dag-trainers, een form met 1 eigen veld) en één gratis demo-kamp.
- Enkele publieke inschrijvingen (solo + groep) via `POST /public/camps/{id}/enroll` (zonder echte Mollie-betaling in seed; status blijft `PendingPayment` voor de betalende, `Confirmed` voor het gratis kamp).

## Testing / Definition of Done

- Backend unit-tests (NUnit + Moq) voor: `CreateCampRequest`-validator (datums, trainer-venster binnen kampuren, prijs ≥ 0), `CampService` (create met dagen/trainers, capaciteit), `CampEnrollmentService` (capaciteit/dubbel/groep, immediate-payment vs gratis-vertakking), mapper-methodes, `PaymentService` camp-variant.
- Volledige backend-suite groen (`dotnet test CoachOS.slnx`).
- Frontend build groen (`bun run build`).
- **Reset + seed E2E volledig groen** incl. de nieuwe kamp-seed (verplicht vóór "done", zie root `CLAUDE.md`).
- Handmatige flows: kamp aanmaken met per-dag-trainers; publiek inschrijven (solo + groep) betalend → redirect Mollie + mail met link; gratis kamp → meteen bevestigd; webhook → `Confirmed`.
- Geen hardcoded NL-strings buiten toegestane constantes; geen em-dashes in geschreven content.

## Conventie-aandachtspunten (uit eerdere features)

- Tests: **NUnit + Moq**, niet xUnit/NSubstitute.
- EF-config + tenant-filter **handmatig** registreren in `ApplicationDbContext`.
- `getAxiosErrorMessages(error, fallback)` vereist een fallback-message.
- `reset-db.sh` rebuildt de backend-image **niet**; nieuwe endpoints/migraties vereisen `docker compose up -d --build`. Poort 5432 botst met andere lokale postgres-containers.
- Nooit `var`; services geven `Result<T>`; repositories filteren op `OrganizationId` + `.AsNoTracking()` voor reads.

## Bewust buiten scope (niet bouwen in v1)

- Trainer-self-service voor kampen
- Waarschuwing op basis van trainerbeschikbaarheid bij het toewijzen (mogelijke fase 2-koppeling)
- Wachtlijsten, kortingscodes, aanbetalingen/gedeeltelijke betaling
- Per-dag inschrijven (altijd hele kamp)
- Wijzigen van het scheduling-algoritme (kampen gebruiken dat niet)
