# Kamp-pakketten Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (native, in this session; the Agent tool is reserved for Playwright tests per project memory). Steps use checkbox (`- [ ]`) syntax.

**Goal:** Clubs maken per kamp vaste pakketten (dagdelen + prijs); ouders kiezen per kind een pakket; limiet geldt per dagdeel; bestaande tarieven worden een vast kortingsbedrag.

**Architecture:** `CampDay` krijgt `CampDayPart`s (dagdelen). `CampPackage` + `CampPackageSlot(Date, PartOrder)` leggen vast welke dagdelen erin zitten. `CampEnrollment.CampPackageId` koppelt een deelnemer aan een pakket. `CampPricing` blijft de enige prijsberekening: basis = pakketprijs (of `Camp.Price`) min `CampPrice.Discount`. Een nieuwe `CampCapacity` telt per dagdeel.

**Tech Stack:** .NET 10, EF Core + PostgreSQL, NUnit + NSubstitute + FluentAssertions, Next.js 15, next-intl, vitest.

**Spec:** `docs/superpowers/specs/2026-10-26-kamp-pakketten-design.md`

## Global Constraints

- Branch `feat/kamp-pakketten`, nooit op `main`. Logische, atomaire conventional commits; eindigen met `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- Elke service filtert op `OrganizationId`; services geven `Result<T>` terug; geen exceptions voor businessfouten.
- Alle FK's `DeleteBehavior.Restrict`; EF-config via `IEntityTypeConfiguration<T>`; nieuwe entiteiten krijgen een tenant query filter in `ApplicationDbContext` zoals `CampPrice`.
- Geen hardcoded Nederlandse UI-strings: `frontend/messages/nl.json`. Geen em-dashes in teksten. Geen `any`. Zod: geen `z.coerce.number()`.
- Datums `yyyy-MM-dd`, tijden `HH:mm`, EUR, bedragen `decimal(10,2)`.
- Prijs per deelnemer = `max(0, basis - korting)`; basis = pakketprijs, of `Camp.Price` zonder pakket.
- Publiek: heeft het kamp pakketten, dan is `CampPackageId` verplicht voor leider en elk lid. Zonder pakketten wordt een meegestuurd `CampPackageId` genegeerd. Admin mag `CampPackageId = null` (= hoofdprijs, telt voor alle dagdelen).
- Limiet per dagdeel enkel als het kamp pakketten heeft en `MaxParticipants` gezet is; anders blijft het totaal-gedrag.
- Reset/seed-run is niet nodig (gebruiker), maar `seed-demo-data.py` moet het nieuwe contract volgen.

## Review Focus

- Kamp bijwerken verwijdert alle dagen en maakt ze opnieuw: een pakket dat naar een verdwenen (datum, dagdeel) wijst moet geweigerd worden, niet stil kapot gaan.
- Een pakket dat een inschrijving gebruikt kan niet verwijderd worden (zoals tarieven).
- Groep met kinderen op verschillende pakketten: totaal = som per kind; geannuleerde leden tellen niet.
- Groep waarvan één pakket vol zit: de hele inschrijving wordt geweigerd, niets half opgeslagen.
- Bestaande inschrijvingen zonder pakket tellen mee in elk dagdeel van een kamp dat achteraf pakketten krijgt.
- Pakket met prijs 0 of volledig weggekorte prijs: kamp-inschrijving is gratis (bevestigd, geen betaalstap).
- Migratie: tarief duurder dan de hoofdprijs wordt korting 0, niet negatief.

---

### Task 1: Domein, EF-configuratie, migratie

**Files:**
- Create: `backend/CoachOS.Domain/Entities/CampDayPart.cs`, `CampPackage.cs`, `CampPackageSlot.cs`
- Modify: `Camp.cs` (`ICollection<CampPackage> Packages`), `CampDay.cs` (`ICollection<CampDayPart> Parts`), `CampEnrollment.cs` (`Guid? CampPackageId`, `CampPackage? Package`), `CampPrice.cs` (`Amount` wordt `Discount`, summary aanpassen: vast kortingsbedrag op de pakket- of hoofdprijs)
- Create: `Infrastructure/Persistence/Configurations/CampDayPartConfiguration.cs`, `CampPackageConfiguration.cs`, `CampPackageSlotConfiguration.cs`
- Modify: `CampPriceConfiguration.cs`, `CampEnrollmentConfiguration.cs`, `ApplicationDbContext.cs` (DbSets, `ApplyConfiguration`, query filters op `CampDayPart`, `CampPackage`, `CampPackageSlot` op `OrganizationId`), `CampRepository.cs`, `ICampRepository.cs`, `CampEnrollmentRepository.cs`
- Create: migratie `AddCampPackagesAndDayParts` (EF)
- Test: `backend/CoachOS.Tests/Repositories/CampPricingLoadingTests.cs`

**Interfaces:**
- Produces: `CampDayPart { Guid Id; Guid OrganizationId; Guid CampDayId; int Order; string Name; TimeOnly StartTime; TimeOnly EndTime }`; `CampPackage { Guid Id; Guid OrganizationId; Guid CampId; string Name; string? Description; decimal Price; int SortOrder; ICollection<CampPackageSlot> Slots }`; `CampPackageSlot { Guid Id; Guid OrganizationId; Guid CampPackageId; DateOnly Date; int PartOrder }`; `CampPrice.Discount`.
- `ICampRepository`: `void RemovePackages(IEnumerable<CampPackage>)`, `void RemovePackageSlots(IEnumerable<CampPackageSlot>)`; `RemoveDays` verwijdert ook de `Parts`.
- `ICampEnrollmentRepository`: `Task<bool> AnyUsingPackagesAsync(IEnumerable<Guid> packageIds, CancellationToken)`; `Task<List<CampEnrollment>> GetActiveByCampAsync(Guid campId, CancellationToken)` (AsNoTracking, status Pending/Confirmed/PendingPayment, alleen kolommen die de capaciteit nodig heeft: `Id`, `CampPackageId`).
- Include `Packages` met `Slots` en `Days` met `Parts` in `GetByIdWithDetailsAsync` en `GetByIdPublicAsync`; `GetByCampWithResponsesAsync` include `Package`.

- [ ] **Step 1: Breid `CampPricingLoadingTests` uit** met `PaymentLoadPath_PackagePriceAndDiscountAreUsed`: kamp 120, pakket "Ochtenden" 60 met één slot, tarief "Leden" `Discount = 10`; solo op pakket+tarief verwacht 50, groep met leider (pakket+tarief) en lid (hoofdprijs) verwacht 170 via `CampPricing.Total` na laden met `CampRepository.GetByIdPublicAsync`. Het bestaande tarieftest gebruikt `Discount = 40` (was `Amount = 80` bij prijs 120).
- [ ] **Step 2: Run** `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~CampPricingLoadingTests"`. Expected: compileerfout / FAIL.
- [ ] **Step 3: Implementeer entiteiten, configs, DbContext, repository-includes en de twee nieuwe repo-methodes.** Max lengtes: pakketnaam 120, omschrijving 500, dagdeelnaam 60. Indexen: `CampPackage(CampId, SortOrder)`, `CampPackageSlot(CampPackageId)`, `CampDayPart(CampDayId)`, unieke `CampPackageSlot(CampPackageId, Date, PartOrder)`.
- [ ] **Step 4: Maak de migratie** `dotnet ef migrations add AddCampPackagesAndDayParts --project CoachOS.Infrastructure --startup-project CoachOS.API` en pas ze aan: voeg `Discount` toe (default 0), zet vóór het droppen van `Amount` via `migrationBuilder.Sql` `Discount = GREATEST(0, camp."Price" - price."Amount")` (join op `CampId`), drop `Amount`. Controleer dat `Down` `Amount` terugzet als `GREATEST(0, camp."Price" - Discount)`.
- [ ] **Step 5: Run** de test uit Step 2 en `dotnet build CoachOS.slnx`. Expected: PASS (pricing-assertions volgen in Task 2; tot dan mag enkel de laadtest op includes en `Discount` getest worden).
- [ ] **Step 6: Commit** `feat(kampen): dagdelen, pakketten en kortingsbedrag in het datamodel`.

