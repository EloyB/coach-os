# Lid manueel toevoegen aan een bestaande groep — Implementatieplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Admin/hoofdtrainer kan een deelnemer manueel aan een bestaande groep toevoegen; het lid erft de status + prijsoptie van de groep, geblokkeerd bij een betaalde/bevestigde groep.

**Architecture:** Nieuwe `AddGroupMemberAsync` in `EnrollmentService` (hergebruikt de manuele-solo-validaties), nieuw endpoint, en de bestaande `ManualEnrollmentDialog` uitgebreid met een optionele `groupId`. Geen schemawijziging.

**Tech Stack:** .NET 10 (service pattern, Result<T>, FluentValidation), Next.js 15 + React Query + next-intl, NUnit/Moq/FluentAssertions.

## Global Constraints

- Business-fouten via `Result<T>.Fail(...)`, nooit exceptions; service filtert op `organizationId`.
- Geen hardcoded Nederlandse strings op de FE — alles via `next-intl` (`messages/nl.json`); geen `any`.
- Nieuw lid **erft** `Status` en `SelectedPriceOptionId` van de groepsleider.
- Toevoegen **geblokkeerd** (`Conflict`) als de leider `Confirmed`/`PendingPayment` is.
- Hergebruik de manuele-solo-validaties: leeftijd, formulier, duplicaat, capaciteit.
- **Geen** bevestigingsmail bij toevoegen (lid is nog niet bevestigd).
- Nieuw lid is nooit de leider; groep-`LeaderEnrollmentId` blijft ongewijzigd.
- Autorisatie: admin + hoofdtrainer (`HeadTrainerAccess.EnsureSerieAccessAsync` + `EnsureManualEnrollmentAllowed`), zoals de andere groep/manueel-endpoints op deze branch.

## Referentie (bestaande code, ongewijzigd)

- `EnrollmentService` ctor injecteert al: `enrollmentRepo, enrollmentFormRepo, lessonSeriesRepo, enrollmentGroupRepo, timeSlotPreferenceRepo, orgSettingsRepo, userLookup, emailOutboxRepository, mapper, logger` — **geen ctor-wijziging**.
- Hergebruikte helpers/patronen uit `CreateManualEnrollmentAsync`: `lessonSeriesRepo.GetByIdPublicAsync`, `DateOfBirthRules.TryParse`, `CheckAgeEligibility(SubmitEnrollmentRequest, series)`, `enrollmentFormRepo.GetBySeriesIdReadOnlyAsync` + `FormResponseValidator.Validate`, `orgSettingsRepo.GetByOrganizationReadOnlyAsync`, `EnrollmentEmails.Normalize`, serializable transactie (`BeginTransactionAsync(IsolationLevel.Serializable)` / `CommitTransactionAsync` / `RollbackTransactionAsync`), `CountActiveBySeriesAsync`, `enrollmentRepo.IsDuplicateParticipantAsync`, `ResolveCategory(dobString, youthMaxAge, DateOnly)`, `enrollmentRepo.AddAsync`, `enrollmentRepo.AddFormResponseAsync`, `enrollmentRepo.SaveChangesAsync`.
- `enrollmentGroupRepo.GetByIdAsync(groupId, orgId)` laadt `Include(g => g.Members)` (getrackt).
- `CreateManualEnrollmentRequest`: `StudentName, ContactEmail, StudentEmail?, StudentPhone?, DateOfBirth, Responses`.
- Endpoint-patroon: `CreateManualEnrollmentEndpoint` (POST `/lessonseries/{id}/enrollments/manual`, HeadTrainerAccess-gates, `ValidationFilter<CreateManualEnrollmentRequest>`, `RequireRole("Admin","Trainer")`).

---

## Task 1: Backend — `AddGroupMemberAsync` service + interface + endpoint + unit tests

**Files:**
- Modify: `backend/CoachOS.Application/Enrollments/IEnrollmentService.cs`
- Modify: `backend/CoachOS.Application/Enrollments/EnrollmentService.cs`
- Create: `backend/CoachOS.API/Endpoints/LessonSerie/AddGroupMemberEndpoint.cs`
- Modify: `backend/CoachOS.Tests/Services/EnrollmentServiceTests.cs`

