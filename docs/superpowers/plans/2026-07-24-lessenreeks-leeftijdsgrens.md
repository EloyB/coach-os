# Leeftijdsgrens op een lessenreeks — Implementatieplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Een lessenreeks krijgt een minimum- en maximumleeftijd (default 3–99), die publiek getoond wordt en die inschrijvingen buiten de grens weigert.

**Architecture:** Twee gehele velden `MinAge`/`MaxAge` op `LessonSerie`, gevalideerd bij aanmaken/bewerken. Bij inschrijven wordt de leeftijd op de **startdatum van de reeks** berekend met de bestaande `ParticipantCategoryResolver.CalculateAge` en per deelnemer getoetst. De frontend voegt twee velden toe aan wizard-stap 1 en het bewerken-formulier, toont de range op de publieke pagina en spiegelt de check client-side.

**Tech Stack:** .NET 10, EF Core (PostgreSQL), FluentValidation, NUnit + Moq + FluentAssertions, Next.js 15 + Zod + react-hook-form, next-intl.

**Spec:** `docs/superpowers/specs/2026-07-24-lessenreeks-leeftijdsgrens-design.md`

## Global Constraints

- Services geven `Result<T>` terug; nooit exceptions voor businessfouten.
- Elke service filtert op `OrganizationId`; endpoints halen die uit `ctx.GetOrganizationId()`.
- EF-configuratie hoort in een `IEntityTypeConfiguration<T>`, niet in `OnModelCreating`.
- Range is **inclusief**: toegelaten als `MinAge ≤ leeftijd ≤ MaxAge`. Defaults `MinAge = 3`, `MaxAge = 99`. Grenzen 0–120.
- Leeftijd wordt getoetst op `series.StartDate` via `ParticipantCategoryResolver.CalculateAge(dob, startDate)`.
- Geen geboortedatum → geen leeftijdsblokkade (consistent met de bestaande categorie-/index-logica).
- Wizard-stap 1 gebruikt `useTranslations("lessonWizard")` + `messages/nl.json`. Het bewerken-formulier op de detailpagina gebruikt hardcoded Nederlandse strings — volg dat patroon.
- Backend-tests: `cd backend && dotnet test CoachOS.slnx`. Eén test: `dotnet test --filter "FullyQualifiedName~<naam>"`.
- Frontend: `cd frontend && bun run build`.
- Commit per taak, conventional commits. Nooit `git push`.

---

## File Structure

**Backend — gewijzigd:**
- `CoachOS.Domain/Entities/LessonSerie.cs` — `MinAge`/`MaxAge`
- `CoachOS.Infrastructure/Persistence/Configurations/LessonSerieConfiguration.cs` — defaults
- `CoachOS.Application/LessonSerie/DTOs/CreateLessonSerieRequest.cs`, `UpdateLessonSerieRequest.cs`, `LessonSerieDto.cs`
- `CoachOS.Application/Enrollments/DTOs/PublicLessonSerieDto.cs`
- `CoachOS.Application/LessonSerie/Validators/CreateLessonSerieRequestValidator.cs`, `UpdateLessonSerieRequestValidator.cs`
- `CoachOS.Application/Mappings/ApplicationMapper.cs` — create-map + `ToLessonSerieDto`
- `CoachOS.Application/LessonSerie/LessonSerieService.cs` — update-service
- `CoachOS.Application/Enrollments/EnrollmentService.cs` — leeftijdscheck + `PublicLessonSerieDto`-build

**Backend — nieuw:**
- `CoachOS.Infrastructure/Migrations/<timestamp>_AddAgeRangeToLessonSerie.cs` (via `dotnet ef`)