### Task 2: Prijsberekening met pakketten en korting

**Files:**
- Modify: `Application/Camps/CampPricing.cs`, `DTOs/CampPriceDto.cs` (`Amount` wordt `Discount` in `CampPriceDto` en `SaveCampPriceRequest`), `Validators/SaveCampPriceRequestValidator.cs` (`Discount >= 0`, bericht "Korting van de prijsoptie mag niet negatief zijn"), `CampService.cs` (`NewPrice`, `SyncPricesAsync`, `MapPrices`, prijswijziging-detectie), `CampEnrollmentService.cs` (`CampPriceDto` mapping), `Export/*` indien `CampPrice.Amount` gebruikt wordt
- Test: `CoachOS.Tests/Services/CampPricingTests.cs`, `Validators/SaveCampPriceRequestValidatorTests.cs`, `CampServiceTests.cs`

**Interfaces:**
- Produces: `CampPricing.UnitPrice(Camp camp, CampEnrollment e)` = `Math.Max(0, Base - Discount)`, `Base` = prijs van `camp.Packages` met `e.CampPackageId`, anders `camp.Price`; onbekend tarief of pakket valt terug op hoofdprijs/geen korting. `Total` ongewijzigd (skip geannuleerd).

- [ ] **Step 1: Herschrijf `CampPricingTests`** met: `UnitPrice_WithoutPackageOrDiscount_IsMainPrice` (100), `UnitPrice_WithDiscount_SubtractsFromMainPrice` (korting 25 geeft 75), `UnitPrice_WithPackage_IsPackagePrice` (pakket 40 geeft 40), `UnitPrice_WithPackageAndDiscount_SubtractsDiscount` (40-10=30), `UnitPrice_DiscountLargerThanBase_IsZero`, `UnitPrice_UnknownPackage_FallsBackToMainPrice`, `Total_SumsMixedPackagesAndSkipsCancelled`.
- [ ] **Step 2: Run** `--filter FullyQualifiedName~CampPricingTests`. Expected: FAIL.
- [ ] **Step 3: Implementeer `CampPricing`** en hernoem `Amount` naar `Discount` overal in backend (de compiler wijst de plekken aan); pas bestaande tests aan die `Amount` gebruiken met dezelfde bedoeling (korting = hoofdprijs - oud bedrag).
- [ ] **Step 4: Run** `dotnet test CoachOS.slnx`. Expected: PASS.
- [ ] **Step 5: Commit** `feat(kampen): prijs per deelnemer = pakketprijs min kortingsbedrag`.