**Interfaces:**
- Produces: `IEnrollmentService.AddGroupMemberAsync(Guid lessonSeriesId, Guid groupId, CreateManualEnrollmentRequest request, Guid organizationId, CancellationToken ct = default) → Task<Result<Guid>>`.
- Endpoint: `POST /lessonseries/{id}/enrollment-groups/{groupId}/members` → `201` met de nieuwe id.

- [ ] **Step 1: Schrijf de falende tests**

Voeg tests toe aan `EnrollmentServiceTests.cs` (de fixture heeft al alle benodigde mocks: `_enrollmentRepo, _enrollmentGroupRepo, _lessonSeriesRepo, _orgSettingsRepo, _enrollmentFormRepo, _emailOutboxRepository`). Als er al `CreateManualEnrollment*`-tests bestaan, spiegel hun opzet voor de reeks-/transactie-mocks. Anders volstaat onderstaande.

Helper + tests:

```csharp
private static readonly Guid OrgIdG = Guid.NewGuid();
private static readonly Guid SeriesIdG = Guid.NewGuid();

private (Domain.Entities.LessonSerie Series, Domain.Entities.EnrollmentGroup Group, Domain.Entities.Enrollment Leader)
    SetupGroup(EnrollmentStatus leaderStatus = EnrollmentStatus.Pending, Guid? priceOptionId = null)
{
    Domain.Entities.LessonSerie series = new()
    {
        Id = SeriesIdG, OrganizationId = OrgIdG, Name = "Reeks", TennisClubId = Guid.NewGuid(),
        StartDate = new DateOnly(2026, 9, 1), MinAge = 3, MaxAge = 99, MaxRegistrations = 100,
    };
    Guid groupId = Guid.NewGuid();
    Domain.Entities.Enrollment leader = new()
    {
        Id = Guid.NewGuid(), OrganizationId = OrgIdG, LessonSerieId = SeriesIdG,
        StudentName = "Leider", ContactEmail = "leider@test.local", EnrollmentGroupId = groupId,
        Status = leaderStatus, SelectedPriceOptionId = priceOptionId, EnrolledAt = DateTime.UtcNow,
    };
    Domain.Entities.EnrollmentGroup group = new()
    {
        Id = groupId, OrganizationId = OrgIdG, LessonSerieId = SeriesIdG, Name = "Groep A",
        LeaderEnrollmentId = leader.Id, Members = new List<Domain.Entities.Enrollment> { leader },
    };
    _lessonSeriesRepo.Setup(r => r.GetByIdPublicAsync(SeriesIdG, It.IsAny<CancellationToken>())).ReturnsAsync(series);
    _enrollmentGroupRepo.Setup(r => r.GetByIdAsync(groupId, OrgIdG, It.IsAny<CancellationToken>())).ReturnsAsync(group);
    _enrollmentFormRepo.Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesIdG, It.IsAny<CancellationToken>()))
        .ReturnsAsync((EnrollmentForm?)null);
    _orgSettingsRepo.Setup(r => r.GetByOrganizationReadOnlyAsync(OrgIdG, It.IsAny<CancellationToken>()))
        .ReturnsAsync((OrganizationSettings?)null);
    _enrollmentRepo.Setup(r => r.CountActiveBySeriesAsync(SeriesIdG, It.IsAny<CancellationToken>())).ReturnsAsync(3);
    _enrollmentRepo.Setup(r => r.IsDuplicateParticipantAsync(
        SeriesIdG, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);
    return (series, group, leader);
}

private static CreateManualEnrollmentRequest MemberRequest() => new()
{
    StudentName = "Nieuw Lid", ContactEmail = "nieuw@test.local",
    StudentPhone = "+32470000000", DateOfBirth = "2005-05-05", Responses = new(),
};

[Test]
public async Task AddGroupMember_PendingGroup_CreatesMemberInheritingStatusAndPriceOption()
{
    Guid opt = Guid.NewGuid();
    var (_, group, _) = SetupGroup(EnrollmentStatus.Pending, opt);
    Domain.Entities.Enrollment? added = null;
    _enrollmentRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Enrollment>(), It.IsAny<CancellationToken>()))
        .Callback<Domain.Entities.Enrollment, CancellationToken>((e, _) => added = e)
        .Returns(Task.CompletedTask);

    Result<Guid> result = await _service.AddGroupMemberAsync(
        SeriesIdG, group.Id, MemberRequest(), OrgIdG, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    added.Should().NotBeNull();
    added!.EnrollmentGroupId.Should().Be(group.Id);
    added.Status.Should().Be(EnrollmentStatus.Pending);        // status geërfd
    added.SelectedPriceOptionId.Should().Be(opt);              // prijsoptie geërfd
    _emailOutboxRepository.Verify(r => r.AddRangeAsync(
        It.IsAny<IEnumerable<EmailOutboxMessage>>(), It.IsAny<CancellationToken>()), Times.Never); // geen mail
}

[Test]
public async Task AddGroupMember_ConfirmedGroup_ReturnsConflict()
{
    var (_, group, _) = SetupGroup(EnrollmentStatus.Confirmed);

    Result<Guid> result = await _service.AddGroupMemberAsync(
        SeriesIdG, group.Id, MemberRequest(), OrgIdG, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
    _enrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
}

[Test]
public async Task AddGroupMember_PendingPaymentGroup_ReturnsConflict()
{
    var (_, group, _) = SetupGroup(EnrollmentStatus.PendingPayment);

    Result<Guid> result = await _service.AddGroupMemberAsync(
        SeriesIdG, group.Id, MemberRequest(), OrgIdG, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
}

[Test]
public async Task AddGroupMember_DuplicateParticipant_ReturnsConflict()
{
    var (_, group, _) = SetupGroup(EnrollmentStatus.Pending);
    _enrollmentRepo.Setup(r => r.IsDuplicateParticipantAsync(
        SeriesIdG, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

    Result<Guid> result = await _service.AddGroupMemberAsync(
        SeriesIdG, group.Id, MemberRequest(), OrgIdG, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
}

[Test]
public async Task AddGroupMember_GroupNotInSeries_ReturnsNotFound()
{
    var (_, group, _) = SetupGroup(EnrollmentStatus.Pending);
    _enrollmentGroupRepo.Setup(r => r.GetByIdAsync(group.Id, OrgIdG, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Domain.Entities.EnrollmentGroup?)null);

    Result<Guid> result = await _service.AddGroupMemberAsync(
        SeriesIdG, group.Id, MemberRequest(), OrgIdG, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
}
```