**Frontend — gewijzigd:**
- `frontend/app/(dashboard)/dashboard/lessons/new/_types.ts` — `Step1Data`
- `frontend/app/(dashboard)/dashboard/lessons/new/_components/step-1-basisinfo.tsx` — schema + velden
- `frontend/app/(dashboard)/dashboard/lessons/new/_components/step-3-validation.tsx` — payload
- `frontend/lib/api/lessonSeries.ts` — `CreateLessonSeriesWizardRequest`, `UpdateLessonSeriesRequest`
- `frontend/lib/api/enrollments.ts` — `PublicLessonSeriesDto`
- `frontend/app/(public)/enroll/[seriesId]/page.tsx` — weergave + client-side check
- `frontend/app/(dashboard)/dashboard/lessons/[id]/page.tsx` — `EditSeriesForm`
- `frontend/messages/nl.json` — labels

**Scripts:**
- `backend/Scripts/seed-data.json`

---

## Task 1: Datamodel & migratie

**Files:**
- Modify: `backend/CoachOS.Domain/Entities/LessonSerie.cs:28`
- Modify: `backend/CoachOS.Infrastructure/Persistence/Configurations/LessonSerieConfiguration.cs:23`
- Create: `backend/CoachOS.Infrastructure/Migrations/<timestamp>_AddAgeRangeToLessonSerie.cs`

**Interfaces:**
- Produces: `LessonSerie.MinAge` (`int`, default 3), `LessonSerie.MaxAge` (`int`, default 99).

- [ ] **Step 1: Entity-velden toevoegen**

In `LessonSerie.cs`, direct na `public int? MaxRegistrations { get; set; }`:

```csharp
    /// <summary>Minimumleeftijd (inclusief) op de startdatum van de reeks.</summary>
    public int MinAge { get; set; } = 3;

    /// <summary>Maximumleeftijd (inclusief) op de startdatum van de reeks.</summary>
    public int MaxAge { get; set; } = 99;
```

- [ ] **Step 2: EF-defaults configureren**

In `LessonSerieConfiguration.cs`, na het `Price`-blok:

```csharp
        builder.Property(ls => ls.MinAge)
            .HasDefaultValue(3);

        builder.Property(ls => ls.MaxAge)
            .HasDefaultValue(99);
```

De `HasDefaultValue` zorgt dat de migratie bestaande rijen backfilt met 3/99.

- [ ] **Step 3: Migratie genereren**

```bash
cd backend
dotnet ef migrations add AddAgeRangeToLessonSerie --project CoachOS.Infrastructure --startup-project CoachOS.API
```

Verwacht: nieuw migratiebestand met `AddColumn<int>(... "MinAge", ... defaultValue: 3)` en idem voor `MaxAge` met `defaultValue: 99`. Controleer dat beide `defaultValue` bevatten (backfill van bestaande rijen); handmatig aanpassen is niet nodig.

- [ ] **Step 4: Build**

```bash
cd backend && dotnet build CoachOS.slnx
```

Verwacht: build slaagt.

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Domain/Entities/LessonSerie.cs \
        backend/CoachOS.Infrastructure/Persistence/Configurations/LessonSerieConfiguration.cs \
        backend/CoachOS.Infrastructure/Migrations/
