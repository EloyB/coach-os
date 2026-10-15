# Prijsoptie aanpassen in de inschrijf-dialog — Implementatieplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** De planner kan in de aanpas-dialog van een inschrijving de gekozen prijsoptie wijzigen (solo of hele groep), zolang de inschrijving nog niet betaald/bevestigd is.

**Architecture:** Uitbreiding van de bestaande basis-update. `Enrollment.SelectedPriceOptionId` bestaat al (geen schemawijziging). De backend-service `UpdateBasicEnrollmentAsync` krijgt gate + optie-validatie + groep-propagatie; de dialog krijgt een dropdown met de reeks-prijsopties.

**Tech Stack:** .NET 10 (service pattern, Result<T>, FluentValidation), Next.js 15 + react-hook-form + Zod + next-intl, NUnit/Moq/FluentAssertions.

## Global Constraints

- Geen hardcoded Nederlandse strings op de FE — alles via `next-intl` in `messages/nl.json`.
- Geen `any` in TypeScript; Zod v4 zonder `z.coerce`.
- Backend: business-fouten via `Result<T>.Failure(...)`, nooit exceptions; elke service filtert op `organizationId`.
- Groep-reikwijdte: een prijsoptie-wijziging geldt voor **alle leden** van de groep.
- Gate: prijsoptie enkel aanpasbaar bij status `Pending`; geblokkeerd bij `Confirmed`, `PendingPayment`, `Cancelled`.
- Selector enkel tonen wanneer de reeks prijsopties heeft.
- Autorisatie ongewijzigd: `UpdateBasicEnrollmentEndpoint` blijft Admin-only.

---

## File Structure

**Backend**
- `CoachOS.Application/Enrollments/DTOs/LessonSerieEnrollmentDto.cs` — veld `SelectedPriceOptionId` toevoegen.
- `CoachOS.Application/Enrollments/DTOs/UpdateBasicEnrollmentRequest.cs` — veld `SelectedPriceOptionId` toevoegen.
- `CoachOS.Application/Enrollments/EnrollmentService.cs` — DTO-buildsites (2×) + gate/validatie/propagatie in `UpdateBasicEnrollmentAsync`; ctor krijgt `ILessonSeriePriceRepository`.
- `CoachOS.Tests/Services/EnrollmentServiceTests.cs` + `SharedContactEmailTests.cs` — ctor-mock toevoegen.
- `CoachOS.Tests/Services/EnrollmentPriceOptionTests.cs` — nieuwe unit-tests.

**Frontend**
- `frontend/lib/api/enrollments.ts` — `LessonSeriesEnrollmentDto.selectedPriceOptionId` + `UpdateBasicEnrollmentRequest.selectedPriceOptionId`.
- `frontend/app/(dashboard)/dashboard/lessons/[id]/_components/enrollments-table.tsx` — `EditEnrollmentDialog`: prijsopties ophalen, dropdown, gate, groep-hint, meesturen bij Opslaan.
- `frontend/messages/nl.json` — nieuwe `enrollmentsTable`-keys.

---

## Task 1: Backend — prijsoptie exposen in DTO's

**Files:**
- Modify: `backend/CoachOS.Application/Enrollments/DTOs/LessonSerieEnrollmentDto.cs`
- Modify: `backend/CoachOS.Application/Enrollments/DTOs/UpdateBasicEnrollmentRequest.cs`
- Modify: `backend/CoachOS.Application/Enrollments/EnrollmentService.cs` (2 DTO-buildsites)

**Interfaces:**
- Produces: `LessonSerieEnrollmentDto.SelectedPriceOptionId` (`Guid?`) — gelezen door de FE voor preselectie. `UpdateBasicEnrollmentRequest.SelectedPriceOptionId` (`Guid?`) — meegestuurd bij Opslaan.

- [ ] **Step 1: Voeg veld toe aan `LessonSerieEnrollmentDto`**

In `LessonSerieEnrollmentDto.cs`, na `IsOpenToGrouping`:

```csharp
    public bool IsOpenToGrouping { get; set; }

    /// <summary>Gekozen prijsoptie (null = geen optie/legacy prijs). Voor de aanpas-dialog.</summary>
    public Guid? SelectedPriceOptionId { get; set; }

    public List<EnrollmentResponseItemDto> FormResponses { get; set; } = new();
```

- [ ] **Step 2: Voeg veld toe aan `UpdateBasicEnrollmentRequest`**