(Voeg ontbrekende `using`-regels toe: `CoachOS.Domain.Enums`, `CoachOS.Domain.Entities`, `CoachOS.Application.Enrollments.DTOs`. Namespaces van entities: `OrganizationSettings`, `EnrollmentForm`, `EmailOutboxMessage` — controleer de exacte namespace/typenaam zoals gebruikt in de bestaande tests/`CreateManualEnrollmentAsync`; `OrganizationSettings` staat mogelijk als alias `OrganizationSettingsEntity` in de service.)

- [ ] **Step 2: Run de tests → falen (methode bestaat niet)**

Run: `cd backend && dotnet build CoachOS.slnx -clp:ErrorsOnly`
Expected: FAIL — `IEnrollmentService`/`EnrollmentService` heeft geen `AddGroupMemberAsync`.

- [ ] **Step 3: Interface-methode**

In `IEnrollmentService.cs`, na `CreateManualEnrollmentAsync`:

```csharp
    Task<Result<Guid>> AddGroupMemberAsync(
        Guid lessonSeriesId, Guid groupId, CreateManualEnrollmentRequest request,
        Guid organizationId, CancellationToken ct = default);
```

- [ ] **Step 4: Implementeer `AddGroupMemberAsync`**

Voeg toe in `EnrollmentService.cs` (spiegelt `CreateManualEnrollmentAsync`, met groep-gate + geërfde status/optie + géén mail):