### Task 3: Dagdelen en pakketten opslaan bij kamp aanmaken en bijwerken

**Files:**
- Create: `DTOs/CampDayPartDto.cs` (`CampDayPartDto(int Order, string Name, string StartTime, string EndTime)`, `CreateCampDayPartRequest(string Name, string StartTime, string EndTime)`), `DTOs/CampPackageDto.cs` (`CampPackageSlotDto(string Date, int PartOrder)`, `CampPackageDto(Guid Id, string Name, string? Description, decimal Price, List<CampPackageSlotDto> Slots, bool IsFull = false)`, `SaveCampPackageRequest(Guid? Id, string Name, string? Description, decimal Price, List<CampPackageSlotDto> Slots)`), `Validators/SaveCampPackageRequestValidator.cs` (`MaxPackages = 10`)
- Modify: `CampDayDto` (+ `List<CampDayPartDto> Parts`), `CreateCampDayRequest` (+ `List<CreateCampDayPartRequest>? Parts = null`), `CreateCampRequest`/`UpdateCampRequest` (+ `List<SaveCampPackageRequest>? Packages = null`), `CampDetailDto`/`PublicCampDto` (+ `List<CampPackageDto> Packages`), `CreateCampRequestValidator.cs`, `UpdateCampRequestValidator.cs`, `CampService.cs`, `CampEnrollmentService.GetPublicCampAsync`
- Test: `Validators/CreateCampRequestValidatorTests.cs`, `Validators/UpdateCampRequestValidatorTests.cs`, `Validators/SaveCampPackageRequestValidatorTests.cs` (nieuw), `Services/CampServiceTests.cs`