git commit -m "feat(lessonserie): MinAge/MaxAge velden + migratie (default 3-99)"
```

---

## Task 2: Contract & validatie

**Files:**
- Modify: `backend/CoachOS.Application/LessonSerie/DTOs/CreateLessonSerieRequest.cs`
- Modify: `backend/CoachOS.Application/LessonSerie/DTOs/UpdateLessonSerieRequest.cs`
- Modify: `backend/CoachOS.Application/LessonSerie/DTOs/LessonSerieDto.cs`
- Modify: `backend/CoachOS.Application/Enrollments/DTOs/PublicLessonSerieDto.cs`
- Modify: `backend/CoachOS.Application/LessonSerie/Validators/CreateLessonSerieRequestValidator.cs`
- Modify: `backend/CoachOS.Application/LessonSerie/Validators/UpdateLessonSerieRequestValidator.cs`
- Modify: `backend/CoachOS.Application/Mappings/ApplicationMapper.cs`
- Modify: `backend/CoachOS.Application/LessonSerie/LessonSerieService.cs:178`
- Modify: `backend/CoachOS.Application/Enrollments/EnrollmentService.cs:48-69`
- Test: `backend/CoachOS.Tests/Validators/CreateLessonSerieRequestValidatorTests.cs`

**Interfaces:**
- Consumes: `LessonSerie.MinAge`/`MaxAge` (taak 1).
- Produces: `CreateLessonSerieRequest.MinAge`/`.MaxAge` (`int`), `UpdateLessonSerieRequest.MinAge`/`.MaxAge` (`int`), `LessonSerieDto.MinAge`/`.MaxAge`, `PublicLessonSerieDto.MinAge`/`.MaxAge`.

- [ ] **Step 1: Falende validatortests schrijven**

Zoek `backend/CoachOS.Tests/Validators/CreateLessonSerieRequestValidatorTests.cs`. Bestaat het niet, maak het aan met dezelfde `using`- en fixture-stijl als `SubmitEnrollmentRequestValidatorTests.cs` (NUnit, FluentAssertions). Voeg toe (en zorg dat `ValidRequest()` alle bestaande verplichte velden invult — kopieer de opzet uit een bestaande geldige-request-helper in dat bestand of bouw er één):

```csharp
    [Test]
    public void MinAgeGreaterThanMaxAge_Fails()
    {
        CreateLessonSerieRequest request = ValidRequest() with { MinAge = 50, MaxAge = 10 };

        _validator.Validate(request).Errors
            .Should().Contain(e => e.ErrorMessage == "Minimumleeftijd mag niet groter zijn dan de maximumleeftijd.");
    }

    [Test]
    public void AgeBounds_0_And_120_Pass()
    {
        CreateLessonSerieRequest request = ValidRequest() with { MinAge = 0, MaxAge = 120 };

        _validator.Validate(request).Errors
            .Should().NotContain(e => e.PropertyName is "MinAge" or "MaxAge");
    }

    [Test]
    public void MaxAgeAbove120_Fails()
    {
        CreateLessonSerieRequest request = ValidRequest() with { MaxAge = 121 };

        _validator.Validate(request).IsValid.Should().BeFalse();
    }
```

- [ ] **Step 2: Tests draaien, verwacht falen**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~CreateLessonSerieRequestValidatorTests"
```

Verwacht: compileerfout (`MinAge`/`MaxAge` bestaan nog niet op het request) of falende asserties.

- [ ] **Step 3: DTO's aanpassen**

In `CreateLessonSerieRequest.cs`, na `MaxRegistrations`:

```csharp
    public int MinAge { get; init; } = 3;
    public int MaxAge { get; init; } = 99;
```

In `UpdateLessonSerieRequest.cs`, na `MaxRegistrations`:

```csharp
    public int MinAge { get; init; } = 3;
    public int MaxAge { get; init; } = 99;
```

In `LessonSerieDto.cs`, na `MaxRegistrations`:

```csharp
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
```

In `PublicLessonSerieDto.cs`, na `MaxRegistrations`:

```csharp
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
```

- [ ] **Step 4: Validators aanpassen**

Voeg in `CreateLessonSerieRequestValidator.cs`, na de `MaxRegistrations`-regel, toe:

```csharp
        RuleFor(x => x.MinAge)
            .InclusiveBetween(0, 120).WithMessage("Minimumleeftijd moet tussen 0 en 120 liggen.");

        RuleFor(x => x.MaxAge)
            .InclusiveBetween(0, 120).WithMessage("Maximumleeftijd moet tussen 0 en 120 liggen.");

        RuleFor(x => x)
            .Must(x => x.MinAge <= x.MaxAge)
            .WithMessage("Minimumleeftijd mag niet groter zijn dan de maximumleeftijd.")
            .WithName("MinAge");
```

Voeg dezelfde drie regels toe aan `UpdateLessonSerieRequestValidator.cs` (open het bestand; volg de bestaande stijl — het valideert `x.MaxRegistrations` op dezelfde manier).