```csharp
    public async Task<Result<Guid>> AddGroupMemberAsync(
        Guid lessonSeriesId, Guid groupId, CreateManualEnrollmentRequest request,
        Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.LessonSerie? series = await lessonSeriesRepo.GetByIdPublicAsync(lessonSeriesId, ct);
        if (series is null || series.OrganizationId != organizationId)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        Domain.Entities.EnrollmentGroup? group = await enrollmentGroupRepo.GetByIdAsync(groupId, organizationId, ct);
        if (group is null || group.LessonSerieId != lessonSeriesId)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

        Domain.Entities.Enrollment leader =
            group.Members.FirstOrDefault(m => m.Id == group.LeaderEnrollmentId) ?? group.Members.First();

        // Gate: geen lid toevoegen aan een al betaalde/bevestigde groep.
        if (leader.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.PendingPayment)
            return Result<Guid>.Fail(new Error(ErrorCodes.Conflict,
                "Je kan geen lid toevoegen aan een groep die al betaald of bevestigd is."));

        if (!DateOfBirthRules.TryParse(request.DateOfBirth, out DateOnly dateOfBirth))
            return Result<Guid>.Fail(new Error(ErrorCodes.Validation, "Geboortedatum is ongeldig."));

        Error? ageError = CheckAgeEligibility(
            new SubmitEnrollmentRequest { StudentName = request.StudentName, DateOfBirth = request.DateOfBirth }, series);
        if (ageError is not null)
            return Result<Guid>.Fail(ageError);

        EnrollmentForm? form = await enrollmentFormRepo.GetBySeriesIdReadOnlyAsync(lessonSeriesId, ct);
        if (form is not null)
        {
            Error? formError = FormResponseValidator.Validate(
                form.Fields.Select(f => (f.Id, f.IsRequired, f.Label)),
                request.Responses.Select(r => (r.FormFieldId, r.Value)));
            if (formError is not null) return Result<Guid>.Fail(formError);
        }

        OrganizationSettingsEntity? settings =
            await orgSettingsRepo.GetByOrganizationReadOnlyAsync(organizationId, ct);
        int youthMaxAge = settings?.YouthMaxAge ?? 17;
        string contactEmail = EnrollmentEmails.Normalize(request.ContactEmail);

        await enrollmentRepo.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (series.MaxRegistrations.HasValue &&
                await enrollmentRepo.CountActiveBySeriesAsync(lessonSeriesId, ct) >= series.MaxRegistrations.Value)
            {
                await enrollmentRepo.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(new Error(ErrorCodes.Conflict, "Deze lessenreeks is volzet."));
            }

            if (await enrollmentRepo.IsDuplicateParticipantAsync(
                    lessonSeriesId, contactEmail, request.StudentName, dateOfBirth, ct))
            {
                await enrollmentRepo.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(new Error(
                    ErrorCodes.Conflict, $"{request.StudentName} is al ingeschreven voor deze lessenreeks."));
            }

            Domain.Entities.Enrollment enrollment = new()
            {
                OrganizationId = organizationId,
                LessonSerieId = lessonSeriesId,
                EnrollmentGroupId = groupId,
                StudentName = request.StudentName.Trim(),
                ContactEmail = contactEmail,
                StudentEmail = string.IsNullOrWhiteSpace(request.StudentEmail)
                    ? null
                    : EnrollmentEmails.Normalize(request.StudentEmail),
                StudentPhone = request.StudentPhone,
                DateOfBirth = dateOfBirth,
                Category = ResolveCategory(request.DateOfBirth, youthMaxAge, DateOnly.FromDateTime(DateTime.UtcNow)),
                Status = leader.Status,                          // status geërfd van de groep
                SelectedPriceOptionId = leader.SelectedPriceOptionId, // prijsoptie geërfd
                EnrolledAt = DateTime.UtcNow,
                IsOpenToGrouping = false,
            };
            await enrollmentRepo.AddAsync(enrollment, ct);

            foreach (FormResponseValueDto responseDto in request.Responses)
            {
                await enrollmentRepo.AddFormResponseAsync(new FormResponse
                {
                    EnrollmentId = enrollment.Id,
                    FormFieldId = responseDto.FormFieldId,
                    Value = responseDto.Value,
                }, ct);
            }

            await enrollmentRepo.SaveChangesAsync(ct);
            // Bewust geen bevestigingsmail: het lid is nog niet bevestigd; die mail volgt met de groep.
            await enrollmentRepo.CommitTransactionAsync(ct);
            return Result<Guid>.Ok(enrollment.Id);
        }
        catch
        {
            await enrollmentRepo.RollbackTransactionAsync(ct);
            throw;
        }
    }
```

> Let op: gebruik dezelfde type-aliassen/namespaces als `CreateManualEnrollmentAsync` in dit bestand (bv. `OrganizationSettingsEntity`, `EnrollmentForm`, `FormResponse`, `FormResponseValueDto`, `EnrollmentEmails`, `DateOfBirthRules`, `SubmitEnrollmentRequest`). Kopieer de exacte vormen uit die methode.