**Interfaces:**
- Consumes: Task 1 repo-methodes.
- Produces (`CampService`, privé): `Error? ValidatePackageSlots(List<CreateCampDayRequest> days, List<SaveCampPackageRequest> packages)`; `Task<Error?> SyncPackagesAsync(Camp camp, Guid organizationId, List<SaveCampPackageRequest> incoming, CancellationToken ct)`.

Regels (validator): max 2 dagdelen per dag; dagdeelnaam verplicht ≤60; tijden `HH:mm`, einde na begin, dagdelen niet overlappend en binnen de dagtijden; heeft het kamp pakketten, dan heeft elke dag minstens één dagdeel; pakketnaam verplicht ≤120, omschrijving ≤500, prijs ≥ 0, minstens één slot, geen dubbele slots. Service: `Order` = index in de lijst; als er dagdelen zijn worden `CampDay.StartTime/EndTime` de min/max van de dagdelen; elk slot moet naar een bestaande (datum, `PartOrder`) in de nieuwe dagen wijzen, anders `ErrorCodes.Validation` "Een pakket verwijst naar een dagdeel dat niet meer bestaat."; een pakket dat een inschrijving gebruikt (`AnyUsingPackagesAsync`) kan niet verwijderd worden (`ErrorCodes.Conflict`); pakketprijs- of nieuwe/verwijderde-pakket-wijziging triggert `SyncAllPendingCashAmountsAsync` zoals prijswijziging nu.

- [ ] **Step 1: Schrijf validatortests**: ongeldige dagdelen (3 stuks, overlap, einde voor begin), kamp met pakketten en een dag zonder dagdelen, pakket zonder slots/naam, dubbele slots, geldig voorbeeld (ochtend 09:00-12:00 en namiddag 13:00-16:00 met pakket "Ochtenden").
- [ ] **Step 2: Schrijf servicetests** in `CampServiceTests` (bestaand stijl: NSubstitute-repos): `CreateAsync_WithPartsAndPackages_PersistsThemAndDerivesDayTimes`, `UpdateAsync_PackageSlotPointsToRemovedPart_Fails`, `UpdateAsync_RemovingPackageInUse_Fails`, `UpdateAsync_ChangingPackagePrice_RecalculatesPendingCashPayment`.
- [ ] **Step 3: Run** de gefilterde tests. Expected: FAIL.
- [ ] **Step 4: Implementeer** DTOs, validators, `BuildDays` (parts), `RemoveDays` (parts), `SyncPackagesAsync`, `ValidatePackageSlots`, mapping in `BuildDayDtosAsync`, `GetByIdAsync` en `GetPublicCampAsync` (`IsFull` komt in Task 4).
- [ ] **Step 5: Run** `dotnet test CoachOS.slnx`. Expected: PASS.
- [ ] **Step 6: Commit** `feat(kampen): dagdelen en pakketten aanmaken en bijwerken`.

### Task 4: Limiet per dagdeel en publiek inschrijven met pakketten