- [ ] **Step 5: Mapper aanpassen**

In `ApplicationMapper.cs`, in de create-map (het object met `MaxRegistrations = request.MaxRegistrations,`), voeg toe:

```csharp
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
```

In `ToLessonSerieDto`, na `MaxRegistrations = ls.MaxRegistrations,`:

```csharp
            MinAge = ls.MinAge,
            MaxAge = ls.MaxAge,
```

- [ ] **Step 6: Update-service aanpassen**

In `LessonSerieService.cs`, na `series.MaxRegistrations = request.MaxRegistrations;` (regel ~178):

```csharp
        series.MinAge = request.MinAge;
        series.MaxAge = request.MaxAge;
```

- [ ] **Step 7: Publieke DTO-build aanpassen**

In `EnrollmentService.cs`, in `GetPublicLessonSerieAsync` (het `new PublicLessonSerieDto { ... }` rond regel 48), na `MaxRegistrations = series.MaxRegistrations,`:

```csharp
            MinAge = series.MinAge,
            MaxAge = series.MaxAge,
```

- [ ] **Step 8: Tests draaien**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~CreateLessonSerieRequestValidatorTests"
```

Verwacht: alle tests slagen.

- [ ] **Step 9: Commit**

```bash
git add backend/CoachOS.Application/ backend/CoachOS.Tests/Validators/
git commit -m "feat(lessonserie): MinAge/MaxAge in contract, validatie en mapping"
```

---

## Task 3: Afdwingen bij inschrijven

**Files:**
- Modify: `backend/CoachOS.Application/Enrollments/EnrollmentService.cs` (`SubmitEnrollmentAsync`)
- Test: `backend/CoachOS.Tests/Services/EnrollmentServiceTests.cs`

**Interfaces:**
- Consumes: `series.MinAge`/`.MaxAge`/`.StartDate`, `ParticipantCategoryResolver.CalculateAge(DateOnly, DateOnly)`, private `ParseBirthDate(string?) → DateOnly?`.
- Produces: leeftijdsgrens-afdwinging vóór de transactie.

- [ ] **Step 1: Falende tests schrijven**

Voeg toe aan `EnrollmentServiceTests.cs`. Gebruik de bestaande `BuildActiveSeries`-helper en `SetupSuccessfulEnrollment` uit dat bestand. `BuildActiveSeries` zet `StartDate = DateOnly.FromDateTime(DateTime.Today)`; stel voor de leeftijdsberekening de reeks-grenzen en een geboortedatum expliciet in:

```csharp
    [Test]
    public async Task SubmitEnrollment_ParticipantYoungerThanMinAge_ReturnsConflict()
    {
        LessonSerie series = BuildActiveSeries();
        series.MinAge = 6;
        series.MaxAge = 99;
        series.StartDate = new DateOnly(2026, 1, 1);
        SetupSuccessfulEnrollment(series, "kind@test.be");

        // 3 jaar oud op de startdatum → onder de min van 6.
        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Jong Kind",
            StudentEmail = "kind@test.be",
            DateOfBirth = "2023-01-01",
        };

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
        _enrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SubmitEnrollment_ParticipantExactlyMinAge_Succeeds()
    {
        LessonSerie series = BuildActiveSeries();
        series.MinAge = 3;
        series.MaxAge = 99;
        series.StartDate = new DateOnly(2026, 1, 1);
        SetupSuccessfulEnrollment(series, "kind@test.be");

        // Precies 3 op de startdatum.
        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Net Drie",
            StudentEmail = "kind@test.be",
            DateOfBirth = "2023-01-01",
        };

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task SubmitEnrollment_GroupMemberOutsideRange_RejectsWholeEnrollment()
    {
        LessonSerie series = BuildActiveSeries();
        series.MinAge = 6;
        series.MaxAge = 12;
        series.StartDate = new DateOnly(2026, 1, 1);
        SetupSuccessfulEnrollment(series, "leader@test.be");

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Leader",
            StudentEmail = "leader@test.be",
            DateOfBirth = "2016-01-01", // 10 jaar → ok
            EnrollmentType = "group",
            GroupMembers = new()
            {
                new() { StudentName = "Te Jong", StudentEmail = null, DateOfBirth = "2023-01-01" }, // 3 → buiten
            },
        };

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
        _enrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }
```

- [ ] **Step 2: Tests draaien, verwacht falen**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~EnrollmentServiceTests.SubmitEnrollment_Participant|FullyQualifiedName~EnrollmentServiceTests.SubmitEnrollment_GroupMemberOutsideRange"
```

Verwacht: FAIL — er is nog geen leeftijdscheck, de eerste twee tests geven succes waar conflict verwacht wordt.

- [ ] **Step 3: Leeftijdscheck implementeren**

In `EnrollmentService.cs`, zoek in `SubmitEnrollmentAsync` de plek net ná het laden en valideren van de reeks en vlak vóór `await enrollmentRepo.BeginTransactionAsync(...)`. Voeg daar toe:

```csharp
            // Leeftijdsgrens: toets elke deelnemer op de startdatum van de reeks. Fail-fast
            // vóór de transactie — geen DB-werk als iemand buiten de grens valt.
            Error? ageError = CheckAgeEligibility(request, series);
            if (ageError is not null)
                return Result<Guid>.Fail(ageError);
```

Voeg onderaan de klasse (bij de andere private helpers, naast `ParseBirthDate`) toe:

```csharp
    /// <summary>
    /// Controleert of elke deelnemer (leider + groepsleden) op de startdatum van de reeks
    /// binnen [MinAge, MaxAge] valt. Zonder bruikbare geboortedatum wordt niet geblokkeerd,
    /// consistent met de tariefcategorie en de partiële unique index.
    /// </summary>
    private static Error? CheckAgeEligibility(
        SubmitEnrollmentRequest request, Domain.Entities.LessonSerie series)
    {
        List<(string Name, string? Dob)> people = [(request.StudentName, request.DateOfBirth)];
        if (request.EnrollmentType == "group" && request.GroupMembers is not null)
            people.AddRange(request.GroupMembers.Select(m => (m.StudentName, (string?)m.DateOfBirth)));

        foreach ((string name, string? dob) in people)
        {
            if (!DateOfBirthRules.TryParse(dob, out DateOnly parsed)) continue;

            int age = ParticipantCategoryResolver.CalculateAge(parsed, series.StartDate);
            if (age < series.MinAge || age > series.MaxAge)
            {
                return new Error(ErrorCodes.Validation,
                    $"{name} ({age} jaar) valt buiten de leeftijdsgrens van deze reeks " +
                    $"({series.MinAge}–{series.MaxAge} jaar).");
            }
        }

        return null;
    }
```

Controleer bovenaan het bestand dat `using CoachOS.Application.Common;` (voor `DateOfBirthRules`) en `using CoachOS.Domain.Common;` (voor `ParticipantCategoryResolver`) aanwezig zijn; voeg toe indien nodig.

- [ ] **Step 4: Tests draaien**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~EnrollmentServiceTests"
```

Verwacht: alle EnrollmentServiceTests slagen (de drie nieuwe + de bestaande).

- [ ] **Step 5: Volledige backend-suite**

```bash
cd backend && dotnet test CoachOS.slnx
```

Verwacht: alles groen.

- [ ] **Step 6: Commit**

```bash
git add backend/CoachOS.Application/Enrollments/EnrollmentService.cs backend/CoachOS.Tests/Services/EnrollmentServiceTests.cs
git commit -m "feat(enrollments): weiger inschrijving buiten de leeftijdsgrens van de reeks"
```

---

## Task 4: Wizard stap 1 (aanmaken)

**Files:**
- Modify: `frontend/app/(dashboard)/dashboard/lessons/new/_types.ts:27`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/new/_components/step-1-basisinfo.tsx`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/new/_components/step-3-validation.tsx:153`
- Modify: `frontend/lib/api/lessonSeries.ts` (`CreateLessonSeriesWizardRequest`)
- Modify: `frontend/messages/nl.json` (`lessonWizard`)

**Interfaces:**
- Consumes: backend-contract uit taak 2 (`minAge`/`maxAge` op het wizard-request).
- Produces: `Step1Data.minAge`/`.maxAge` (`number`).

- [ ] **Step 1: Vertaalsleutels toevoegen**

In `frontend/messages/nl.json`, binnen `"lessonWizard"` (na `"maxRegistrations"`):

```json
    "minAge": "Min. leeftijd",
    "maxAge": "Max. leeftijd",