In `UpdateBasicEnrollmentRequest.cs`, na `IsOpenToGrouping`:

```csharp
    public bool IsOpenToGrouping { get; init; }

    /// <summary>Nieuwe prijsoptie voor deze inschrijving (en, bij een groep, alle leden). Null laat ze ongemoeid/leeg.</summary>
    public Guid? SelectedPriceOptionId { get; init; }
```

- [ ] **Step 3: Vul `SelectedPriceOptionId` in beide DTO-buildsites**

In `EnrollmentService.cs`, de lijst-build (rond de `enrollments.Select(e => new LessonSerieEnrollmentDto { ... })`): voeg toe binnen de initializer:

```csharp
            IsOpenToGrouping = e.IsOpenToGrouping,
            SelectedPriceOptionId = e.SelectedPriceOptionId,
            FormResponses = e.FormResponses
```

En in de inline DTO aan het einde van `UpdateBasicEnrollmentAsync` (na `IsOpenToGrouping = enrollment.IsOpenToGrouping,`):

```csharp
            IsOpenToGrouping = enrollment.IsOpenToGrouping,
            SelectedPriceOptionId = enrollment.SelectedPriceOptionId,
        };
```

- [ ] **Step 4: Build**

Run: `cd backend && dotnet build CoachOS.slnx -clp:ErrorsOnly`
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add backend/CoachOS.Application/Enrollments/DTOs/LessonSerieEnrollmentDto.cs \
        backend/CoachOS.Application/Enrollments/DTOs/UpdateBasicEnrollmentRequest.cs \
        backend/CoachOS.Application/Enrollments/EnrollmentService.cs