**Files:**
- Create: `Application/Camps/CampCapacity.cs`
- Modify: `DTOs/SubmitCampEnrollmentRequest.cs` (+ `Guid? CampPackageId`), `DTOs/CampGroupMemberDto.cs` (+ `Guid? CampPackageId`), `Validators/SubmitCampEnrollmentRequestValidator.cs`, `CampEnrollmentService.cs` (`GetPublicCampAsync` zet `IsFull`, `SubmitAsync`)
- Test: nieuw `CoachOS.Tests/Services/CampCapacityTests.cs`, `CampEnrollmentServiceTests.cs`

**Interfaces:**
- Produces:
  - `static class CampCapacity` met `static IReadOnlyList<(DateOnly Date, int PartOrder)> SlotsOf(Camp camp, Guid? packageId)` (slots van het pakket; zonder of onbekend pakket alle dagdelen van het kamp),
  - `static bool IsFull(Camp camp, IEnumerable<Guid?> existingPackageIds, IEnumerable<Guid?> newPackageIds)` (true als in één dagdeel bestaande + nieuwe dekking `MaxParticipants` overschrijdt; false als `MaxParticipants` null of het kamp geen pakketten heeft),
  - `static bool IsPackageFull(Camp camp, CampPackage package, IEnumerable<Guid?> existingPackageIds)` (voor `IsFull` in de publieke DTO: true als één van zijn dagdelen op de limiet zit).
- Gedrag `SubmitAsync`: heeft het kamp pakketten dan is `CampPackageId` van leider en leden verplicht en moet bij het kamp horen (`ErrorCodes.Validation` "Kies een pakket voor elke deelnemer."); zonder pakketten wordt het genegeerd. `isPaid` = `CampPricing.Total` over alle nieuwe deelnemers > 0. Limietcontrole gebeurt in de bestaande Serializable-transactie met `CampCapacity.IsFull` op `enrollments.GetActiveByCampAsync`; faalt hij, dan "Dit kamp is volzet." en niets wordt opgeslagen. Zonder pakketten blijft de bestaande totaalcontrole. Deelnemers krijgen hun `CampPackageId` mee.

- [ ] **Step 1: Schrijf `CampCapacityTests`**: kamp met max 2, ochtend/namiddag op één dag; pakket A (ochtend), B (namiddag), C (beide). Twee bestaande op A + nieuwe op B is niet vol; twee op A + nieuwe op C is vol; bestaande zonder pakket telt in elk dagdeel; `MaxParticipants` null nooit vol; kamp zonder pakketten nooit "vol" via deze klasse; `IsPackageFull` waar/onwaar.
- [ ] **Step 2: Schrijf servicetests**: solo met pakket slaat `CampPackageId` op en `RequiresPayment` volgt de pakketprijs; pakketprijs 0 geeft Confirmed/gratis; ontbrekend pakket bij kamp met pakketten faalt; groep met twee pakketten (60 en 90) levert totaal 150 voor de betaling; groep waarvan één pakket vol zit wordt geweigerd en `AddAsync` wordt niet aangeroepen.
- [ ] **Step 3: Run** gefilterde tests. Expected: FAIL.
- [ ] **Step 4: Implementeer** `CampCapacity`, validator-aanpassing (geen regels nodig voor pakketten behalve dat het id niet leeg-Guid mag zijn), `SubmitAsync` en `IsFull` in de publieke DTO.
- [ ] **Step 5: Run** `dotnet test CoachOS.slnx`. Expected: PASS.
- [ ] **Step 6: Commit** `feat(kampen): inschrijven voor een pakket met limiet per dagdeel`.

### Task 5: Beheer-flows, betaalbedrag en export