```

- [ ] **Step 2: Types uitbreiden**

In `_types.ts`, in `Step1Data`, na `maxRegistrations: number;`:

```ts
  minAge: number;
  maxAge: number;
```

In `lib/api/lessonSeries.ts`, in `CreateLessonSeriesWizardRequest`, na `maxRegistrations: number;`:

```ts
  minAge: number;
  maxAge: number;
```

- [ ] **Step 3: Zod-schema + defaults**

In `step-1-basisinfo.tsx`, voeg in het `z.object({...})` (na `maxRegistrations`) toe:

```ts
    minAge: z
      .number({ message: "Minimumleeftijd is verplicht" })
      .int("Gebruik een heel getal")
      .min(0, "Minimaal 0")
      .max(120, "Maximaal 120"),
    maxAge: z
      .number({ message: "Maximumleeftijd is verplicht" })
      .int("Gebruik een heel getal")
      .min(0, "Minimaal 0")
      .max(120, "Maximaal 120"),
```

Voeg ná de bestaande `.refine(...)` voor de datums een tweede `.refine` toe:

```ts
  .refine((d) => d.minAge <= d.maxAge, {
    message: "Minimumleeftijd mag niet groter zijn dan de maximumleeftijd",
    path: ["maxAge"],
  })
```

Pas de `defaultValues` aan:

```ts
    defaultValues: defaultValues ?? { price: 0, maxRegistrations: 0, minAge: 3, maxAge: 99 },
```

- [ ] **Step 4: Velden renderen**

In `step-1-basisinfo.tsx`, direct ná het "Max leerlingen"-blok (het `<div>` met `register("maxRegistrations")`), voeg toe:

```tsx
        {/* Leeftijdsgrens */}
        <div className="grid grid-cols-2 gap-3">
          <div>
            <Label required>{t("minAge")}</Label>
            <input
              {...register("minAge", { valueAsNumber: true })}
              type="number"
              min={0}
              max={120}
              className={inputClass}
            />
            <FieldError message={errors.minAge?.message} />
          </div>
          <div>
            <Label required>{t("maxAge")}</Label>
            <input
              {...register("maxAge", { valueAsNumber: true })}
              type="number"
              min={0}
              max={120}
              className={inputClass}
            />
            <FieldError message={errors.maxAge?.message} />
          </div>
        </div>
```

- [ ] **Step 5: Payload meesturen**

In `step-3-validation.tsx`, in het object dat naar `createLessonSeriesWizard` gaat (na `maxRegistrations: step1Data.maxRegistrations,`, rond regel 155):

```tsx
      minAge: step1Data.minAge,
      maxAge: step1Data.maxAge,