git commit -m "feat(enrollments): expose SelectedPriceOptionId in inschrijf-DTO's"
```

---

## Task 2: Backend — gate + validatie + groep-propagatie in `UpdateBasicEnrollmentAsync`

**Files:**
- Modify: `backend/CoachOS.Application/Enrollments/EnrollmentService.cs`
- Modify: `backend/CoachOS.Tests/Services/EnrollmentServiceTests.cs` (ctor-mock)
- Modify: `backend/CoachOS.Tests/Services/SharedContactEmailTests.cs` (ctor-mock)
- Create: `backend/CoachOS.Tests/Services/EnrollmentPriceOptionTests.cs`

**Interfaces:**
- Consumes: `ILessonSeriePriceRepository.GetBySeriesAsync(seriesId, orgId, ct)` → `IReadOnlyList<LessonSeriePrice>`; `IEnrollmentRepository.GetByIdWithGroupAsync(id, orgId, ct)` → `Enrollment?` met `EnrollmentGroup.Members` (getrackt).
- Produces: `UpdateBasicEnrollmentAsync` past bij een gewijzigde optie de gate + validatie + propagatie toe.

- [ ] **Step 1: Schrijf de falende tests**

Maak `backend/CoachOS.Tests/Services/EnrollmentPriceOptionTests.cs`. Volg het ctor-patroon van `EnrollmentServiceTests` (dezelfde mocks) en voeg `Mock<ILessonSeriePriceRepository> _priceRepo` toe. De service-ctor-volgorde is:
`enrollmentRepo, enrollmentFormRepo, lessonSeriesRepo, enrollmentGroupRepo, timeSlotPreferenceRepo, orgSettingsRepo, userLookup, emailOutboxRepository, mapper, logger` → **voeg `priceRepo` toe net vóór `mapper`** (zie Step 3).

```csharp
using CoachOS.Application.Enrollments;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class EnrollmentPriceOptionTests
{
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<IEnrollmentFormRepository> _enrollmentFormRepo = null!;
    private Mock<ILessonSerieRepository> _lessonSeriesRepo = null!;
    private Mock<IEnrollmentGroupRepository> _enrollmentGroupRepo = null!;
    private Mock<ITimeSlotPreferenceRepository> _timeSlotPreferenceRepo = null!;
    private Mock<IOrganizationSettingsRepository> _orgSettingsRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IEmailOutboxRepository> _emailOutboxRepository = null!;
    private Mock<ILessonSeriePriceRepository> _priceRepo = null!;
    private Mock<ILogger<EnrollmentService>> _logger = null!;
    private ApplicationMapper _mapper = null!;
    private EnrollmentService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid OptionA = Guid.NewGuid();
    private static readonly Guid OptionB = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _enrollmentFormRepo = new Mock<IEnrollmentFormRepository>();
        _lessonSeriesRepo = new Mock<ILessonSerieRepository>();
        _enrollmentGroupRepo = new Mock<IEnrollmentGroupRepository>();
        _timeSlotPreferenceRepo = new Mock<ITimeSlotPreferenceRepository>();
        _orgSettingsRepo = new Mock<IOrganizationSettingsRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _emailOutboxRepository = new Mock<IEmailOutboxRepository>();
        _priceRepo = new Mock<ILessonSeriePriceRepository>();
        _logger = new Mock<ILogger<EnrollmentService>>();
        _mapper = new ApplicationMapper();

        _service = new EnrollmentService(
            _enrollmentRepo.Object, _enrollmentFormRepo.Object, _lessonSeriesRepo.Object,
            _enrollmentGroupRepo.Object, _timeSlotPreferenceRepo.Object, _orgSettingsRepo.Object,
            _userLookup.Object, _emailOutboxRepository.Object, _priceRepo.Object, _mapper, _logger.Object);

        // Geen duplicaat; reeks bevat OptionA en OptionB.
        _enrollmentRepo
            .Setup(r => r.IsDuplicateParticipantExceptAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _priceRepo
            .Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LessonSeriePrice>
            {
                new() { Id = OptionA, LessonSerieId = SeriesId, Label = "Groep van 3", TotalPrice = 100 },
                new() { Id = OptionB, LessonSerieId = SeriesId, Label = "Groep van 4", TotalPrice = 90 },
            });
    }

    private static Enrollment SoloEnrollment(EnrollmentStatus status, Guid? option) => new()
    {
        Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId,
        StudentName = "Lars Peeters", ContactEmail = "lars@test.local", DateOfBirth = new DateOnly(2000, 1, 1),
        Status = status, SelectedPriceOptionId = option,
    };

    private static UpdateBasicEnrollmentRequest Request(Guid? option) => new()
    {
        StudentName = "Lars Peeters", ContactEmail = "lars@test.local",
        DateOfBirth = "2000-01-01", IsOpenToGrouping = false, SelectedPriceOptionId = option,
    };

    [Test]
    public async Task Solo_Pending_ChangeOption_Persists()
    {
        Enrollment e = SoloEnrollment(EnrollmentStatus.Pending, OptionA);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(e.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, e.Id, OrgId, Request(OptionB), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        e.SelectedPriceOptionId.Should().Be(OptionB);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Group_Pending_ChangeOption_AppliesToAllMembers()
    {
        Guid groupId = Guid.NewGuid();
        Enrollment leader = SoloEnrollment(EnrollmentStatus.Pending, OptionA);
        leader.EnrollmentGroupId = groupId;
        Enrollment member = SoloEnrollment(EnrollmentStatus.Pending, OptionA);
        member.EnrollmentGroupId = groupId;
        EnrollmentGroup group = new() { Id = groupId, Members = new List<Enrollment> { leader, member } };
        leader.EnrollmentGroup = group; member.EnrollmentGroup = group;

        _enrollmentRepo.Setup(r => r.GetByIdAsync(leader.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(leader);
        _enrollmentRepo.Setup(r => r.GetByIdWithGroupAsync(leader.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(leader);

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, leader.Id, OrgId, Request(OptionB), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        leader.SelectedPriceOptionId.Should().Be(OptionB);
        member.SelectedPriceOptionId.Should().Be(OptionB);
    }

    [Test]
    public async Task Confirmed_ChangeOption_ReturnsConflict()
    {
        Enrollment e = SoloEnrollment(EnrollmentStatus.Confirmed, OptionA);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(e.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, e.Id, OrgId, Request(OptionB), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Code == ErrorCodes.Conflict);
        e.SelectedPriceOptionId.Should().Be(OptionA);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PendingPayment_ChangeOption_ReturnsConflict()
    {
        Enrollment e = SoloEnrollment(EnrollmentStatus.PendingPayment, OptionA);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(e.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, e.Id, OrgId, Request(OptionB), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task InvalidOption_ReturnsValidation()
    {
        Enrollment e = SoloEnrollment(EnrollmentStatus.Pending, OptionA);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(e.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        Guid unknown = Guid.NewGuid();

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, e.Id, OrgId, Request(unknown), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Code == ErrorCodes.Validation);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Confirmed_UnchangedOption_StillUpdatesBasicFields()
    {
        // Optie ongewijzigd → geen gate; basis-update (bv. telefoon) mag gewoon door, ook bij Confirmed.
        Enrollment e = SoloEnrollment(EnrollmentStatus.Confirmed, OptionA);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(e.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        UpdateBasicEnrollmentRequest req = Request(OptionA) with { StudentPhone = "+32470000000" };

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, e.Id, OrgId, req, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        e.StudentPhone.Should().Be("+32470000000");
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

- [ ] **Step 2: Run de tests → falen (compileert niet: ctor mist priceRepo)**

Run: `cd backend && dotnet build CoachOS.slnx -clp:ErrorsOnly`
Expected: FAIL — `EnrollmentService` heeft geen ctor-overload met `ILessonSeriePriceRepository`.

- [ ] **Step 3: Injecteer `ILessonSeriePriceRepository` in `EnrollmentService`**

In `EnrollmentService.cs`, ctor — voeg `priceRepo` toe net vóór `mapper`:

```csharp
public class EnrollmentService(
    IEnrollmentRepository enrollmentRepo,
    IEnrollmentFormRepository enrollmentFormRepo,
    ILessonSerieRepository lessonSeriesRepo,
    IEnrollmentGroupRepository enrollmentGroupRepo,
    ITimeSlotPreferenceRepository timeSlotPreferenceRepo,
    IOrganizationSettingsRepository orgSettingsRepo,
    IUserLookupService userLookup,
    IEmailOutboxRepository emailOutboxRepository,
    ILessonSeriePriceRepository priceRepo,
    ApplicationMapper mapper,
    ILogger<EnrollmentService> logger) : IEnrollmentService
{
```

- [ ] **Step 4: Voeg gate + validatie + propagatie toe in `UpdateBasicEnrollmentAsync`**

In `UpdateBasicEnrollmentAsync`, direct **vóór** `await enrollmentRepo.SaveChangesAsync(ct);` (dus na de basis-veldmutaties):

```csharp
        // Prijsoptie: enkel behandelen wanneer ze effectief wijzigt.
        if (request.SelectedPriceOptionId != enrollment.SelectedPriceOptionId)
        {
            // Gate: niet meer aanpasbaar zodra betaald/bevestigd of een betaling loopt.
            if (enrollment.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.PendingPayment)
                return Result<LessonSerieEnrollmentDto>.Fail(new Error(ErrorCodes.Conflict,
                    "De prijsoptie kan niet meer aangepast worden: deze inschrijving is al betaald of bevestigd."));

            // Validatie: een gekozen optie moet bij deze reeks horen (null = optie wissen, toegestaan).
            if (request.SelectedPriceOptionId is Guid optionId)
            {
                IReadOnlyList<LessonSeriePrice> options =
                    await priceRepo.GetBySeriesAsync(lessonSeriesId, organizationId, ct);
                if (options.All(o => o.Id != optionId))
                    return Result<LessonSerieEnrollmentDto>.Fail(new Error(ErrorCodes.Validation,
                        "Geselecteerde prijsoptie hoort niet bij deze lessenreeks."));
            }

            // Propagatie: groep → alle leden; solo → enkel deze inschrijving.
            if (enrollment.EnrollmentGroupId is not null)
            {
                Enrollment? withGroup =
                    await enrollmentRepo.GetByIdWithGroupAsync(enrollmentId, organizationId, ct);
                List<Enrollment> members =
                    withGroup?.EnrollmentGroup?.Members.ToList() ?? [enrollment];
                foreach (Enrollment member in members)
                {
                    member.SelectedPriceOptionId = request.SelectedPriceOptionId;
                    member.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                enrollment.SelectedPriceOptionId = request.SelectedPriceOptionId;
            }
        }
```

> Let op: de basis-veldmutaties op `enrollment` zijn al toegepast maar nog niet opgeslagen; een gate-`return` vóór `SaveChangesAsync` persisteert dus niets (atomair). `GetByIdWithGroupAsync` levert binnen dezelfde DbContext dezelfde getrackte `enrollment`-instance mét groep, zodat de mutaties meegaan in de bestaande `SaveChangesAsync`.

- [ ] **Step 5: Voeg de ctor-mock toe in de twee bestaande testfixtures**

In `EnrollmentServiceTests.cs` en `SharedContactEmailTests.cs`: declareer `private Mock<ILessonSeriePriceRepository> _priceRepo = null!;`, instantieer in `SetUp` (`_priceRepo = new Mock<ILessonSeriePriceRepository>();`), en voeg `_priceRepo.Object` toe **net vóór `_mapper`/`_mapper.Object`** in de `new EnrollmentService(...)`-aanroep.

- [ ] **Step 6: Run alle enrollment-tests → groen**

Run: `cd backend && dotnet build CoachOS.slnx -clp:ErrorsOnly && dotnet test CoachOS.slnx --no-build --filter "FullyQualifiedName~Enrollment|FullyQualifiedName~SharedContactEmail"`
Expected: PASS (incl. de 6 nieuwe tests).

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Application/Enrollments/EnrollmentService.cs \
        backend/CoachOS.Tests/Services/EnrollmentServiceTests.cs \
        backend/CoachOS.Tests/Services/SharedContactEmailTests.cs \
        backend/CoachOS.Tests/Services/EnrollmentPriceOptionTests.cs
git commit -m "feat(enrollments): prijsoptie aanpasbaar met gate + groep-propagatie"
```

---

## Task 3: Frontend — prijsoptie-dropdown in de aanpas-dialog

**Files:**
- Modify: `frontend/lib/api/enrollments.ts`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/[id]/_components/enrollments-table.tsx`
- Modify: `frontend/messages/nl.json`

**Interfaces:**
- Consumes: `getLessonSeriePrices(seriesId)` uit `@/lib/api/lessonSeriePrices` → `LessonSeriePriceDto[]` (`{ id, label, description, totalPrice, sortOrder, reusableKey }`).
- Produces: `updateBasicEnrollment` stuurt `selectedPriceOptionId` mee.

- [ ] **Step 1: Breid de FE-types uit**

In `frontend/lib/api/enrollments.ts` — voeg toe aan de enrollment-DTO (na `isOpenToGrouping`):

```typescript
  isOpenToGrouping: boolean;
  /** Gekozen prijsoptie (null = geen/legacy). */
  selectedPriceOptionId: string | null;
  formResponses: EnrollmentResponseItem[];
```

En aan `UpdateBasicEnrollmentRequest` (na `isOpenToGrouping`):

```typescript
  isOpenToGrouping: boolean;
  /** Nieuwe prijsoptie (weglaten = ongemoeid). */
  selectedPriceOptionId?: string | null;
```

- [ ] **Step 2: `EditEnrollmentDialog` — prijsopties ophalen + form-veld**

In `enrollments-table.tsx`, boven in `EditEnrollmentDialog` (het component dat `useForm<BasicEnrollmentFormValues>` gebruikt):

1. Imports bovenaan het bestand aanvullen:

```typescript
import { getLessonSeriePrices } from "@/lib/api/lessonSeriePrices";
```

2. In `EditEnrollmentDialog`, na de bestaande `const t = useTranslations("enrollmentsTable");`:

```typescript
  const { data: priceOptions = [] } = useQuery({
    queryKey: ["lessonSeriePrices", seriesId],
    queryFn: () => getLessonSeriePrices(seriesId),
  });

  // Prijsoptie is vergrendeld zodra er betaald/bevestigd is of een betaling loopt.
  const priceLocked =
    enrollment.status === "Confirmed" ||
    enrollment.status === "PendingPayment" ||
    enrollment.status === "Cancelled";
  const inGroup = enrollment.enrollmentGroupId !== null;
```

3. Voeg `selectedPriceOptionId` toe aan de Zod-schema (`basicEnrollmentSchema`) en aan `useForm`'s `values`:

In `basicEnrollmentSchema`: `selectedPriceOptionId: z.string().optional(),`
In `useForm({ ... values: { ... isOpenToGrouping: enrollment.isOpenToGrouping, selectedPriceOptionId: enrollment.selectedPriceOptionId ?? undefined } })`.

- [ ] **Step 3: Render de dropdown (enkel bij opties)**

In het `<form>` van `EditEnrollmentDialog`, bij de andere velden (bv. na het telefoon-veld), voeg toe:

```tsx
        {priceOptions.length > 0 && (
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">
              {t("priceOptionLabel")}
            </label>
            {priceLocked ? (
              <p className="rounded-md border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600">
                {priceOptions.find((o) => o.id === enrollment.selectedPriceOptionId)?.label
                  ?? t("priceOptionNone")}
                <span className="mt-1 block text-xs text-gray-400">{t("priceOptionLocked")}</span>
              </p>
            ) : (
              <>
                <select
                  {...form.register("selectedPriceOptionId")}
                  className={inputClass}
                >
                  {priceOptions
                    .slice()
                    .sort((a, b) => a.sortOrder - b.sortOrder)
                    .map((o) => (
                      <option key={o.id} value={o.id}>
                        {o.label} — €{o.totalPrice}
                      </option>
                    ))}
                </select>
                {inGroup && (
                  <p className="mt-1 text-xs text-gray-400">{t("priceOptionGroupHint")}</p>
                )}
              </>
            )}
          </div>
        )}
```

- [ ] **Step 4: Stuur `selectedPriceOptionId` mee bij Opslaan**

In de `mutation` van `EditEnrollmentDialog` (`updateBasicEnrollment(seriesId, enrollment.id, { ... })`), voeg toe aan het request-object:

```typescript
        isOpenToGrouping: values.isOpenToGrouping,
        selectedPriceOptionId: values.selectedPriceOptionId,
```

(Bij `priceLocked` verandert de waarde niet — de dropdown wordt dan niet gerenderd, dus `values.selectedPriceOptionId` blijft de initiële waarde en de backend-gate slaat sowieso aan als iemand toch een andere waarde forceert.)

- [ ] **Step 5: Voeg de nl.json-keys toe**

In `frontend/messages/nl.json`, in het `enrollmentsTable`-object:

```json
    "priceOptionLabel": "Prijsoptie",
    "priceOptionNone": "Geen prijsoptie",
    "priceOptionLocked": "Vergrendeld: deze inschrijving is al betaald of bevestigd.",
    "priceOptionGroupHint": "Geldt voor de hele groep."
```

- [ ] **Step 6: Typecheck + lint**

Run: `cd frontend && bunx tsc --noEmit && bunx eslint "app/(dashboard)/dashboard/lessons/[id]/_components/enrollments-table.tsx"`
Expected: geen fouten (de bekende `react-hooks/set-state-in-effect`-melding elders telt niet mee).

- [ ] **Step 7: Commit**

```bash
git add frontend/lib/api/enrollments.ts \
        "frontend/app/(dashboard)/dashboard/lessons/[id]/_components/enrollments-table.tsx" \
        frontend/messages/nl.json
git commit -m "feat(enrollments): prijsoptie-dropdown in de aanpas-dialog"
```

---

## Task 4: Verificatie — build, tests, reset + seed, manuele E2E

**Files:** geen (verificatie).

- [ ] **Step 1: Volledige backend-suite**

Run: `cd backend && dotnet test CoachOS.slnx`
Expected: alle tests groen (incl. de nieuwe `EnrollmentPriceOptionTests`).

- [ ] **Step 2: Frontend build**

Run: `cd frontend && bun run build`
Expected: build slaagt.

- [ ] **Step 3: Seed-scripts nalopen**

Controleer of `backend/Scripts/seed-demo-data.*` een prijsoptie via de basis-update zet — vermoedelijk niet (de seed zet de optie enkel bij het inschrijven). Contractwijziging is additief (nieuw optioneel veld), dus geen aanpassing verwacht. Alleen aanpassen als de seed faalt.

- [ ] **Step 4: Reset + seed (definitieve E2E-check)**

```bash
cd backend
bash Scripts/reset-db.sh --no-frontend
# wacht tot http://localhost:5142/health → 200
bash Scripts/seed-demo-data.sh
```
Expected: reset + seed lopen groen.

- [ ] **Step 5: Manuele E2E in de app**

Op een reeks **met** prijsopties: open een inschrijving met status Pending → wijzig de prijsoptie → Opslaan → controleer dat de waarde bewaard blijft (heropen de dialog). Bij een groep: controleer dat alle leden mee wijzigen. Op een `Confirmed` inschrijving: controleer dat de optie read-only is.

- [ ] **Step 6: Commit (indien seed-scripts aangepast)**

```bash
git add backend/Scripts/
git commit -m "chore(seed): prijsoptie-aanpassing meenemen in seed indien nodig"
```

---

## Self-Review

- **Spec-dekking:** groep-propagatie (Task 2), gate op betaling (Task 2 + Task 3 UI), selector enkel bij opties (Task 3 Step 3), opslag via bestaande Opslaan (Task 3 Step 4), geen schemawijziging (geen migratie-taak). ✓
- **Types consistent:** backend `SelectedPriceOptionId : Guid?` ↔ FE `selectedPriceOptionId : string | null` (query) / `?: string | null` (request). Gate-statussen identiek in backend (`Confirmed`/`PendingPayment`) en FE (plus `Cancelled` voor read-only weergave). ✓
- **Geen placeholders:** alle stappen bevatten concrete code/commando's. ✓