- [ ] **Step 5: Endpoint**

Create `backend/CoachOS.API/Endpoints/LessonSerie/AddGroupMemberEndpoint.cs` (spiegelt `CreateManualEnrollmentEndpoint`):

```csharp
using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Enrollments;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.LessonSerie;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.LessonSerie;

public class AddGroupMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/enrollment-groups/{groupId:guid}/members",
            async (Guid id, Guid groupId, CreateManualEnrollmentRequest request, IEnrollmentService service,
                ILessonSerieService series, HttpContext ctx, CancellationToken ct) =>
            {
                Result access = await HeadTrainerAccess.EnsureSerieAccessAsync(ctx, series, id, ct);
                if (!access.IsSuccess) return access.ToErrorResult();
                Result write = HeadTrainerAccess.EnsureManualEnrollmentAllowed(ctx);
                if (!write.IsSuccess) return write.ToErrorResult();

                Result<Guid> result = await service.AddGroupMemberAsync(
                    id, groupId, request, ctx.GetOrganizationId(), ct);
                return result.IsSuccess
                    ? Results.Created($"/api/lessonseries/{id}/enrollments/{result.Value}", result.Value)
                    : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .AddEndpointFilter<ValidationFilter<CreateManualEnrollmentRequest>>()
        .WithTags("Enrollments");
    }
}
```

- [ ] **Step 6: Build + tests → groen**

Run: `cd backend && dotnet build CoachOS.slnx -clp:ErrorsOnly && dotnet test CoachOS.slnx --no-build --filter "FullyQualifiedName~EnrollmentServiceTests"`
Expected: PASS (incl. de 5 nieuwe tests).

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Application/Enrollments/IEnrollmentService.cs \
        backend/CoachOS.Application/Enrollments/EnrollmentService.cs \
        backend/CoachOS.API/Endpoints/LessonSerie/AddGroupMemberEndpoint.cs \
        backend/CoachOS.Tests/Services/EnrollmentServiceTests.cs
git commit -m "feat(enrollments): lid manueel toevoegen aan een bestaande groep (backend)"
```

---

## Task 2: Frontend — API + dialog (groep-modus) + knop op het groep-blok

**Files:**
- Modify: `frontend/lib/api/enrollments.ts`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/[id]/_components/manual-enrollment-dialog.tsx`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/[id]/_components/enrollments-table.tsx`
- Modify: `frontend/messages/nl.json`

**Interfaces:**
- Consumes: het nieuwe endpoint uit Task 1.
- Produces: `addGroupMember(seriesId, groupId, request)`; een `groupId`-modus op `ManualEnrollmentDialog`; een "lid toevoegen"-knop op `GroupBlockRows`.

- [ ] **Step 1: API-client**

In `frontend/lib/api/enrollments.ts`, naast `createManualEnrollment`:

```typescript
/** Voegt een lid toe aan een bestaande groep (erft status + prijsoptie van de groep). */
export async function addGroupMember(
  seriesId: string,
  groupId: string,
  request: CreateManualEnrollmentRequest,
): Promise<string> {
  const { data } = await apiClient.post<string>(
    `/lessonseries/${seriesId}/enrollment-groups/${groupId}/members`,
    request,
  );
  return data;
}
```

- [ ] **Step 2: `ManualEnrollmentDialog` — optionele groep-modus**

Breid `ManualEnrollmentDialog` uit met een optionele `groupId?: string`:
- Prop toevoegen: `{ seriesId, open, onOpenChange, groupId }`.
- Import `addGroupMember` aanvullen.
- `mutationFn`: als `groupId` gezet → `addGroupMember(seriesId, groupId, {...})`, anders de huidige `createManualEnrollment(...)`.
- `onSuccess`: bij groep-modus ook `["planning", seriesId]` invalideren en `toast.success(t("addMemberSuccess"))`; anders het bestaande gedrag (`t("manualSuccess")`).
- Titel/omschrijving: bij groep-modus `t("addMemberTitle")` / `t("addMemberDescription")`, anders `t("manualTitle")` / `t("manualDescription")`.
- Submit-knop: bij groep-modus `t("addMemberSubmit")`, anders `t("manualSubmit")`.

Behoud de bestaande velden (naam/e-mail/telefoon/geboortedatum); géén prijsoptie-veld.

- [ ] **Step 3: `GroupBlockRows` — knop + dialog**

In `enrollments-table.tsx`, in `GroupBlockRows`:
1. `const [addMemberOpen, setAddMemberOpen] = useState(false);`
2. Een "lid toevoegen"-actie (in het groep-acties-menu of als knopje bij de groepskop), gated op:
   `canManage && leader.status !== "Confirmed" && leader.status !== "PendingPayment"` (leider = `block.leader`), met label `t("addMember")`.
   Klik → `setAddMemberOpen(true)`.
3. Render de dialog in groep-modus:

```tsx
      <ManualEnrollmentDialog
        seriesId={seriesId}
        groupId={block.groupId}
        open={addMemberOpen}
        onOpenChange={setAddMemberOpen}
      />