```

- [ ] **Step 6: Build**

```bash
cd frontend && bun run build
```

Verwacht: build slaagt.

- [ ] **Step 7: Commit**

```bash
git add frontend/
git commit -m "feat(wizard): min/max leeftijd in stap 1 van de reeks-aanmaakflow"
```

---

## Task 5: Publieke weergave, client-check & bewerken

**Files:**
- Modify: `frontend/lib/api/enrollments.ts` (`PublicLessonSeriesDto`)
- Modify: `frontend/app/(public)/enroll/[seriesId]/page.tsx`
- Modify: `frontend/lib/api/lessonSeries.ts` (`UpdateLessonSeriesRequest`)
- Modify: `frontend/app/(dashboard)/dashboard/lessons/[id]/page.tsx` (`EditSeriesForm`)

**Interfaces:**
- Consumes: `PublicLessonSeriesDto.minAge`/`.maxAge`, `series.startDate`.
- Produces: publieke weergave + client-side leeftijdscheck + bewerkbare velden.

- [ ] **Step 1: Publieke FE-type uitbreiden**

In `lib/api/enrollments.ts`, in `PublicLessonSeriesDto`, na `maxRegistrations: number | null;`:

```ts
  minAge: number;
  maxAge: number;
```

- [ ] **Step 2: Range tonen op de enroll-pagina**

In `app/(public)/enroll/[seriesId]/page.tsx`, in het reeksinfo-blok (bij `series.tennisClubName` / `series.price`, rond regel 532-536), voeg een regel toe in dezelfde stijl als de omliggende info-items:

```tsx
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <span>Leeftijd: {series.minAge}–{series.maxAge} jaar</span>
            </div>
```

(Match het exacte wrapper-/icoonpatroon van de naburige regels in dat blok.)

- [ ] **Step 3: Client-side leeftijdscheck**

In dezelfde `page.tsx`, zoek de `validate()`-functie. Voeg een helper toe boven de component (naast `validateBirthDate`):

```tsx
/** Leeftijd in hele jaren op een peildatum (yyyy-MM-dd strings). */
function ageOn(dob: string, onDate: string): number | null {
  if (!dob || !onDate) return null;
  const b = new Date(dob + "T00:00:00");
  const d = new Date(onDate + "T00:00:00");
  if (Number.isNaN(b.getTime()) || Number.isNaN(d.getTime())) return null;
  let age = d.getFullYear() - b.getFullYear();
  const m = d.getMonth() - b.getMonth();
  if (m < 0 || (m === 0 && d.getDate() < b.getDate())) age--;
  return age;
}
```

Voeg in `validate()`, waar de geboortedatum van de leider al gecheckt wordt (`validateBirthDate(dateOfBirth)`), na die check toe:

```tsx
    const leaderAge = ageOn(dateOfBirth, series.startDate);
    if (leaderAge !== null && (leaderAge < series.minAge || leaderAge > series.maxAge)) {
      errors.dateOfBirth = `Leeftijd moet tussen ${series.minAge} en ${series.maxAge} jaar zijn`;
    }
```

En binnen de `groupMembers.forEach((m, i) => {...})`, na de bestaande `validateBirthDate(m.dateOfBirth)`-check:

```tsx
        const memberAge = ageOn(m.dateOfBirth, series.startDate);
        if (memberAge !== null && (memberAge < series.minAge || memberAge > series.maxAge)) {
          e.dateOfBirth = `Leeftijd moet tussen ${series.minAge} en ${series.maxAge} jaar zijn`;
        }
```

(Zorg dat `e.dateOfBirth` daarna in de bestaande `if (e.name || e.email || e.dateOfBirth)`-verzameling meegenomen wordt — dat is al zo.)

- [ ] **Step 4: Update-request FE-type uitbreiden**

In `lib/api/lessonSeries.ts`, in `UpdateLessonSeriesRequest`, na `registrationDeadline?: string;`:

```ts
  minAge: number;
  maxAge: number;
```

- [ ] **Step 5: Bewerken-formulier uitbreiden**

In `app/(dashboard)/dashboard/lessons/[id]/page.tsx`, in `EditSeriesForm`:

Voeg aan `editSchema` (na `registrationDeadline`) toe:

```ts
  minAge: z.number().int().min(0).max(120),
  maxAge: z.number().int().min(0).max(120),
