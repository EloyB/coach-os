# Betaalmethodes en inschrijfwijze op een lessenreeks — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Een admin kan per lessenreeks kiezen welke inschrijfwijzen (solo/groep) en betaalmethodes (online via Mollie / handmatig via overschrijving) toegelaten zijn; die keuzes worden publiek afgedwongen bij inschrijven en bij bevestigen.

**Architecture:** Vier booleans op `LessonSerie` sturen de UI en de validatie. Inschrijfwijze wordt afgedwongen in `EnrollmentService.SubmitEnrollmentAsync`. De betaalmethode-keuze bestaat al op de bevestigingspagina (`StudentConfirmationService`); die wordt beperkt tot de serie-vlaggen, en het cash-pad wijzigt van "meteen betaald" naar camp-stijl (`PendingPayment` tot de admin bevestigt via een nieuwe mark-cash-paid actie).

**Tech Stack:** .NET 10 (Clean Architecture + service pattern, `Result<T>`, FluentValidation, Mapperly, EF Core/PostgreSQL), Next.js 15 App Router + TypeScript + Tailwind + React Query + next-intl, Playwright E2E.

## Global Constraints

- Elke service filtert op `OrganizationId`; endpoints halen die uit de JWT via `ctx.GetOrganizationId()`.
- Services gooien geen exceptions voor business-fouten → `Result<T>.Fail(new Error(ErrorCodes.X, "..."))`.
- Geen fluent config in `ApplicationDbContext` → `IEntityTypeConfiguration<T>`.
- Geen `DeleteBehavior.Cascade` (niet relevant hier: enkel booleans).
- Geen hardcoded strings: FE via `messages/nl.json` + `useTranslations`; nieuwe user-facing errormeldingen in het Nederlands (bestaande services gebruiken inline NL-strings — volg dat patroon).
- Geen `any` in TypeScript. Geen `var` in C#. Read-only queries `.AsNoTracking()`. Async met `CancellationToken`.
- Zod v4: `z.number()` + `valueAsNumber: true`, nooit `z.coerce.number()`.
- Datums dd/MM/yyyy in UI, EUR, tijdzone CET/CEST.
- Na backend-werk dat het contract raakt: seed-scripts bijwerken; feature is pas done na groene `reset-db.sh --no-frontend` + `seed-demo-data.sh`.
- Commit-conventie: conventional commits; niet pushen, geen PR (gebruiker doet dat).

## File Structure

**Backend — nieuw:**
- `backend/CoachOS.Infrastructure/Migrations/<timestamp>_AddSerieEnrollmentPaymentFlags.cs` (gegenereerd)
- `backend/CoachOS.API/Endpoints/Enrollments/MarkEnrollmentCashPaidEndpoint.cs`

**Backend — gewijzigd:**
- `CoachOS.Domain/Entities/LessonSerie.cs` — 4 booleans
- `CoachOS.Infrastructure/Persistence/Configurations/LessonSerieConfiguration.cs` — defaults
- `CoachOS.Application/LessonSerie/DTOs/CreateLessonSerieRequest.cs`, `UpdateLessonSerieRequest.cs`, `LessonSerieDto.cs`
- `CoachOS.Application/LessonSerie/Validators/CreateLessonSerieRequestValidator.cs`, `UpdateLessonSerieRequestValidator.cs`
- `CoachOS.Application/Mappings/ApplicationMapper.cs` — `ToLessonSerie`, `ToLessonSerieDto`
- `CoachOS.Application/LessonSerie/LessonSerieService.cs` (+ ctor) — Mollie-gating
- `CoachOS.Application/Enrollments/EnrollmentService.cs` — solo/groep-afdwinging
- `CoachOS.Application/StudentConfirmation/DTOs/AssignmentDetailsDto.cs` — 2 vlaggen
- `CoachOS.Application/StudentConfirmation/StudentConfirmationService.cs` — gating + cash→PendingPayment + `MarkEnrollmentCashPaidAsync`
- `CoachOS.Application/StudentConfirmation/IStudentConfirmationService.cs`
- `CoachOS.Domain/Interfaces/IPaymentRepository.cs` + `CoachOS.Infrastructure/Repositories/PaymentRepository.cs` — `GetLatestPendingCashByEnrollmentIdAsync`

**Frontend — gewijzigd:**
- `frontend/lib/api/lessonSeries.ts` — 4 velden in request + dto
- `frontend/lib/api/confirmation.ts` — 2 vlaggen in AssignmentDetails-type
- `frontend/lib/api/enrollments.ts` — `markEnrollmentCashPaid`
- `frontend/app/(dashboard)/dashboard/lessons/new/_types.ts` + `_components/step-1-basisinfo.tsx` + `page.tsx`
- `frontend/app/(dashboard)/dashboard/lessons/[id]/page.tsx` — edit-form blokken + "markeer betaald"-actie
- `frontend/app/(public)/enroll/[seriesId]/page.tsx` — solo/groep-gating
- `frontend/app/confirmation/[token]/page.tsx` — betaalmethode-gating
- `frontend/messages/nl.json` — nieuwe labels

**Scripts:** `backend/Scripts/seed-data.json`, `backend/Scripts/seed-demo-data.py`

---

## Task 1: Domein + migratie — 4 booleans op LessonSerie

**Files:**
- Modify: `backend/CoachOS.Domain/Entities/LessonSerie.cs`
- Modify: `backend/CoachOS.Infrastructure/Persistence/Configurations/LessonSerieConfiguration.cs`
- Create: migratie (gegenereerd)

**Interfaces:**
- Produces: `LessonSerie.AllowSoloEnrollment`, `.AllowGroupEnrollment`, `.AcceptOnlinePayment`, `.AcceptManualPayment` — alle `bool`.

- [ ] **Step 1: Voeg de vier booleans toe aan de entity**

In `LessonSerie.cs`, na `public PaymentMode PaymentMode { get; set; } = PaymentMode.Immediate;` (regel ~43):

```csharp
    /// <summary>Leerling mag zich solo inschrijven op deze reeks.</summary>
    public bool AllowSoloEnrollment { get; set; } = true;

    /// <summary>Leerling mag zich als groep inschrijven op deze reeks.</summary>
    public bool AllowGroupEnrollment { get; set; } = true;

    /// <summary>Online betalen via Mollie toegestaan. Enkel true wanneer de org een MollieConnection heeft.</summary>
    public bool AcceptOnlinePayment { get; set; } = true;

    /// <summary>Handmatig betalen (overschrijving/cash) toegestaan; bevestigd door de admin.</summary>
    public bool AcceptManualPayment { get; set; }
```

- [ ] **Step 2: Zet DB-defaults in de EF-configuratie**

In `LessonSerieConfiguration.cs`, binnen `Configure(...)`, voeg toe (volg de bestaande `builder.Property(...)`-stijl in dat bestand):