**Files:**
- Modify: `DTOs/CreateManualCampEnrollmentRequest.cs`, `UpdateCampEnrollmentRequest.cs`, `AddCampGroupMemberRequest.cs` (elk + `Guid? CampPackageId`), `DTOs/CampEnrollmentDto.cs` (+ `Guid? CampPackageId = null, string? CampPackageName = null` als laatste optionele parameters), `CampService.cs` (`ApplyPriceChangeAsync` wordt `ApplyPricingChangeAsync(camp, enrollment, newPriceId, newPackageId, organizationId, ct)`, `AddGroupMemberAsync`, `GetEnrollmentsAsync`), `CampEnrollmentService.cs` (`CreateManualAsync`, `GetPaymentOptionsAsync(Guid campId, Guid? campEnrollmentId, CancellationToken)`), `ICampEnrollmentService.cs`, `API/Endpoints/Public/GetCampPaymentOptionsEndpoint.cs` (optionele query `campEnrollmentId`), `Export/CampExportModel.cs` + `Infrastructure/Export/ClosedXmlCampWorkbookBuilder.cs` (kolom "Pakket")
- Test: `CampServiceTests.cs`, `CampEnrollmentServiceTests.cs`, `CampExportServiceTests.cs`, `ClosedXmlCampWorkbookBuilderTests.cs`

**Interfaces:**
- Consumes: `CampCapacity.IsFull`, `CampPricing`.
- Gedrag: een onbekend `CampPackageId` geeft `ErrorCodes.NotFound` "Pakket niet gevonden."; admin mag `null`. Pakketwijziging van een bestaande inschrijving controleert de limiet met de inschrijving zelf uitgesloten van de bestaande lijst, en volgt dezelfde betaalregels als een tariefwijziging (betaald of online lopend: `Conflict`; openstaande cash volgt het nieuwe totaal; bevestigd zonder betaling mag geen betaald pakket krijgen). `AddGroupMemberAsync` en `CreateManualAsync` controleren de limiet per dagdeel en bepalen `isPaid`/gratis uit pakket + tarief. `GetPaymentOptionsAsync` geeft met `campEnrollmentId` het totaal van de betaler (leider of solo, `CampAmount`-logica via `CampPricing.Total` over de groep) in `Price`, zonder id de hoofdprijs zoals nu.

- [ ] **Step 1: Schrijf tests**: handmatige inschrijving op vol pakket faalt; lid toevoegen met ander pakket verhoogt het openstaande cash-bedrag; pakket wijzigen van betaalde inschrijving geeft Conflict; pakket wijzigen naar vol dagdeel faalt maar eigen plek telt niet mee; `GetEnrollmentsAsync` vult pakketnaam; `GetPaymentOptionsAsync` met groep-id geeft de som; exportwerkboek bevat de kolom "Pakket".
- [ ] **Step 2: Run** gefilterde tests. Expected: FAIL.
- [ ] **Step 3: Implementeer** de wijzigingen hierboven.
- [ ] **Step 4: Run** `dotnet test CoachOS.slnx`. Expected: PASS.
- [ ] **Step 5: Commit** `feat(kampen): pakketten in handmatige inschrijving, bewerken, groepen, betaalbedrag en export`.

### Task 6: Frontend contract, hulpfuncties en teksten

**Files:**
- Modify: `frontend/lib/api/camps.ts` (types spiegelen de DTOs uit Task 3-5: `CampDayPartDto`, `CampPackageDto`, `CampPackageSlotDto`, `SaveCampPackageInput`; `discount` in plaats van `amount` bij prijsopties; `packages` op detail en publiek kamp; `parts` op dagen; `campPackageId`/`campPackageName` op inschrijving en `campPackageId` op submit/handmatig/bewerken/lid; `getCampPaymentOptions(campId, campEnrollmentId?)`)
- Create: `frontend/lib/camp-packages.ts` met `summarizePackageSlots(days: CampDayDto[], slots: CampPackageSlotDto[]): string[]` (een regel per dag: "ma 14 juli: Ochtend 09:00-12:00") en `packageLabel(pkg, formatMoney)`; test `frontend/lib/camp-packages.test.ts` (vitest)
- Modify: `frontend/messages/nl.json` (namespace `camps`: dagdelen, pakketten, korting, "Kies een pakket", "Vol", foutmeldingen)

