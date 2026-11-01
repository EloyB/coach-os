# Kampen: dagen en dagdelen kiezen, implementatieplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deelnemers kiezen per kampdag voormiddag, namiddag of hele dag (of "heel kamp"); organisator stelt dagprijs, halve-dagprijs, halve-dagen-toggle en per dag de dagdelen en uren in.

**Architecture:** `Camp.Price` en `CampPrice` verdwijnen. `CampDay` krijgt twee optionele dagdelen. Nieuw `CampEnrollmentDay` legt de keuze per inschrijving vast, `CampEnrollment.TotalAmount` het bedrag. Prijs- en keuzelogica zit in pure statische klassen (`CampPricing`, `CampDaySelectionRules`) die alle services gebruiken; capaciteit wordt per dagdeel binnen de Serializable-transactie gecontroleerd.

**Tech Stack:** .NET 10 (service pattern, EF Core, FluentValidation, xUnit), Next.js 15, React Query, next-intl, Playwright.

**Spec:** `docs/superpowers/specs/2026-11-01-kamp-dagdelen-design.md`

## Global Constraints

- Branch `feat/kamp-dagdelen`, nooit op `main`; atomische conventional commits, eindigend op `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- Elke service filtert op `OrganizationId`; `DeleteBehavior.Restrict`; geen business logic in endpoints; services geven `Result<T>`.
- Geen hardcoded Nederlandse tekst in de frontend (`messages/nl.json`, namespace `camps`); geen `any`; geen em-dashes in website-teksten.
- Zod v4: nooit `z.coerce.number()`, wel `z.number()` + `valueAsNumber: true`.
- Prijzen in EUR, datums dd/MM/yyyy in UI, tijdzone CET/CEST.
- `CampPricing`-regel: beide dagdelen gekozen = `DayPrice`; één dagdeel = `HalfDayPrice` (ook als dat het enige dagdeel van die dag is). `HalfDayPrice <= DayPrice`.
- Seed-scripts (`backend/Scripts/seed-demo-data.py`, `seed-data.json`) en `docs/testing/kampen/TESTGIDS.md` meebewerken bij contractwijzigingen.
- Na backendwerk: `bash Scripts/reset-db.sh --no-frontend` + `bash Scripts/seed-demo-data.sh` moet groen lopen (definitieve check, Task 11).

## Review Focus

1. Hele dag boeken terwijl voormiddag vol zit, maar namiddag niet: hele inschrijving geweigerd, melding noemt dag en dagdeel.
2. Organisator past kamp aan (dag verwijderen, dagdeel weghalen, prijs wijzigen) nadat er inschrijvingen zijn: dagen/dagdelen met inschrijvingen blijven beschermd, `TotalAmount` van bestaande inschrijvingen verandert nooit.
3. Deelnemer kiest een dagdeel dat die dag niet bestaat, een dag van een ander kamp, dezelfde dag twee keer, of niets.
4. Geannuleerde inschrijving geeft plaats in de dagdelen vrij.
5. Gratis kamp (dagprijs 0): meteen bevestigd, geen betaalstap.

---

### Task 1: Datamodel en migratie

**Files:**
- Create: `backend/CoachOS.Domain/Enums/CampDayPart.cs`, `backend/CoachOS.Domain/Entities/CampEnrollmentDay.cs`, `backend/CoachOS.Infrastructure/Persistence/Configurations/CampEnrollmentDayConfiguration.cs`
- Modify: `Domain/Entities/Camp.cs`, `CampDay.cs`, `CampEnrollment.cs`; `Infrastructure/Persistence/ApplicationDbContext.cs` (DbSet, `ApplyConfiguration`, tenant `HasQueryFilter` zoals bij `CampPrice`); `CampConfiguration.cs`, `CampDayConfiguration.cs`, `CampEnrollmentConfiguration.cs`
- Delete: `Domain/Entities/CampPrice.cs`, `CampPriceConfiguration.cs`, en alle `CampPrice`-verwijzingen (DbSet, filter, `ICampRepository.RemovePrices`, `ICampEnrollmentRepository.AnyUsingPricesAsync`, `Include(c => c.Prices)` in `CampRepository`)
- Create: migratie `AddCampDayParts` via `dotnet ef migrations add AddCampDayParts --project CoachOS.Infrastructure --startup-project CoachOS.API`
- Test: migratie wordt handmatig geverifieerd (stap 4)

**Interfaces:**
- Produces: `enum CampDayPart { Morning = 0, Afternoon = 1 }`; `Camp.DayPrice`, `Camp.HalfDayPrice` (`decimal`), `Camp.AllowHalfDays` (`bool`), `Camp.MaxParticipants` (`int?`, nu per dagdeel); `CampDay.MorningStart/MorningEnd/AfternoonStart/AfternoonEnd` (`TimeOnly?`), `CampDay.HasPart(CampDayPart)`, `CampDay.FirstStart`/`LastEnd` (`TimeOnly`, niet gemapt: vroegste start en laatste einde van de bestaande dagdelen); `CampEnrollmentDay { Id, OrganizationId, CampEnrollmentId, CampDayId, Morning, Afternoon }`; `CampEnrollment.TotalAmount` (`decimal`), `CampEnrollment.Days` (`ICollection<CampEnrollmentDay>`).

- [ ] **Step 1: Entiteiten wijzigen** zoals in Interfaces. `CampDay.StartTime`/`EndTime` vervallen; dashboard/export/trainervalidatie gebruiken `FirstStart`/`LastEnd`. `CampEnrollmentDay` configuratie: FK's Restrict, uniek (`CampEnrollmentId`, `CampDayId`), check-constraint `Morning OR Afternoon`.
- [ ] **Step 2: Migratie genereren**, dan de gegenereerde `Up` aanvullen met data-SQL in deze volgorde (voor het droppen van oude kolommen/tabel):
  1. `Camps`: `AllowHalfDays=false`, `DayPrice = round(Price / greatest(aantal dagen,1), 2)`, `HalfDayPrice = DayPrice` (zodat een eendelige dag in een bestaand kamp voor de volle dagprijs verkocht blijft worden).
  2. `CampDays`: `MorningStart = StartTime`, `MorningEnd = EndTime`, namiddag NULL.
  3. `CampEnrollments.TotalAmount` = bedrag van gekozen `CampPrices.Amount`, anders `Camps.Price`.
  4. `CampEnrollmentDays`: per bestaande inschrijving een rij per `CampDay` van het kamp met `Morning=true, Afternoon=false`.
  5. Pas daarna `CampPriceId`, `CampPrices`, `Camps.Price`, `CampDays.StartTime/EndTime` droppen. `Down` mag de oude kolommen herstellen zonder data te herstellen.
- [ ] **Step 3: Compile-fouten oplossen** in andere projecten en de rest van de build mag rood blijven tot Task 3-6 (elke volgende taak herstelt zijn deel). Controleer alleen dat `dotnet build CoachOS.Infrastructure` slaagt.
- [ ] **Step 4: Migratie verifiëren op bestaande data.** `docker-compose up -d`; `dotnet ef database update 20261021044306_AddCampPriceOptions`; voeg met `psql` een kamp (prijs 90, 3 dagen), een tarief "Leden" 70, een inschrijving met dat tarief en een zonder toe; `dotnet ef database update`. Verwacht: `DayPrice=30`, `HalfDayPrice=30`, `TotalAmount` 70 en 90, elk 3 `CampEnrollmentDays` met `Morning=true`, dagen met `MorningStart/End` gevuld.
- [ ] **Step 5: Commit** `feat(kampen): dagdelen en inschrijvingsdagen in het datamodel`.

### Task 2: Prijs- en keuzelogica

**Files:**
- Create: `backend/CoachOS.Application/Camps/DTOs/CampDaySelectionDto.cs`, `backend/CoachOS.Application/Camps/CampDaySelectionRules.cs`
- Modify: `backend/CoachOS.Application/Camps/CampPricing.cs`
- Test: `backend/CoachOS.Tests/Services/CampPricingTests.cs` (herschrijven), `backend/CoachOS.Tests/Services/CampDaySelectionRulesTests.cs`

**Interfaces:**
- Produces: `record CampDaySelectionDto(Guid CampDayId, bool Morning, bool Afternoon)`; `static decimal CampPricing.AmountFor(Camp camp, IEnumerable<CampDaySelectionDto> selection)`; `static decimal CampPricing.Total(IEnumerable<CampEnrollment> participants)` (som `TotalAmount` van niet-geannuleerde); `static Error? CampDaySelectionRules.Validate(Camp camp, IReadOnlyList<CampDaySelectionDto> selection)`.

- [ ] **Step 1: Failing tests** voor `CampPricing`: dagprijs 32.5 / halve 20: één dag beide dagdelen = 32.5; één dag voormiddag = 20; 4 dagen waarvan 3 hele + 1 voormiddag = 117.5; gratis kamp (0/0) = 0; dag met enkel voormiddag volledig gekozen = 20.
- [ ] **Step 2: Failing tests** voor `Validate` (elk een eigen test): lege selectie, dubbele dag, onbekende `CampDayId`, dagdeel bestaat niet op die dag, `Morning=false, Afternoon=false`, `AllowHalfDays=false` met één dagdeel gekozen terwijl de dag er twee heeft (fout) maar dag met één dagdeel en dat dagdeel gekozen (ok), geldige selectie geeft `null`. Foutcode `ErrorCodes.Validation`, berichten Nederlands met datum.
- [ ] **Step 3: Draai** `dotnet test CoachOS.slnx --filter "FullyQualifiedName~CampPricingTests|FullyQualifiedName~CampDaySelectionRulesTests"` (compilatiefout/rood verwacht).
- [ ] **Step 4: Implementeer** signaturen uit Interfaces. `Total` vervangt de oude `Total(camp, ...)`/`UnitPrice`.
- [ ] **Step 5: Tests groen**, commit `feat(kampen): prijs en keuzevalidatie voor dagdelen`.

### Task 3: Kamp aanmaken, bijwerken en lezen

**Files:**
- Modify: `Application/Camps/DTOs/CreateCampRequest.cs`, `UpdateCampRequest.cs`, `CampDayDto.cs`, `CampDetailDto.cs`, `PublicCampDto.cs`, `CampDto.cs` indien nodig; `Application/Camps/CampService.cs`; `Validators/CreateCampRequestValidator.cs`, `UpdateCampRequestValidator.cs`; `Infrastructure/Repositories/CampRepository.cs` / `Domain/Interfaces/ICampRepository.cs`
- Delete: `DTOs/CampPriceDto.cs`, `Validators/SaveCampPriceRequestValidator.cs`, `SaveCampPriceRequestValidatorTests.cs`, `CampPricingLoadingTests.cs`
- Test: `CampServiceTests.cs`, `CreateCampRequestValidatorTests.cs`, `UpdateCampRequestValidatorTests.cs`

**Interfaces:**
- Produces: `CreateCampDayRequest(Guid? Id, string Date, string? MorningStart, string? MorningEnd, string? AfternoonStart, string? AfternoonEnd, List<CreateCampDayTrainerRequest> Trainers)`; `CreateCampRequest(... decimal DayPrice, decimal HalfDayPrice, bool AllowHalfDays, ...)` en `UpdateCampRequest` idem (zonder `Price`/`Prices`); `CampDayDto(Guid Id, string Date, string? MorningStart, string? MorningEnd, string? AfternoonStart, string? AfternoonEnd, List<CampDayTrainerDto> Trainers)`; `CampDetailDto` krijgt `DayPrice`, `HalfDayPrice`, `AllowHalfDays` in plaats van `Price`/`Prices`.

- [ ] **Step 1: Failing validatortests**: dagdelen als `HH:mm`, einde na start, minstens één dagdeel per dag, namiddag begint na einde voormiddag, `DayPrice >= 0`, `HalfDayPrice <= DayPrice`, `HalfDayPrice > 0` verplicht bij `AllowHalfDays` en `DayPrice > 0`, trainer-uren binnen `[vroegste start, laatste einde]` van die dag, trainer-overlap blijft (`CampShiftRules`).
- [ ] **Step 2: Failing servicetests**: aanmaken slaat prijzen, toggle en dagdelen op; detail geeft ze terug; **update met bestaande inschrijvingen** (Review Focus 2): dag met inschrijving verwijderen geeft `Conflict` "Deze dag heeft al inschrijvingen", dagdeel weghalen dat in een `CampEnrollmentDay` gekozen is geeft `Conflict`, dagdelen/uren van dagen zonder inschrijving wijzigen mag, prijswijziging laat `TotalAmount` van bestaande inschrijvingen ongemoeid, `AllowHalfDays` van aan naar uit met bestaande halve-dag-inschrijvingen geeft `Conflict`.
- [ ] **Step 3: Draai** `--filter "FullyQualifiedName~CampServiceTests|FullyQualifiedName~CampRequestValidator"`, rood verwacht.
- [ ] **Step 4: Implementeer.** `UpdateAsync` vervangt de "volledige vervanging van dagen" door diff op `Id` (nieuwe dagen toevoegen, bestaande bijwerken, ontbrekende verwijderen alleen zonder `CampEnrollmentDay`); trainers van een bijgewerkte dag mogen wel vervangen worden. Verwijder `SyncAllPendingCashAmountsAsync`-aanroep voor prijswijzigingen (bedragen zijn vastgelegd); verwijder `SyncPricesAsync`, `MapPrices`, `NewPrice`. `ICampRepository` krijgt `Task<bool> DayHasEnrollmentsAsync(Guid campDayId, CancellationToken ct)` en `Task<bool> AnyEnrollmentUsesPartAsync(Guid campDayId, CampDayPart part, CancellationToken ct)`, plus `Task<bool> AnyHalfDayEnrollmentAsync(Guid campId, CancellationToken ct)`.
- [ ] **Step 5: Tests groen**, commit `feat(kampen): dagdelen en prijzen in aanmaken en bijwerken van kampen`.

### Task 4: Publiek inschrijven

**Files:**
- Modify: `Application/Camps/CampEnrollmentService.cs`, `ICampEnrollmentService.cs`, `DTOs/SubmitCampEnrollmentRequest.cs`, `DTOs/PublicCampDto.cs`, `DTOs/CampPaymentOptionsDto.cs`, `Domain/Interfaces/ICampEnrollmentRepository.cs`, `Infrastructure/Repositories/CampEnrollmentRepository.cs`, `API/Endpoints/Public/GetCampPaymentOptionsEndpoint.cs`, `Validators/SubmitCampEnrollmentRequestValidator.cs`, `Application/Payments/PaymentService.cs` (`CampAmount` -> `CampPricing.Total(CampParticipants(enrollment))`)
- Test: `CampEnrollmentServiceTests.cs`, `PaymentServiceTests.cs`, `CampEnrollmentRepositoryTests` indien repo-tests bestaan in `Tests/Repositories`

**Interfaces:**
- Consumes: Task 2 `CampPricing.AmountFor`, `CampDaySelectionRules.Validate`.
- Produces: `SubmitCampEnrollmentRequest.Days` (`List<CampDaySelectionDto>`); `PublicCampDayDto(Guid Id, string Date, string? MorningStart, string? MorningEnd, string? AfternoonStart, string? AfternoonEnd, bool MorningFull, bool AfternoonFull)` in `PublicCampDto.Days`, met `DayPrice`, `HalfDayPrice`, `AllowHalfDays`; `Task<Dictionary<(Guid CampDayId, CampDayPart Part), int>> ICampEnrollmentRepository.CountActivePerDayPartAsync(Guid campId, CancellationToken ct)` (telt niet-geannuleerde deelnemers, een hele dag telt in beide dagdelen); `GetPaymentOptionsAsync(Guid campId, Guid campEnrollmentId, CancellationToken ct)` met `CampPaymentOptionsDto(decimal Price, bool OnlineAvailable)` waar `Price` het `TotalAmount` van de groep/inschrijving is (route krijgt query `?enrollmentId=`).

- [ ] **Step 1: Failing tests** (`CampEnrollmentServiceTests`): solo met 2 hele dagen + 1 voormiddag krijgt `TotalAmount` volgens de prijsregel en `PendingPayment`; gratis kamp wordt `Confirmed`; Review Focus 1 (hele dag met vol voormiddag geweigerd, `Conflict`, tekst bevat dag en "voormiddag", niets opgeslagen); `MaxParticipants = 1`: tweede inschrijving op hetzelfde dagdeel geweigerd, op ander dagdeel toegestaan; geannuleerde inschrijving geeft plaats vrij (Review Focus 4); groep met 2 leden: iedereen krijgt dezelfde `CampEnrollmentDay`-rijen en eigen `TotalAmount`, capaciteit telt `groepsgrootte`; ongeldige selecties (Review Focus 3) leveren `Validation` en slaan niets op; deadline verstreken blijft geweigerd; `GetPublicCampAsync` zet `MorningFull/AfternoonFull` correct.
- [ ] **Step 2: Failing test** `PaymentServiceTests`: betaalbedrag groep = som `TotalAmount` zonder geannuleerde leden.
- [ ] **Step 3: Draai** `--filter "FullyQualifiedName~CampEnrollmentServiceTests|FullyQualifiedName~PaymentServiceTests"`, rood.
- [ ] **Step 4: Implementeer** in `SubmitAsync`: validatie met `CampDaySelectionRules.Validate` vóór de transactie; binnen de Serializable-transactie `CountActivePerDayPartAsync` ophalen en per gevraagd dagdeel `bestaand + groepsgrootte > MaxParticipants` toetsen; per deelnemer `CampEnrollmentDay`-rijen en `TotalAmount = CampPricing.AmountFor(...)`; `isPaid = TotalAmount > 0`. Verwijder alle `camp.Price`/`CampPriceId`-gebruik. Valideer `Days` ook in `SubmitCampEnrollmentRequestValidator` (niet leeg).
- [ ] **Step 5: Tests groen**, commit `feat(kampen): inschrijven per dagdeel met capaciteit per dagdeel`.

### Task 5: Beheer van inschrijvingen

**Files:**
- Modify: `CampService.cs` (`CreateManualAsync` staat in `CampEnrollmentService`; bewerken en groepslid toevoegen in `CampService`), `DTOs/CreateManualCampEnrollmentRequest.cs`, `UpdateCampEnrollmentRequest.cs`, `AddCampGroupMemberRequest.cs`, `CampEnrollmentDto.cs`, bijbehorende validators
- Test: `CampServiceTests.cs`, `CampEnrollmentServiceTests.cs`

**Interfaces:**
- Consumes: Task 2 en Task 4 (`CountActivePerDayPartAsync`).
- Produces: `CreateManualCampEnrollmentRequest.Days` en `UpdateCampEnrollmentRequest.Days` (`List<CampDaySelectionDto>`); `AddCampGroupMemberRequest` zonder prijsveld (lid neemt de selectie van de groepsleider over, mijn keuze); `CampEnrollmentDto` krijgt `List<CampDaySelectionDto> Days` en `decimal TotalAmount` in plaats van `CampPriceId`/`CampPriceLabel`.

- [ ] **Step 1: Failing tests**: handmatige inschrijving berekent en slaat bedrag + dagen op, `Paid=true` markeert cash betaald voor dat bedrag; bewerken met andere dagkeuze herberekent `TotalAmount` en werkt een openstaande cash-betaling (`payment.Amount`, bestaande logica in `CampService.ApplyPriceChangeAsync`, hernoem naar `ApplyDayChangeAsync`) bij; bewerken is geweigerd als de wijziging een betaalde (Paid) inschrijving van bedrag zou doen veranderen (behoud bestaand gedrag voor paid, lees de oude `ApplyPriceChangeAsync` en houd de regel); capaciteit per dagdeel geldt ook handmatig en bij bewerken (eigen inschrijving telt niet dubbel); groepslid toevoegen kopieert groepsselectie en past groepsbetaling aan.
- [ ] **Step 2: Draai** `--filter "FullyQualifiedName~CampServiceTests|FullyQualifiedName~CampEnrollmentServiceTests"`, rood.
- [ ] **Step 3: Implementeer** en verwijder `CampPriceId`/`UnitPrice`-resten; `GetEnrollmentsAsync` vult `Days` en `TotalAmount`.
- [ ] **Step 4: Tests groen**, commit `feat(kampen): dagkeuze in handmatig inschrijven, bewerken en groepsleden`.

### Task 6: Export en dashboard

**Files:**
- Modify: `Application/Export/CampExportModel.cs`, `CampExportService.cs`, `Infrastructure/Export/ClosedXmlCampWorkbookBuilder.cs`, `Application/Dashboard/DashboardService.cs` (regel ~166 en MapCampDay), DTO's `MyCampDayDto`
- Test: `CampExportServiceTests.cs`, `ClosedXmlCampWorkbookBuilderTests.cs`, `Tests/Dashboard/*`

**Interfaces:**
- Produces: `CampEnrollmentRow` krijgt `string DaysSummary` (bv. "ma voormiddag, di hele dag") en `decimal TotalAmount`; `CampDayRow` krijgt `MorningStart/End`, `AfternoonStart/End` (`TimeOnly?`) plus `int MorningCount`, `int AfternoonCount` (deelnemers, hele dag telt in beide); `MyCampDayDto` blijft `StartTime`/`EndTime` (afgeleid uit `FirstStart`/`LastEnd`).

- [ ] **Step 1: Failing tests**: exportrij toont dagsamenvatting en bedrag uit `TotalAmount`; dagentab toont aantallen per dagdeel; geannuleerden tellen niet mee; dashboard-dag blijft werken met `FirstStart`/`LastEnd`.
- [ ] **Step 2: Rood**, **implementeer**, **groen** (`--filter "FullyQualifiedName~Camp|FullyQualifiedName~Dashboard"`).
- [ ] **Step 3: Commit** `feat(kampen): dagdelen in export en dashboard`.

### Task 7: Seed en testgids

**Files:**
- Modify: `backend/Scripts/seed-demo-data.py` (`create_camps`, regel ~571), `seed-data.json`, `docs/testing/kampen/TESTGIDS.md`, `backend/CLAUDE.md` indien daar kamp-prijzen staan

- [ ] **Step 1:** Seed een betalend kamp met `dayPrice 32.5`, `halfDayPrice 20`, `allowHalfDays true`, 4 dagen waarvan de vierde enkel voormiddag, en een gratis kamp; inschrijvingen met gemengde keuzes (solo, groep, hele kamp). Verwijder tarieven uit de seed.
- [ ] **Step 2:** Vervang in de TESTGIDS het tarievenverhaal door dagdeel-scenario's (alle Review Focus-punten als handmatige testcases).
- [ ] **Step 3: Commit** `chore(kampen): seed en testgids voor dagdelen`. (Uitvoering van de seed gebeurt pas in Task 11.)

### Task 8: Frontend API, types en wizard

**Files:**
- Modify: `frontend/lib/api/camps.ts`, `frontend/lib/api/payments.ts`, `frontend/app/(dashboard)/dashboard/camps/new/_types.ts`, `.../new/_components/camp-basis-fields.tsx`, `step-1-basis.tsx`, `step-2-days.tsx`, `camp-days-editor.tsx`, `camp-day-dialog.tsx`, `step-3-review.tsx`, `.../new/page.tsx`, `frontend/lib/build-days.test.ts` (+ de helper die het test dekt), `frontend/messages/nl.json` (`camps`)
- Delete: `.../new/_components/camp-prices-editor.tsx`

**Interfaces:**
- Produces: TypeScript-types die 1-op-1 de backend-DTO's uit Task 3-5 spiegelen (`CampDaySelection`, `CampDayInput` met vier optionele tijden, `PublicCampDay` met `morningFull/afternoonFull`); `campPartsSummary(day): string` hulpfunctie voor weergave.

- [ ] **Step 1: Types en API** aanpassen; `bunx tsc --noEmit` toont de resterende plekken.
- [ ] **Step 2: Stap 1 (basis)**: dagprijs, toggle "Halve dagen toegestaan", halve-dagprijs (alleen bij toggle aan), maximum per dagdeel, standaarduren voor voor- en namiddag (voorinvullen 09:15-12:00 en 13:15-16:00, aanpasbaar); tarieven-editor weg. Zod-schema met `valueAsNumber`.
- [ ] **Step 3: Stap 2 (dagen)**: bestaande dagen-editor behouden; nieuwe dagen krijgen de standaarduren en de toggle bepaalt of ze één of twee dagdelen krijgen; `camp-day-dialog` laat per dag voormiddag/namiddag aan of uit zetten met eigen uren (minstens één aan), trainers per dag zoals nu met uren binnen het bereik van de dag. Pas de bestaande helper en `build-days.test.ts` aan zodat dagdelen worden meegegeven.
- [ ] **Step 4: Stap 3 (controle)** toont per dag de dagdelen en een prijsindicatie (hele dag en halve dag).
- [ ] **Step 5: Verifiëren**: `cd frontend && bun test lib/build-days.test.ts && bun run build`, daarna `bun dev` en in de browser (Playwright MCP) een kamp aanmaken in de wizard en in de lijst/detail terugzien.
- [ ] **Step 6: Commit** `feat(kampen): wizard met dagprijzen en dagdelen`.

### Task 9: Publiek inschrijfformulier

**Files:**
- Modify: `frontend/app/(public)/camp/[campId]/page.tsx` (splits dagkeuze uit in `frontend/app/(public)/camp/[campId]/_components/camp-day-picker.tsx` zodat Task 10 het kan hergebruiken), `frontend/app/(public)/camp-enrollment/payment/page.tsx`, `frontend/messages/nl.json`
- Test: `frontend/e2e/camp-dagdelen.spec.ts` (Playwright)

**Interfaces:**
- Consumes: `PublicCampDay`/`CampDaySelection` uit Task 8.
- Produces: component `CampDayPicker({ days, allowHalfDays, dayPrice, halfDayPrice, value, onChange })` met radiogroep per dag (alleen bestaande opties, "vol" disabled, zonder halve dagen enkel "Hele dag"), snelkeuze "Ik kom heel het kamp" en `calcTotal(value, dayPrice, halfDayPrice): number` die dezelfde regel als `CampPricing.AmountFor` volgt.

- [ ] **Step 1: Playwright-test** (via de enige toegestane Agent-route of zelf): kamp uit de seed openen, "Ik kom heel het kamp" kiezen toont totaal 4 dagen volgens seed, één dag op voormiddag zetten verlaagt het totaal, volle dagdelen zijn uitgeschakeld, inschrijven leidt naar de betaalpagina met hetzelfde bedrag.
- [ ] **Step 2: Implementeer** de picker, koppel aan het formulier en vervang `price`-weergave in publieke pagina en betaalpagina (bedrag via nieuw payment-options endpoint met `enrollmentId`).
- [ ] **Step 3: Verifiëren**: `bun run build` en `bun run test:e2e -- camp-dagdelen`.
- [ ] **Step 4: Commit** `feat(kampen): dagen en dagdelen kiezen op het publieke formulier`.

### Task 10: Beheerschermen

**Files:**
- Modify: `.../camps/_components/camp-manual-enrollment-dialog.tsx`, `camp-edit-enrollment-dialog.tsx`, `camp-add-member-dialog.tsx`, `camp-enrollment-detail-dialog.tsx`, `camp-enrollments-section.tsx`, `camp-edit-form.tsx`, `camp-days-card.tsx`, `camp-days-readonly-card.tsx`, `camps/page.tsx`, `camps/[id]/page.tsx`, `frontend/components/dashboard/trainer-dashboard.tsx` indien het dagtijden toont
- Delete: `camp-prices-card.tsx`, `camp-price-dialog.tsx`, `camp-price-select.tsx`

**Interfaces:**
- Consumes: `CampDayPicker` uit Task 9 (verplaats naar `frontend/components/camps/camp-day-picker.tsx` zodat publiek en beheer het delen).

- [ ] **Step 1:** Handmatige inschrijving en bewerken gebruiken `CampDayPicker` met live totaal; groepslid toevoegen toont de overgenomen groepskeuze alleen-lezen.
- [ ] **Step 2:** Inschrijvingsdetail en -tabel tonen dagsamenvatting en `totalAmount`; kampdetail toont dagprijs/halve-dagprijs en per dag dagdelen met aantallen; foutmelding "dag heeft inschrijvingen" uit Task 3 wordt netjes getoond in `camp-edit-form`.
- [ ] **Step 3: Verifiëren**: `bun run build`, handmatig in de browser of via Playwright MCP: handmatig inschrijven, bewerken, groepslid toevoegen.
- [ ] **Step 4: Commit** `feat(kampen): dagkeuze in de beheerschermen`.

### Task 11: Eindverificatie

- [ ] **Step 1:** `cd backend && dotnet build CoachOS.slnx && dotnet test CoachOS.slnx`, alles groen.
- [ ] **Step 2:** Reset-flow: `bash Scripts/reset-db.sh --no-frontend`, wacht op `/health` 200, `bash Scripts/seed-demo-data.sh`, beide groen.
- [ ] **Step 3:** `cd frontend && bun run build && bun run test:e2e`, groen.
- [ ] **Step 4:** Herhaal de migratieverificatie van Task 1 stap 4 op de definitieve migratie.
- [ ] **Step 5:** Graphify bijwerken (`python3 -c "from graphify.watch import _rebuild_code; from pathlib import Path; _rebuild_code(Path('.'))"`), docs-maintainer-agent mag docs nalopen; daarna `code-reviewer`, `security-auditor` (tenant-isolatie op `CampEnrollmentDay`) en `db-migration-reviewer` over de branch.
- [ ] **Step 6:** Open PR via `gh pr create` (geen merge, geen push naar `main`).