```csharp
        builder.Property(s => s.AllowSoloEnrollment).HasDefaultValue(true);
        builder.Property(s => s.AllowGroupEnrollment).HasDefaultValue(true);
        builder.Property(s => s.AcceptOnlinePayment).HasDefaultValue(true);
        builder.Property(s => s.AcceptManualPayment).HasDefaultValue(true);
```

Let op: `AcceptManualPayment` krijgt DB-default `true` (níet `false`) zodat bestaande reeksen — die vandaag zowel cash als online aanbieden op de confirmation-pagina — hun gedrag behouden. De formulier-default voor nieuwe reeksen wordt in de frontend gezet (Task 4).

- [ ] **Step 3: Genereer de migratie**

Run:
```bash
cd backend
dotnet ef migrations add AddSerieEnrollmentPaymentFlags --project CoachOS.Infrastructure --startup-project CoachOS.API
```
Expected: nieuw migratiebestand met vier `AddColumn<bool>(... defaultValue: true)` in `Up()`.

- [ ] **Step 4: Verifieer de migratie compileert**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Domain/Entities/LessonSerie.cs backend/CoachOS.Infrastructure/Persistence/Configurations/LessonSerieConfiguration.cs backend/CoachOS.Infrastructure/Migrations/
git commit -m "feat(domain): inschrijfwijze- en betaalmethode-vlaggen op LessonSerie"
```

---

## Task 2: DTO's, validators en mapper

**Files:**
- Modify: `backend/CoachOS.Application/LessonSerie/DTOs/CreateLessonSerieRequest.cs`, `UpdateLessonSerieRequest.cs`, `LessonSerieDto.cs`
- Modify: `backend/CoachOS.Application/LessonSerie/Validators/CreateLessonSerieRequestValidator.cs`, `UpdateLessonSerieRequestValidator.cs`
- Modify: `backend/CoachOS.Application/Mappings/ApplicationMapper.cs`
- Test: `backend/CoachOS.Tests/Validators/CreateLessonSerieRequestValidatorTests.cs` (bestaand of nieuw)

**Interfaces:**
- Consumes: entity-velden uit Task 1.
- Produces: request-DTO's dragen `AllowSoloEnrollment`, `AllowGroupEnrollment`, `AcceptOnlinePayment`, `AcceptManualPayment` (alle `bool`); `LessonSerieDto` draagt dezelfde vier.

- [ ] **Step 1: Schrijf de falende validator-test**

Maak/《vul aan》 `backend/CoachOS.Tests/Validators/CreateLessonSerieRequestValidatorTests.cs`. Zoek eerst een bestaande valide-request helper in de tests (grep `new CreateLessonSerieRequest`); bouw daarop voort. Kern-tests:

```csharp
[Fact]
public void Fails_when_no_enrollment_mode_selected()
{
    CreateLessonSerieRequest req = ValidRequest() with
    {
        AllowSoloEnrollment = false,
        AllowGroupEnrollment = false,
    };
    TestValidationResult<CreateLessonSerieRequest> result = _validator.TestValidate(req);
    result.ShouldHaveValidationErrorFor(x => x.AllowSoloEnrollment);
}

[Fact]
public void Fails_when_no_payment_method_selected()
{
    CreateLessonSerieRequest req = ValidRequest() with
    {
        AcceptOnlinePayment = false,
        AcceptManualPayment = false,
    };
    TestValidationResult<CreateLessonSerieRequest> result = _validator.TestValidate(req);
    result.ShouldHaveValidationErrorFor(x => x.AcceptOnlinePayment);
}

[Fact]
public void Passes_with_solo_only_and_manual_only()
{
    CreateLessonSerieRequest req = ValidRequest() with
    {
        AllowSoloEnrollment = true, AllowGroupEnrollment = false,
        AcceptOnlinePayment = false, AcceptManualPayment = true,
    };
    _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
}
```

Als er nog geen `ValidRequest()`-helper is, schrijf er een die alle bestaande verplichte velden invult (kopieer uit een bestaande passerende test in dezelfde map).

- [ ] **Step 2: Voer de test uit — moet falen op compilatie/ontbrekende velden**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~CreateLessonSerieRequestValidatorTests"`
Expected: FAIL — `CreateLessonSerieRequest` bevat de nieuwe properties nog niet (compile error) of validatieregels ontbreken.

- [ ] **Step 3: Voeg de velden toe aan de request-DTO's**

In `CreateLessonSerieRequest.cs`, na `public Guid TennisClubId { get; init; }`:

```csharp
    public bool AllowSoloEnrollment { get; init; } = true;
    public bool AllowGroupEnrollment { get; init; } = true;
    public bool AcceptOnlinePayment { get; init; } = true;
    public bool AcceptManualPayment { get; init; }
```

In `UpdateLessonSerieRequest.cs`, dezelfde vier `init`-properties toevoegen.

In `LessonSerieDto.cs`, na `public Guid TennisClubId { get; set; }`:

```csharp
    public bool AllowSoloEnrollment { get; set; }
    public bool AllowGroupEnrollment { get; set; }
    public bool AcceptOnlinePayment { get; set; }
    public bool AcceptManualPayment { get; set; }
```

- [ ] **Step 4: Voeg de "minstens één"-regels toe aan beide validators**

In `CreateLessonSerieRequestValidator.cs` (constructor) en identiek in `UpdateLessonSerieRequestValidator.cs`:

```csharp
        RuleFor(x => x.AllowSoloEnrollment)
            .Must((req, _) => req.AllowSoloEnrollment || req.AllowGroupEnrollment)
            .WithMessage("Kies minstens één inschrijfwijze (solo of groep).");

        RuleFor(x => x.AcceptOnlinePayment)
            .Must((req, _) => req.AcceptOnlinePayment || req.AcceptManualPayment)
            .WithMessage("Kies minstens één betaalmethode (online of overschrijving).");
```

- [ ] **Step 5: Map de velden in de mapper**

In `ApplicationMapper.cs`, `ToLessonSerie(...)` — voeg binnen de object-initializer toe (na `IsActive = true,`):

```csharp
            AllowSoloEnrollment = request.AllowSoloEnrollment,
            AllowGroupEnrollment = request.AllowGroupEnrollment,
            AcceptOnlinePayment = request.AcceptOnlinePayment,
            AcceptManualPayment = request.AcceptManualPayment,
```

In `ToLessonSerieDto(...)` — voeg toe (na `TennisClubAddress = ...`):

```csharp
            AllowSoloEnrollment = ls.AllowSoloEnrollment,
            AllowGroupEnrollment = ls.AllowGroupEnrollment,
            AcceptOnlinePayment = ls.AcceptOnlinePayment,
            AcceptManualPayment = ls.AcceptManualPayment,
```

- [ ] **Step 6: Update `UpdateAsync` zodat de velden bewaard worden**

In `LessonSerieService.cs`, `UpdateAsync(...)`, na `series.TennisClubId = request.TennisClubId;` (regel ~181):