**Interfaces:**
- Produces: de TypeScript-types en hulpfuncties waar Task 7-9 op bouwen.

- [ ] **Step 1: Schrijf `camp-packages.test.ts`**: twee dagen, pakket met ochtend op beide dagen geeft twee regels in datumvolgorde; pakket met beide dagdelen op één dag geeft "Ochtend 09:00-12:00, Namiddag 13:00-16:00".
- [ ] **Step 2: Run** `cd frontend && bunx vitest run lib/camp-packages.test.ts`. Expected: FAIL.
- [ ] **Step 3: Implementeer** types, helper en teksten.
- [ ] **Step 4: Run** de test en `bunx tsc --noEmit`. Expected: test PASS; `tsc` toont enkel fouten op de nog aan te passen componenten (die worden in Task 7-9 opgelost).
- [ ] **Step 5: Commit** `feat(kampen): frontendcontract en hulpfuncties voor pakketten`.

### Task 7: Kampwizard en kamp bewerken

**Files:**
- Modify: `app/(dashboard)/dashboard/camps/new/_types.ts`, `new/_components/camp-day-dialog.tsx` (1-2 dagdelen met naam en uren; dagtijden worden afgeleid), `camp-days-editor.tsx`, `step-2-days.tsx` (sectie "Pakketten" onder de dagen), `step-3-review.tsx`, `camp-prices-editor.tsx` (korting i.p.v. bedrag, label "Korting (EUR)"), `camps/_components/camp-days-card.tsx`, `camp-days-readonly-card.tsx`, `camp-price-dialog.tsx`, `camp-prices-card.tsx`, `camp-price-select.tsx` (toont "Leden (-€15)"), `camp-edit-form.tsx`, `camps/new/page.tsx`
- Create: `camps/_components/camp-package-dialog.tsx` (naam, omschrijving, prijs, raster dagen × dagdelen met vinkjes), `camp-packages-card.tsx` (lijst + toevoegen/bewerken/verwijderen, voor de bewerkpagina), `new/_components/camp-packages-editor.tsx` (zelfde voor de wizard)

**Interfaces:**
- Consumes: Task 6 types en helper.
- Gedrag: pakketten kunnen pas gemaakt worden als elke dag minstens één dagdeel heeft; verwijdert de gebruiker een dagdeel dat in een pakket zit, dan toont de UI de serverfout "Een pakket verwijst naar een dagdeel dat niet meer bestaat." via `getAxiosErrorMessages`.

- [ ] **Step 1:** Implementeer de dagdeel-invoer in `camp-day-dialog` en de afleiding van start/einde (min/max).
- [ ] **Step 2:** Implementeer `camp-package-dialog` + editors/cards en koppel ze aan wizard (payload `packages`) en bewerkpagina.
- [ ] **Step 3:** Pas tarief-UI aan naar korting.
- [ ] **Step 4: Run** `bunx tsc --noEmit` en `bunx eslint` op de gewijzigde mappen. Expected: geen fouten.
- [ ] **Step 5:** Controleer in de browser (dev server draait op :5317, backend uit Task 10) dat een kamp met dagdelen en twee pakketten aangemaakt en weer geopend kan worden.
- [ ] **Step 6: Commit** `feat(kampen): dagdelen en pakketten in de kampwizard en bij bewerken`.

### Task 8: Publiek inschrijfformulier en betaalpagina

**Files:**
- Modify: `app/(public)/camp/[campId]/page.tsx` (pakketkeuze per deelnemer: leider en elk groepslid; toont per pakket naam, prijs, dagen/uren via `summarizePackageSlots`; volle pakketten disabled met label "Vol"; verplicht als het kamp pakketten heeft; "Prijzen"-blok toont de pakketten; de "ter referentie"-tarieven verdwijnen daaruit want het zijn kortingen die de organisator toewijst), `app/(public)/camp-enrollment/payment/page.tsx` (geeft `campEnrollmentId` mee aan `getCampPaymentOptions`)