```

(Import `ManualEnrollmentDialog` is er al in dit bestand? Zo niet, toevoegen.)

- [ ] **Step 4: nl.json-keys**

In `frontend/messages/nl.json`, in het `enrollmentsTable`-object:

```json
    "addMember": "Lid toevoegen",
    "addMemberTitle": "Lid toevoegen aan groep",
    "addMemberDescription": "De deelnemer wordt aan deze groep toegevoegd en erft de status en prijsoptie van de groep.",
    "addMemberSubmit": "Toevoegen",
    "addMemberSuccess": "Lid toegevoegd aan de groep"
```

- [ ] **Step 5: Typecheck + lint**

Run: `cd frontend && bunx tsc --noEmit && bunx eslint "app/(dashboard)/dashboard/lessons/[id]/_components/manual-enrollment-dialog.tsx" "app/(dashboard)/dashboard/lessons/[id]/_components/enrollments-table.tsx"`
Expected: geen NIEUWE fouten (bekende pre-existing `set-state-in-effect`-meldingen in enrollments-table tellen niet mee).

- [ ] **Step 6: Commit**

```bash
git add frontend/lib/api/enrollments.ts \
        "frontend/app/(dashboard)/dashboard/lessons/[id]/_components/manual-enrollment-dialog.tsx" \
        "frontend/app/(dashboard)/dashboard/lessons/[id]/_components/enrollments-table.tsx" \
        frontend/messages/nl.json
git commit -m "feat(enrollments): 'lid toevoegen' aan een groep vanuit de UI"
```

---

## Task 3: Verificatie — build, tests, reset + seed, live E2E

**Files:** geen.

- [ ] **Step 1: Volledige backend-suite** — `cd backend && dotnet test CoachOS.slnx` → alle groen.
- [ ] **Step 2: Frontend build** — `cd frontend && bun run build` → slaagt.
- [ ] **Step 3: Seed-scripts** — controleer of iets in `backend/Scripts/` het nieuwe endpoint nodig heeft (additief → wellicht niets).
- [ ] **Step 4: Reset + seed**

```bash
cd backend
bash Scripts/reset-db.sh --no-frontend
# wacht tot http://localhost:5142/health → 200
bash Scripts/seed-demo-data.sh
```

- [ ] **Step 5: Live E2E (API)** — admin-token, op een reeks met een `Pending`-groep:
  - POST `.../enrollment-groups/{groupId}/members` met een geldig lid → 201; GET enrollments toont het lid met `enrollmentGroupId` = groep, status = groepsstatus, en (indien de groep een prijsoptie had) de geërfde `selectedPriceOptionId`.
  - Op een `Confirmed` groep → 409.

---

## Self-Review

- **Spec-dekking:** status+optie erven (T1 Step 4), gate bij Confirmed/PendingPayment (T1), validaties hergebruikt (T1), geen mail (T1 + test Times.Never), knop verborgen bij betaalde groep (T2 Step 3), endpoint-scope admin+hoofdtrainer (T1 Step 5). ✓
- **Geen ctor-wijziging** → geen aanpassing aan andere testfixtures. ✓
- **Types consistent:** service `Result<Guid>`; endpoint `201`; FE `addGroupMember` → POST; body = bestaand `CreateManualEnrollmentRequest`-type. ✓
- **Geen placeholders:** volledige code/commando's per stap (met de expliciete instructie om type-aliassen uit `CreateManualEnrollmentAsync` te spiegelen). ✓