```

En een `.refine` op het schema-object (of, als `editSchema` een plat `z.object` is, wrap met `.refine`):

```ts
}).refine((d) => d.minAge <= d.maxAge, {
  message: "Minimumleeftijd mag niet groter zijn dan de maximumleeftijd",
  path: ["maxAge"],
});
```

In de `updateLessonSeries(seriesId, {...})`-payload (na `registrationDeadline: ...`):

```ts
        minAge: data.minAge,
        maxAge: data.maxAge,
```

Voeg twee getalvelden toe aan het formulier (naast de prijs/deadline-grid), met hardcoded labels "Min. leeftijd" / "Max. leeftijd" in dezelfde stijl als de bestaande velden, elk met `{...register("minAge", { valueAsNumber: true })}` resp. `maxAge` en een `FieldError`.

Vul de `defaultValues` bij het openen van het formulier aan met `minAge: series.minAge ?? 3` en `maxAge: series.maxAge ?? 99` (zoek waar `registrationDeadline` uit `series` wordt gezet, rond regel 1872, en voeg de twee toe). Zorg dat de FE `LessonSeriesDto` (het `series`-type op de detailpagina) `minAge`/`maxAge` bevat — voeg die velden toe aan het betreffende type in `lib/api/lessonSeries.ts` als ze er nog niet staan.

- [ ] **Step 6: Build**

```bash
cd frontend && bun run build
```

Verwacht: build slaagt.

- [ ] **Step 7: Commit**

```bash
git add frontend/
git commit -m "feat(enroll): toon leeftijdsgrens, spiegel de check en maak hem bewerkbaar"
```

---

## Task 6: Seed & reset

**Files:**
- Modify: `backend/Scripts/seed-data.json`

**Interfaces:**
- Consumes: het volledige contract uit taken 1–5.

- [ ] **Step 1: Seed-reeksen een range geven**

In `seed-data.json`, voeg aan minstens één lessenreeks-definitie `"minAge"` en `"maxAge"` toe (bv. een jeugdreeks `"minAge": 6, "maxAge": 12` en een algemene reeks met de defaults). Controleer dat de gezaaide De Boer-groep (kinderen ~11 en ~13 jaar) binnen de gekozen range van hun reeks valt — pas de range of de reeks aan zodat de seed niet op de leeftijdsgrens sneuvelt.

Controleer of `seed-demo-data.py` het reeks-body-object samenstelt uit vaste keys (dan `minAge`/`maxAge` toevoegen aan de body-opbouw) of de JSON-velden doorgeeft; pas de body-opbouw aan zodat `minAge`/`maxAge` meegestuurd worden bij `POST /lessonseries`.

- [ ] **Step 2: Volledige reset draaien**

```bash
cd backend
bash Scripts/reset-db.sh --no-frontend
```

Wacht tot `http://localhost:5142/health` 200 geeft, dan:

```bash
bash Scripts/seed-demo-data.sh
```

Verwacht: script loopt volledig door zonder 4xx/5xx; de migratie maakt `MinAge`/`MaxAge` aan met defaults 3/99.

- [ ] **Step 3: Handmatige controle**

Open het dashboard (`jan@deaces.be` / `Demo1234!`) → een gezaaide jeugdreeks → bevestig dat de leeftijdsgrens getoond/bewerkbaar is. Open de publieke inschrijflink en bevestig "Leeftijd: X–Y jaar" + dat een te jonge geboortedatum client-side een fout geeft.

- [ ] **Step 4: Volledige checks**

```bash
cd backend && dotnet test CoachOS.slnx
cd ../frontend && bun run build
```

Verwacht: alles groen.

- [ ] **Step 5: Commit**

```bash
git add backend/Scripts/
git commit -m "chore(scripts): seed lessenreeksen met een leeftijdsgrens"
```

---

## Volgorde en afhankelijkheden

Taken 1 → 2 → 3 zijn backend en moeten in volgorde (contract vóór afdwinging). Taak 4 en 5 (frontend) hebben taak 2 nodig voor het contract; onderling kunnen ze in willekeurige volgorde. Taak 6 sluit af met de definitieve reset + seed.