- [ ] **Step 1:** Implementeer de pakketkeuze + validatie ("Kies een pakket voor elke deelnemer") en de submit-payload (`campPackageId` bij leider en leden).
- [ ] **Step 2:** Pas de betaalpagina aan.
- [ ] **Step 3: Run** `bunx tsc --noEmit`. Expected: geen fouten.
- [ ] **Step 4:** Controleer in de browser: kamp zonder pakketten ziet geen keuze en werkt als vroeger; kamp met pakketten toont de keuze per kind.
- [ ] **Step 5: Commit** `feat(kampen): pakketkeuze per deelnemer op het publieke inschrijfformulier`.

### Task 9: Beheerschermen voor inschrijvingen

**Files:**
- Modify: `camps/_components/camp-manual-enrollment-dialog.tsx`, `camp-edit-enrollment-dialog.tsx`, `camp-add-member-dialog.tsx` (pakketkeuze naast de tariefkeuze, `null` = "Hele kamp, hoofdprijs"), `camp-enrollment-detail-dialog.tsx`, `camp-enrollments-section.tsx` (pakketnaam tonen), `camp-participant-fields.tsx` indien gedeeld

- [ ] **Step 1:** Voeg een `CampPackageSelect` (nieuw bestand `camps/_components/camp-package-select.tsx`, naar het voorbeeld van `camp-price-select.tsx`) toe en gebruik hem in de drie dialogs.
- [ ] **Step 2:** Toon pakket in detail en lijst.
- [ ] **Step 3: Run** `bunx tsc --noEmit` en `bun run build`. Expected: geslaagd.
- [ ] **Step 4: Commit** `feat(kampen): pakketkeuze in de beheerschermen voor inschrijvingen`.

### Task 10: Seed, documentatie en eindcontrole

**Files:**
- Modify: `backend/Scripts/seed-demo-data.py` (regel rond `"prices"`: `amount` wordt `discount`; voeg bij het demo-kamp dagdelen en twee pakketten toe), `CLAUDE.md`-secties of `docs/` waar kampprijzen beschreven staan (docs-maintainer-conventie), spec bijwerken met de afwijkingen (slots op (datum, dagdeel) i.p.v. FK naar dagdeel; e-mails en dashboardkalender niet aangepast)

- [ ] **Step 1:** Pas seed en docs aan.
- [ ] **Step 2: Run** `cd backend && dotnet build CoachOS.slnx && dotnet test CoachOS.slnx`, `cd frontend && bunx tsc --noEmit && bunx vitest run && bun run build`. Expected: alles groen.
- [ ] **Step 3:** Backend herbouwen (`docker-compose up -d --build backend`), `/health` 200 controleren (auto-migratie draait), en één keer door de UI: kamp met pakketten maken, publiek twee kinderen op verschillende pakketten inschrijven, in beheer de som controleren.
- [ ] **Step 4: Commit** `docs(kampen): seed en documentatie voor pakketten`; push de branch en open een PR (toegestaan; mergen doet de gebruiker).

## Self-review

- Spec: dagdelen (T1,T3,T7), pakketten (T1,T3,T7), keuze per kind (T4,T8), prijs/betaling (T2,T4,T5), limiet per dagdeel (T4,T5), korting (T1,T2,T7), migratie (T1), kamp zonder pakketten ongewijzigd (T4,T8). Niet in plan: e-mailtemplates en dashboardkalender (gemeld als bewuste uitzondering in Task 10).
- Types: `CampPackageDto.IsFull`, `SaveCampPackageRequest.Slots` en `CampPackageSlotDto(Date, PartOrder)` komen overeen in Task 3, 4 en 6.