```csharp
        series.AllowSoloEnrollment = request.AllowSoloEnrollment;
        series.AllowGroupEnrollment = request.AllowGroupEnrollment;
        series.AcceptOnlinePayment = request.AcceptOnlinePayment;
        series.AcceptManualPayment = request.AcceptManualPayment;
```

- [ ] **Step 7: Voer de tests uit — moeten slagen**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~CreateLessonSerieRequestValidatorTests"`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add backend/CoachOS.Application backend/CoachOS.Tests
git commit -m "feat(application): DTO's, validators en mapping voor serie-vlaggen"
```

---

## Task 3: Mollie-gating in LessonSerieService (create + update)

**Files:**
- Modify: `backend/CoachOS.Application/LessonSerie/LessonSerieService.cs` (ctor + `CreateAsync` + `UpdateAsync`)
- Test: `backend/CoachOS.Tests/Services/LessonSerieServiceTests.cs` (bestaand of nieuw)

**Interfaces:**
- Consumes: `IMollieConnectionRepository.GetByOrganizationReadOnlyAsync(Guid organizationId, CancellationToken ct)` → `MollieConnection?` (rij bestaat = verbonden).
- Produces: `CreateAsync`/`UpdateAsync` falen met `ErrorCodes.Validation` als `AcceptOnlinePayment == true` en er geen `MollieConnection` is.

- [ ] **Step 1: Schrijf de falende service-test**

In `LessonSerieServiceTests.cs` (volg de bestaande arrange met `Substitute.For<...>` voor alle repos; voeg een `IMollieConnectionRepository` substitute toe):

```csharp
[Fact]
public async Task CreateAsync_rejects_online_payment_without_mollie_connection()
{
    Guid orgId = Guid.NewGuid();
    _tennisClubRepo.ExistsAsync(Arg.Any<Guid>(), orgId, Arg.Any<CancellationToken>()).Returns(true);
    _mollieConnectionRepo.GetByOrganizationReadOnlyAsync(orgId, Arg.Any<CancellationToken>())
        .Returns((MollieConnection?)null);

    CreateLessonSerieRequest request = ValidCreateRequest() with { AcceptOnlinePayment = true };

    Result<Guid> result = await _service.CreateAsync(orgId, request, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
}
```

- [ ] **Step 2: Voer uit — faalt op ontbrekende ctor-dependency / gedrag**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~LessonSerieServiceTests.CreateAsync_rejects_online_payment_without_mollie_connection"`
Expected: FAIL (compile: `_mollieConnectionRepo` bestaat niet, of gedrag ontbreekt).

- [ ] **Step 3: Injecteer de Mollie-repo in de service**

In `LessonSerieService.cs`, breid de primary constructor uit:

```csharp
public class LessonSerieService(
    ILessonSerieRepository lessonSeriesRepo,
    ILessonRepository lessonRepo,
    IEnrollmentRepository enrollmentRepo,
    ITennisClubRepository tennisClubRepo,
    IUserLookupService userLookup,
    IEmailService emailService,
    IMollieConnectionRepository mollieConnectionRepo,
    ApplicationMapper mapper) : ILessonSerieService
```

(`IMollieConnectionRepository` zit in `CoachOS.Domain.Interfaces` — die `using` staat al in het bestand. `IMollieConnectionRepository` is al in DI geregistreerd, dus geen DI-wijziging nodig.)

- [ ] **Step 4: Voeg een gedeelde gating-helper toe en roep hem aan**

In `LessonSerieService.cs`, voeg een private helper toe:

```csharp
    private async Task<Error?> ValidateOnlinePaymentAsync(
        Guid organizationId, bool acceptOnlinePayment, CancellationToken ct)
    {
        if (!acceptOnlinePayment)
            return null;

        MollieConnection? connection =
            await mollieConnectionRepo.GetByOrganizationReadOnlyAsync(organizationId, ct);
        if (connection is null)
            return new Error(ErrorCodes.Validation,
                "Online betalen kan pas aangezet worden nadat de organisatie met Mollie verbonden is.");

        return null;
    }
```

In `CreateAsync`, meteen na de club-check (na regel ~90):

```csharp
        Error? onlinePaymentError =
            await ValidateOnlinePaymentAsync(organizationId, request.AcceptOnlinePayment, ct);
        if (onlinePaymentError is not null)
            return Result<Guid>.Fail(onlinePaymentError);
```

In `UpdateAsync`, meteen na de club-check (na regel ~170):

```csharp
        Error? onlinePaymentError =
            await ValidateOnlinePaymentAsync(organizationId, request.AcceptOnlinePayment, ct);
        if (onlinePaymentError is not null)
            return Result<LessonSerieDto>.Fail(onlinePaymentError);
```

- [ ] **Step 5: Voer de test uit — slaagt**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~LessonSerieServiceTests"`
Expected: PASS (ook bestaande tests; pas hun ctor-aanroep aan met de nieuwe repo-substitute indien nodig).

- [ ] **Step 6: Commit**

```bash
git add backend/CoachOS.Application/LessonSerie/LessonSerieService.cs backend/CoachOS.Tests
git commit -m "feat(application): weiger online betaling zonder Mollie-koppeling op reeks"
```

---

## Task 4: Create-/edit-formulier — checkboxes + Mollie-gating (frontend)

**Files:**
- Modify: `frontend/lib/api/lessonSeries.ts`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/new/_types.ts`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/new/_components/step-1-basisinfo.tsx`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/new/page.tsx`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/[id]/page.tsx` (edit-form)
- Modify: `frontend/messages/nl.json`

**Interfaces:**
- Consumes: `getMollieStatus()` uit `lib/api/mollieConnect.ts` (grep de exacte exportnaam; die levert `{ connected: boolean, ... }`).
- Produces: `CreateLessonSeriesRequest` en `LessonSeriesDto` (TS) dragen de vier booleans; `Step1Data` draagt ze.

- [ ] **Step 1: Voeg de velden toe aan de TS-types**

In `frontend/lib/api/lessonSeries.ts`, in `interface CreateLessonSeriesRequest` en in `interface LessonSeriesDto`, voeg toe:

```typescript
  allowSoloEnrollment: boolean;
  allowGroupEnrollment: boolean;
  acceptOnlinePayment: boolean;
  acceptManualPayment: boolean;
```

(In `UpdateLessonSeriesRequest` idem.)

In `frontend/app/(dashboard)/dashboard/lessons/new/_types.ts`, breid `Step1Data` uit met dezelfde vier `boolean`-velden.

- [ ] **Step 2: Toon de Mollie-status in step 1 en render de blokken**

In `step-1-basisinfo.tsx`:
- Haal de Mollie-status op met React Query: `const { data: mollie } = useQuery({ queryKey: ["mollieStatus"], queryFn: getMollieStatus });` en leid `const mollieConnected = mollie?.connected ?? false;` af.
- Registreer de vier checkboxes via `register("allowSoloEnrollment")` enz. (react-hook-form; booleans → gewone checkbox, geen `valueAsNumber`).
- Twee nieuwe secties onderaan de bestaande velden, met dezelfde `label`-styling als de rest van het formulier:

```tsx
{/* Inschrijfwijze */}
<fieldset className="mt-6">
  <legend className="block text-sm font-medium text-gray-700 mb-1.5">
    {t("enrollmentMode.label")}
  </legend>
  <label className="flex items-center gap-2">
    <input type="checkbox" {...register("allowSoloEnrollment")} />
    <span>{t("enrollmentMode.solo")}</span>
  </label>
  <label className="flex items-center gap-2">
    <input type="checkbox" {...register("allowGroupEnrollment")} />
    <span>{t("enrollmentMode.group")}</span>
  </label>
</fieldset>

{/* Betaalmethodes */}
<fieldset className="mt-6">
  <legend className="block text-sm font-medium text-gray-700 mb-1.5">
    {t("paymentMethods.label")}
  </legend>
  <label className={`flex items-center gap-2 ${!mollieConnected ? "opacity-50" : ""}`}>
    <input
      type="checkbox"
      disabled={!mollieConnected}
      {...register("acceptOnlinePayment")}
    />
    <span>{t("paymentMethods.online")}</span>
  </label>
  {!mollieConnected && (
    <p className="text-xs text-gray-500 mt-1">
      {t("paymentMethods.mollieRequired")}{" "}
      <a href="/dashboard/settings" className="underline">{t("paymentMethods.connectLink")}</a>
    </p>
  )}
  <label className="flex items-center gap-2 mt-2">
    <input type="checkbox" {...register("acceptManualPayment")} />
    <span>{t("paymentMethods.manual")}</span>
  </label>
</fieldset>
```

- [ ] **Step 3: Zet de juiste defaults bij het initialiseren van het formulier**

In `page.tsx` (waar `useForm`/`defaultValues` voor step 1 leeft), zet:
- `allowSoloEnrollment: true`, `allowGroupEnrollment: true`.
- `acceptOnlinePayment`: gelijk aan `mollieConnected`; `acceptManualPayment`: gelijk aan `!mollieConnected`.

Als de defaults vóór het laden van de Mollie-status gezet worden, gebruik een `useEffect` die `setValue("acceptOnlinePayment", mollieConnected)` en `setValue("acceptManualPayment", !mollieConnected)` zet zodra `mollie` binnen is en de gebruiker nog niets veranderde (bijv. een `hasTouchedPayment`-ref).

- [ ] **Step 4: Stuur de velden mee bij submit**

In `page.tsx`, waar het `CreateLessonSeriesRequest`-object gebouwd wordt vóór de create-mutatie, neem de vier velden uit de step-1-data mee.

- [ ] **Step 5: Voeg dezelfde blokken toe aan het edit-formulier**

In `app/(dashboard)/dashboard/lessons/[id]/page.tsx`, in het bewerk-formulier van de reeks: dezelfde twee secties, voorgevuld uit de `LessonSeriesDto` (`allowSoloEnrollment` enz.), met dezelfde Mollie-gating. Neem de vier velden op in het `UpdateLessonSeriesRequest`-payload bij opslaan.

- [ ] **Step 6: Voeg de vertalingen toe**

In `frontend/messages/nl.json`, in de namespace die step 1 gebruikt (grep de `useTranslations("...")`-key bovenaan `step-1-basisinfo.tsx`), voeg toe:

```json
"enrollmentMode": {
  "label": "Inschrijfwijze",
  "solo": "Solo inschrijven",
  "group": "In groep inschrijven"
},
"paymentMethods": {
  "label": "Betaalmethodes",
  "online": "Online betalen (Mollie)",
  "manual": "Overschrijving",
  "mollieRequired": "Online betalen kan pas nadat je met Mollie verbonden bent.",
  "connectLink": "Verbind Mollie in instellingen"
}
```

- [ ] **Step 7: Verifieer de build**

Run: `cd frontend && bun run build`
Expected: build slaagt, geen type-fouten.

- [ ] **Step 8: Commit**

```bash
git add frontend/lib/api/lessonSeries.ts "frontend/app/(dashboard)/dashboard/lessons/new" "frontend/app/(dashboard)/dashboard/lessons/[id]/page.tsx" frontend/messages/nl.json
git commit -m "feat(frontend): inschrijfwijze- en betaalmethode-keuze in reeksformulier"
```

---

## Task 5: Inschrijfwijze afdwingen (enroll-pagina + EnrollmentService)

**Files:**
- Modify: `backend/CoachOS.Application/Enrollments/EnrollmentService.cs`
- Test: `backend/CoachOS.Tests/Services/EnrollmentServiceTests.cs` (bestaand)
- Modify: `frontend/app/(public)/enroll/[seriesId]/page.tsx`

**Interfaces:**
- Consumes: `LessonSerie.AllowSoloEnrollment`, `.AllowGroupEnrollment`; `SubmitEnrollmentRequest.EnrollmentType` (`"solo"` | `"group"`).
- Produces: `SubmitEnrollmentAsync` faalt met `ErrorCodes.Validation` bij een niet-toegelaten inschrijfwijze.

- [ ] **Step 1: Schrijf de falende test**

In `EnrollmentServiceTests.cs` (volg de bestaande arrange; de `seriesRepo`/`lessonSeriesRepo`-substitute levert een `LessonSerie`):

```csharp
[Fact]
public async Task Submit_rejects_group_when_series_is_solo_only()
{
    LessonSerie series = ValidOpenSeries();
    series.AllowSoloEnrollment = true;
    series.AllowGroupEnrollment = false;
    _lessonSeriesRepo.GetByIdAsync(series.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
        .Returns(series);

    SubmitEnrollmentRequest request = ValidSubmit(series) with
    {
        EnrollmentType = "group",
        GroupMembers = new() { new GroupMemberDto { StudentName = "Bob", DateOfBirth = "2000-01-01" } },
    };

    Result<Guid> result = await _service.SubmitEnrollmentAsync(series.Id, request, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
}
```

(Gebruik de bestaande helpernamen in dat testbestand; pas `ValidOpenSeries`/`ValidSubmit` aan of maak ze op basis van een bestaande passerende test.)

- [ ] **Step 2: Voer uit — faalt (geen gating)**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~EnrollmentServiceTests.Submit_rejects_group_when_series_is_solo_only"`
Expected: FAIL (inschrijving slaagt nog).

- [ ] **Step 3: Voeg de gating toe in `SubmitEnrollmentAsync`**

In `EnrollmentService.cs`, ná het laden/valideren van de reeks en vóór de leeftijdscheck (rond regel ~235, waar `groupSize` bepaald wordt), voeg toe:

```csharp
        bool wantsGroup = request.EnrollmentType == "group";
        if (wantsGroup && !series.AllowGroupEnrollment)
            return Result<Guid>.Fail(new Error(
                ErrorCodes.Validation, "Inschrijven in groep is niet mogelijk voor deze lessenreeks."));
        if (!wantsGroup && !series.AllowSoloEnrollment)
            return Result<Guid>.Fail(new Error(
                ErrorCodes.Validation, "Solo inschrijven is niet mogelijk voor deze lessenreeks."));
```

(De variabele die de geladen reeks bevat, heet in deze methode `series` — verifieer de exacte naam bij het laden bovenaan de methode.)

- [ ] **Step 4: Voer de test uit — slaagt**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~EnrollmentServiceTests"`
Expected: PASS.

- [ ] **Step 5: Gate de enroll-pagina (frontend)**

In `frontend/app/(public)/enroll/[seriesId]/page.tsx`:
- De reeks-data (grep hoe de serie geladen wordt — via `lib/api/lessonSeries.ts`) levert nu `allowSoloEnrollment`/`allowGroupEnrollment`.
- Vervang de harde default `useState<"solo" | "group">("solo")` (regel ~148) door een default die de eerst-toegelaten wijze kiest:

```tsx
const [enrollmentType, setEnrollmentType] = useState<"solo" | "group">(
  series.allowSoloEnrollment ? "solo" : "group"
);
```

- Render de solo-radio enkel als `series.allowSoloEnrollment`, de groep-radio enkel als `series.allowGroupEnrollment` (regel ~745–790). Is er maar één toegelaten, toon dan geen keuze-UI (de `enrollmentType` staat al vast).

- [ ] **Step 6: Verifieer FE-build**

Run: `cd frontend && bun run build`
Expected: slaagt.

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Application/Enrollments/EnrollmentService.cs backend/CoachOS.Tests "frontend/app/(public)/enroll/[seriesId]/page.tsx"
git commit -m "feat: dwing toegelaten inschrijfwijze af bij inschrijven"
```

---

## Task 6: Betaalvlaggen naar de bevestigingspagina

**Files:**
- Modify: `backend/CoachOS.Application/StudentConfirmation/DTOs/AssignmentDetailsDto.cs`
- Modify: `backend/CoachOS.Application/StudentConfirmation/StudentConfirmationService.cs` (`BuildDetailsAsync`)
- Modify: `frontend/lib/api/confirmation.ts`
- Modify: `frontend/app/confirmation/[token]/page.tsx`

**Interfaces:**
- Produces: `AssignmentDetailsDto.AcceptOnlinePayment`, `.AcceptManualPayment` (bool); FE `AssignmentDetails`-type draagt `acceptOnlinePayment`, `acceptManualPayment`.

- [ ] **Step 1: Voeg de vlaggen toe aan de DTO**

In `AssignmentDetailsDto.cs`, na `public DateTime ExpiresAt { get; init; }`:

```csharp
    public bool AcceptOnlinePayment { get; init; }
    public bool AcceptManualPayment { get; init; }
```

- [ ] **Step 2: Vul ze in `BuildDetailsAsync`**

In `StudentConfirmationService.cs`, `BuildDetailsAsync(...)`, in de `new AssignmentDetailsDto { ... }` (rond regel ~396), voeg toe (de `series` is daar al geladen):

```csharp
            AcceptOnlinePayment = series.AcceptOnlinePayment,
            AcceptManualPayment = series.AcceptManualPayment,
```

- [ ] **Step 3: Voeg de velden toe aan het FE-type**

In `frontend/lib/api/confirmation.ts`, in het `AssignmentDetails`-interface (grep de exacte naam), voeg toe:

```typescript
  acceptOnlinePayment: boolean;
  acceptManualPayment: boolean;
```

- [ ] **Step 4: Gate de betaalmethode-tegels op de confirmation-pagina**

In `frontend/app/confirmation/[token]/page.tsx`:
- De default-`useState<1 | 2>(2)` (regel ~77) wordt afhankelijk van de vlaggen. Zet de default op de enige toegelaten optie:

```tsx
const [paymentMethod, setPaymentMethod] = useState<1 | 2>(
  details.acceptManualPayment ? 2 : 1
);
```

- Render de cash-tegel (regel ~342) enkel als `details.acceptManualPayment`, en de online-tegel (regel ~360) enkel als `details.acceptOnlinePayment`. Is er maar één toegelaten, toon dan enkel die (geen keuze).

- [ ] **Step 5: Verifieer builds**

Run: `cd backend && dotnet build CoachOS.slnx && cd ../frontend && bun run build`
Expected: beide slagen.

- [ ] **Step 6: Commit**

```bash
git add backend/CoachOS.Application/StudentConfirmation "frontend/lib/api/confirmation.ts" "frontend/app/confirmation/[token]/page.tsx"
git commit -m "feat: toon enkel toegelaten betaalmethodes op bevestigingspagina"
```

---

## Task 7: Server-side betaalmethode-gating + cash → camp-stijl

**Files:**
- Modify: `backend/CoachOS.Application/StudentConfirmation/StudentConfirmationService.cs` (`ConfirmAsync` + `PickAlternativeAsync`)
- Test: `backend/CoachOS.Tests/Services/StudentConfirmationServiceTests.cs`

**Interfaces:**
- Consumes: `LessonSerie.AcceptOnlinePayment`, `.AcceptManualPayment`.
- Produces: `ConfirmAsync`/`PickAlternativeAsync` weigeren een niet-toegelaten methode; cash-pad zet enrollment op `PendingPayment` met `Payment{ Status = Pending }`.

- [ ] **Step 1: Schrijf de falende tests**

In `StudentConfirmationServiceTests.cs` (volg de bestaande arrange; de `seriesRepo`-substitute levert de reeks):

```csharp
[Fact]
public async Task Confirm_rejects_online_when_series_disallows_online()
{
    // series.AcceptOnlinePayment = false, AcceptManualPayment = true
    // request.PaymentMethod = 1 (Online)
    // → Result.Fail met ErrorCodes.Validation, geen Mollie-call
}

[Fact]
public async Task Confirm_cash_sets_pending_payment_not_paid()
{
    // series.AcceptManualPayment = true; request.PaymentMethod = 2 (Cash)
    // → paymentRepo.AddAsync ontvangt Payment met Status == PaymentStatus.Pending
    // → enrollment-status wordt PendingPayment (niet Confirmed)
}
```

Werk deze uit met de bestaande substitutes: assert op `_paymentRepo.Received().AddAsync(Arg.Is<Payment>(p => p.Status == PaymentStatus.Pending), ...)` en op de enrollment-status via de mechaniek die de bestaande tests gebruiken.

- [ ] **Step 2: Voer uit — faalt**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~StudentConfirmationServiceTests"`
Expected: FAIL (huidige code staat online toe en zet cash op `Paid`/`Confirmed`).

- [ ] **Step 3: Voeg de methode-gating toe in `ConfirmAsync`**

In `StudentConfirmationService.cs`, `ConfirmAsync`, meteen na `var series = await seriesRepo.GetByIdAsync(...)` + null-check (na regel ~46):

```csharp
        Error? methodError = ValidatePaymentMethodAllowed(method, series);
        if (methodError is not null)
            return Result<ConfirmResultDto>.Fail(methodError);
```

Voeg een private helper toe:

```csharp
    private static Error? ValidatePaymentMethodAllowed(
        PaymentMethod method, Domain.Entities.LessonSerie series)
    {
        if (method == PaymentMethod.Online && !series.AcceptOnlinePayment)
            return new Error(ErrorCodes.Validation, "Online betalen is niet mogelijk voor deze lessenreeks.");
        if (method == PaymentMethod.Cash && !series.AcceptManualPayment)
            return new Error(ErrorCodes.Validation, "Betalen via overschrijving is niet mogelijk voor deze lessenreeks.");
        return null;
    }
```

- [ ] **Step 4: Zet het cash-pad om naar camp-stijl in `ConfirmAsync`**

Vervang het cash-blok (regels ~71–91) door:

```csharp
        if (method == PaymentMethod.Cash)
        {
            // Camp-stijl: registreer een openstaande cash-betaling; de club bevestigt later.
            Payment cashPayment = new()
            {
                OrganizationId = token.OrganizationId,
                EnrollmentId = token.EnrollmentId,
                Amount = cashBreakdown!.Total,
                Status = PaymentStatus.Pending,
                Method = PaymentMethod.Cash,
                Description = $"Overschrijving — {series.Name}",
            };
            await paymentRepo.AddAsync(cashPayment, ct);

            ConfirmEnrollmentStatuses(assignment, EnrollmentStatus.PendingPayment);
            await paymentRepo.SaveChangesAsync(ct);

            // Géén TryFinalizeSeriesAsync: de reeks is pas rond zodra de betaling bevestigd is.
            return Result<ConfirmResultDto>.Ok(new ConfirmResultDto { IsConfirmed = true });
        }
```

- [ ] **Step 5: Dezelfde twee wijzigingen in `PickAlternativeAsync`**

In `PickAlternativeAsync` (regels ~156–280): voeg dezelfde `ValidatePaymentMethodAllowed(method, series)`-check toe na het laden van `series`, en zet het cash-blok (regels ~240–258) om naar `PaymentStatus.Pending` + `EnrollmentStatus.PendingPayment` zonder finalisatie — identiek aan Step 4 (gebruik de daar geldende variabelenamen `oldAssignment` i.p.v. `assignment`).

- [ ] **Step 6: Voer de tests uit — slagen**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~StudentConfirmationServiceTests"`
Expected: PASS. Draai ook de volledige suite (bestaande cash-tests die `Paid`/`Confirmed` verwachtten moeten aangepast zijn):
Run: `cd backend && dotnet test CoachOS.slnx`
Expected: alle tests groen (pas verouderde asserts aan waar nodig).

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Application/StudentConfirmation backend/CoachOS.Tests
git commit -m "feat: gate betaalmethode server-side en zet cash op wacht-op-bevestiging"
```

---

## Task 8: Repository + service voor "markeer cash betaald"

**Files:**
- Modify: `backend/CoachOS.Domain/Interfaces/IPaymentRepository.cs`
- Modify: `backend/CoachOS.Infrastructure/Repositories/PaymentRepository.cs`
- Modify: `backend/CoachOS.Application/StudentConfirmation/IStudentConfirmationService.cs` + `StudentConfirmationService.cs`
- Test: `backend/CoachOS.Tests/Services/StudentConfirmationServiceTests.cs`

**Rationale:** `MarkEnrollmentCashPaidAsync` leeft op `IStudentConfirmationService` (niet `IPaymentService`), omdat de reeks-finalisatie (`TryFinalizeSeriesAsync`) en de bevestigings-notificatie daar al privé beschikbaar zijn — analoog aan hoe `MarkCampCashPaidAsync` in de camp-laag naast zijn eigen finalize/notify leeft.

**Interfaces:**
- Produces:
  - `IPaymentRepository.GetLatestPendingCashByEnrollmentIdAsync(Guid enrollmentId, Guid organizationId, CancellationToken ct)` → `Payment?`
  - `IStudentConfirmationService.MarkEnrollmentCashPaidAsync(Guid enrollmentId, Guid organizationId, CancellationToken ct)` → `Result`

- [ ] **Step 1: Voeg de repo-methode toe (interface + impl)**

In `IPaymentRepository.cs`, naast `GetLatestPendingCashByCampEnrollmentIdAsync`:

```csharp
    Task<Payment?> GetLatestPendingCashByEnrollmentIdAsync(
        Guid enrollmentId, Guid organizationId, CancellationToken ct = default);
```

In `PaymentRepository.cs`, naar analogie van `GetLatestPendingCashByCampEnrollmentIdAsync` (rond regel ~67):

```csharp
    public async Task<Payment?> GetLatestPendingCashByEnrollmentIdAsync(
        Guid enrollmentId, Guid organizationId, CancellationToken ct = default)
        => await db.Payments
            .Where(p => p.EnrollmentId == enrollmentId
                && p.OrganizationId == organizationId
                && p.Method == PaymentMethod.Cash
                && p.Status == PaymentStatus.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);
```

(Deze is getrackt — geen `.AsNoTracking()` — want de payment wordt gemuteerd. Verifieer dat de camp-variant dat ook zo doet en volg die.)

- [ ] **Step 2: Schrijf de falende service-test**

In `StudentConfirmationServiceTests.cs`:

```csharp
[Fact]
public async Task MarkEnrollmentCashPaid_sets_payment_paid_and_enrollment_confirmed()
{
    Guid orgId = Guid.NewGuid();
    Guid enrollmentId = Guid.NewGuid();
    Payment pending = new()
    {
        OrganizationId = orgId, EnrollmentId = enrollmentId,
        Method = PaymentMethod.Cash, Status = PaymentStatus.Pending, Amount = 120m,
    };
    _paymentRepo.GetLatestPendingCashByEnrollmentIdAsync(enrollmentId, orgId, Arg.Any<CancellationToken>())
        .Returns(pending);
    // arrange enrollment-lookup zoals de bestaande tests dat doen

    Result result = await _service.MarkEnrollmentCashPaidAsync(enrollmentId, orgId, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    pending.Status.Should().Be(PaymentStatus.Paid);
}
```

- [ ] **Step 3: Voer uit — faalt (methode bestaat niet)**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~MarkEnrollmentCashPaid_sets_payment_paid_and_enrollment_confirmed"`
Expected: FAIL (compile).

- [ ] **Step 4: Declareer de service-methode**

In `IStudentConfirmationService.cs`:

```csharp
    Task<Result> MarkEnrollmentCashPaidAsync(
        Guid enrollmentId, Guid organizationId, CancellationToken ct = default);
```

- [ ] **Step 5: Implementeer de methode**

In `StudentConfirmationService.cs` (de service heeft al `paymentRepo`, `enrollmentRepo`/repo's voor enrollments, en de private `TryFinalizeSeriesAsync` + `ConfirmEnrollmentStatuses`). Implementeer:

```csharp
    public async Task<Result> MarkEnrollmentCashPaidAsync(
        Guid enrollmentId, Guid organizationId, CancellationToken ct = default)
    {
        Payment? payment = await paymentRepo.GetLatestPendingCashByEnrollmentIdAsync(
            enrollmentId, organizationId, ct);
        if (payment is null)
            return Result.Fail(new Error(
                ErrorCodes.NotFound, "Geen openstaande overschrijving gevonden voor deze inschrijving."));

        // Laad de enrollment(s) van de reeks voor deze inschrijving (leader + groepsleden)
        // zodat we ze allemaal op Confirmed kunnen zetten. Gebruik de bestaande
        // enrollment-repository-methode die een enrollment + zijn groep ophaalt binnen de org;
        // grep IEnrollmentRepository voor de juiste read (bv. GetByIdWithGroupAsync-equivalent).
        // Bepaal de LessonSerieId uit de geladen enrollment voor de finalisatie hieronder.

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;

        // Zet de bijhorende enrollment(s) op Confirmed (hergebruik ConfirmEnrollmentStatuses
        // via de ScheduleAssignment van deze enrollment, net als de confirm-flow doet).
        // await paymentRepo.SaveChangesAsync(ct); // één save flusht payment + enrollment (gedeelde DbContext)

        await TryFinalizeSeriesAsync(/* lessonSerieId */ default, organizationId, ct);

        // Stuur dezelfde "plek definitief"-bevestigingsmail als het online-betaalde pad
        // (hergebruik de bestaande notify-helper; grep in deze klasse naar de email-verzending
        // die na een geslaagde betaling loopt).

        return Result.Ok();
    }
```

> **Implementatienoot voor de uitvoerder:** vul de gemarkeerde plekken in met de exacte
> repository-reads en de bestaande notify/finalize-helpers in deze klasse. Verifieer welke
> read de enrollment + zijn `ScheduleAssignment` + groep binnen de org levert (grep
> `IEnrollmentRepository` en de bestaande confirm-flow). De atomiciteitsaanpak (één
> `SaveChangesAsync` voor payment + enrollment via de gedeelde scoped `ApplicationDbContext`)
> is identiek aan `MarkCampCashPaidAsync` in `PaymentService` — gebruik die als referentie.

- [ ] **Step 6: Voer de test uit — slaagt**

Run: `cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~StudentConfirmationServiceTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Domain/Interfaces/IPaymentRepository.cs backend/CoachOS.Infrastructure/Repositories/PaymentRepository.cs backend/CoachOS.Application/StudentConfirmation backend/CoachOS.Tests
git commit -m "feat: admin kan cash-betaling van reeksinschrijving als betaald markeren"
```

---

## Task 9: Endpoint + admin-UI voor "markeer betaald"

**Files:**
- Create: `backend/CoachOS.API/Endpoints/Enrollments/MarkEnrollmentCashPaidEndpoint.cs`
- Modify: `frontend/lib/api/enrollments.ts`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/[id]/page.tsx`

**Interfaces:**
- Consumes: `IStudentConfirmationService.MarkEnrollmentCashPaidAsync(...)`.
- Produces: `POST /enrollments/{enrollmentId:guid}/mark-cash-paid`; FE `markEnrollmentCashPaid(seriesId, enrollmentId)`.

- [ ] **Step 1: Maak het endpoint (kopieer het camp-patroon)**

Create `backend/CoachOS.API/Endpoints/Enrollments/MarkEnrollmentCashPaidEndpoint.cs`:

```csharp
using CoachOS.API.Extensions;
using CoachOS.Application.StudentConfirmation;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Enrollments;

/// <summary>
/// Admin/trainer markeert de overschrijving van een reeksinschrijving als betaald,
/// wat de inschrijving bevestigt en de bevestigingsmail verstuurt.
/// </summary>
public class MarkEnrollmentCashPaidEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/enrollments/{enrollmentId:guid}/mark-cash-paid",
            async (Guid enrollmentId, IStudentConfirmationService service, HttpContext ctx, CancellationToken ct) =>
            {
                Result result = await service.MarkEnrollmentCashPaidAsync(
                    enrollmentId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Enrollments");
    }
}
```

(Verifieer de exacte `IEndpoint`-namespace/`using` t.o.v. `MarkCampEnrollmentCashPaidEndpoint.cs`; endpoints worden automatisch geregistreerd via de bestaande assembly-scan.)

- [ ] **Step 2: Verifieer de backend build**

Run: `cd backend && dotnet build CoachOS.slnx`
Expected: succeeded.

- [ ] **Step 3: Voeg de FE-api toe**

In `frontend/lib/api/enrollments.ts`:

```typescript
export async function markEnrollmentCashPaid(enrollmentId: string): Promise<void> {
  await apiClient.post(`/enrollments/${enrollmentId}/mark-cash-paid`);
}
```

- [ ] **Step 4: Voeg de actie toe aan de inschrijvingenlijst**

In `app/(dashboard)/dashboard/lessons/[id]/page.tsx`, in `EnrollmentRow` (rond regel ~1493): toon een "Markeer als betaald"-knop wanneer `enrollment.status === "PendingPayment"`. De knop roept een mutatie aan die `markEnrollmentCashPaid(enrollment.id)` uitvoert en daarna `queryClient.invalidateQueries({ queryKey: ["lessonSeriesEnrollments", seriesId] })` (gebruik de bestaande query-key uit dit bestand). Volg de bestaande knop/mutatie-stijl van `handleCancelEnrollment` in dezelfde component.

> Als `LessonSeriesEnrollmentDto` geen `status === "PendingPayment"` blootgeeft die
> onderscheid maakt tussen cash-pending en online-pending: toon de knop voor alle
> `PendingPayment`-rijen. De backend faalt netjes (`NotFound`) als er geen openstaande
> cash-betaling is — dat is een acceptabele vangnet-UX.

- [ ] **Step 5: Voeg de knop-vertaling toe**

In `frontend/messages/nl.json` (namespace van de reeksdetailpagina): `"markCashPaid": "Markeer als betaald"`.

- [ ] **Step 6: Verifieer FE-build**

Run: `cd frontend && bun run build`
Expected: slaagt.

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.API/Endpoints/Enrollments/MarkEnrollmentCashPaidEndpoint.cs "frontend/lib/api/enrollments.ts" "frontend/app/(dashboard)/dashboard/lessons/[id]/page.tsx" frontend/messages/nl.json
git commit -m "feat: endpoint en admin-actie om reeks-overschrijving als betaald te markeren"
```

---

## Task 10: E-mail — betaalinstructies bij overschrijving

**Files:**
- Modify: de MJML-bevestigingstemplate voor reeksinschrijvingen (grep `backend` naar `.mjml` templates + `MjmlTemplateRenderer`-gebruik in de confirm-/notify-flow)
- Modify: de plek waar de confirmation-mail wordt samengesteld (in `StudentConfirmationService` of de notify-helper die het online-pad gebruikt)

**Interfaces:**
- Consumes: bestaande `IEmailService` / `MjmlTemplateRenderer`.

- [ ] **Step 1: Lokaliseer de bevestigingsmail-flow**

Run: `cd backend && grep -rn "MjmlTemplateRenderer\|SendConfirmation\|bevestig" CoachOS.Application/StudentConfirmation CoachOS.Infrastructure | head`
Bepaal welke template de "plek bevestigd"-mail rendert en welke tokens ze aanvaardt.

- [ ] **Step 2: Voeg een cash-instructieblok toe aan de template**

Breid de template uit met een conditioneel blok (via een boolean/tekst-token) dat bij een openstaande overschrijving toont: *"Betaal €{bedrag} via overschrijving. Je plek is definitief zodra de club je betaling bevestigt."* Houd `MjmlTemplateRenderer` ongewijzigd van vorm (enkel nieuwe tokens).

- [ ] **Step 3: Vul de tokens in het cash-pad**

In `ConfirmAsync`/`PickAlternativeAsync` cash-pad (Task 7): verstuur de bevestigingsmail met het cash-instructie-token gezet (bedrag = `cashBreakdown.Total`). In `MarkEnrollmentCashPaidAsync` (Task 8): verstuur de "plek definitief"-mail zonder het instructie-token (zoals het online-betaalde pad).

- [ ] **Step 4: Verifieer build + relevante tests**

Run: `cd backend && dotnet build CoachOS.slnx && dotnet test CoachOS.slnx --filter "FullyQualifiedName~StudentConfirmationServiceTests"`
Expected: build + tests groen.

- [ ] **Step 5: Commit**

```bash
git add backend
git commit -m "feat(email): betaalinstructies bij overschrijving in bevestigingsmail"
```

---

## Task 11: Seed-scripts + definitieve reset/seed E2E

**Files:**
- Modify: `backend/Scripts/seed-data.json`
- Modify: `backend/Scripts/seed-demo-data.py`

**Interfaces:**
- Consumes: het gewijzigde `POST /tennisclubs`-onafhankelijke contract van `POST /lessonseries` (create-DTO met vier nieuwe velden).

- [ ] **Step 1: Voeg de vier velden toe aan de serie-payloads**

In `backend/Scripts/seed-data.json`, bij de lessenreeksen: voeg per reeks `allowSoloEnrollment`, `allowGroupEnrollment`, `acceptOnlinePayment`, `acceptManualPayment` toe. Maak minstens:
- één reeks **solo-only** (`allowGroupEnrollment: false`),
- één reeks **groep-only** (`allowSoloEnrollment: false`),
- één reeks met **handmatige betaling** (`acceptOnlinePayment: false, acceptManualPayment: true`) — deze org heeft in de seed geen Mollie-koppeling, dus online moet uit staan.

Zorg dat elke reeks minstens één inschrijfwijze én één betaalmethode aan heeft (validators uit Task 2).

- [ ] **Step 2: Stuur de velden mee in de python-seeder**

In `backend/Scripts/seed-demo-data.py`, `create_lesson_series(...)` (rond regel ~234): neem de vier velden uit de spec mee in de POST-body (met defaults `True/True/True/False` als een reeks ze niet expliciet zet).

- [ ] **Step 3: Voer de definitieve reset + seed uit**

```bash
cd backend
bash Scripts/reset-db.sh --no-frontend
# wacht tot http://localhost:5142/health → 200
bash Scripts/seed-demo-data.sh
```

> **Let op (lokale omgeving):** postgres draait op host-poort **5439** via
> `docker-compose.override.yml`, en `reset-db.sh` gebruikt expliciet `-f docker-compose.yml`
> (laadt de override niet). Als de reset op de 5432-poortconflict botst, draai de reset
> handmatig zoals eerder: `docker-compose down -v && docker-compose up -d --build` (dat pikt
> de override wél op), wacht op `/health` 200, dan `bash Scripts/seed-demo-data.sh`.

Expected: seed loopt volledig groen — clubs, series (incl. de solo-only/groep-only/handmatige varianten), enrollments, planning bevestigd.

- [ ] **Step 4: Rook-test de nieuwe gating end-to-end**

Verifieer met `curl` (login als seed-admin, token ophalen) dat:
- `POST /lessonseries` met `acceptOnlinePayment: true` op een org zónder Mollie een `400`/validatiefout geeft;
- `POST /enroll/.../` (submit) met `enrollmentType: "group"` op de solo-only reeks een `400` geeft.

Expected: beide geweigerd met een duidelijke NL-melding.

- [ ] **Step 5: Commit**

```bash
git add backend/Scripts/seed-data.json backend/Scripts/seed-demo-data.py
git commit -m "chore(seed): serie-vlaggen in demo-data + solo/groep/handmatige varianten"
```

---

## Self-Review

**Spec-coverage:** §1 datamodel → Task 1. §2 DTO/mapper → Task 2. §2 Mollie-gating → Task 3. §3 create/edit-form → Task 4. §4 inschrijfwijze afdwingen → Task 5. §5a vlaggen naar confirmation → Task 6. §5b/5c gating + cash camp-stijl → Task 7. §5d repo/service mark-paid → Task 8. §5d endpoint + admin-UI → Task 9. §6 e-mail → Task 10. §7 tests → per task + Task 11 reset/seed. Alle secties gedekt.

**Type-consistentie:** entity-booleans `AllowSoloEnrollment`/`AllowGroupEnrollment`/`AcceptOnlinePayment`/`AcceptManualPayment` identiek in entity, DTO's, mapper, validators, Dto, AssignmentDetailsDto, FE-types. Service-methode `MarkEnrollmentCashPaidAsync(Guid, Guid, CancellationToken)` en repo `GetLatestPendingCashByEnrollmentIdAsync(Guid, Guid, CancellationToken)` consistent tussen Task 8 (definitie) en Task 9 (gebruik). FE `markEnrollmentCashPaid` consistent tussen Task 9 def en gebruik.

**Open implementatiedetails (bewust, met vindwijzer):** in Task 8 zijn de exacte enrollment-repository-reads en notify/finalize-helpers gemarkeerd met een grep-aanwijzing i.p.v. verzonnen signatures, omdat de precieze bestaande helpers in `StudentConfirmationService` geverifieerd moeten worden tijdens uitvoering. Dit is geen placeholder-werk maar een expliciete "gebruik de bestaande helper X"-instructie met referentie (`MarkCampCashPaidAsync`).
