# Lid uit een groep halen — Implementatieplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Admin/hoofdtrainer kan één lid uit een groep halen (wordt solo), vanuit het acties-menu van de ledenrij; met leider-promotie, ontbinden bij 1 lid (met behoud van de planningsplek), en blokkade bij betaald/bevestigd.

**Architecture:** Nieuwe `RemoveMemberFromGroupAsync` in `AssignmentService` (Planning) — naast het bestaande `DissolveGroupAsync`, hergebruikt dezelfde repos. Nieuw endpoint met admin+hoofdtrainer-scope. Geen schemawijziging. Frontend: actie + bevestiging in `PersonRow`.

**Tech Stack:** .NET 10 (service pattern, Result<T>), Next.js 15 + react-hook-form/next-intl, NUnit/Moq/FluentAssertions.

## Global Constraints

- Business-fouten via `Result<T>.Fail(...)`, nooit exceptions; service filtert op `organizationId`.
- Geen hardcoded Nederlandse strings op de FE — alles via `next-intl` (`messages/nl.json`).
- Geen `any` in TypeScript.
- Groep zakt naar 1 lid → ontbinden, laatste lid solo, planningsplek behouden.
- Leider verwijderen → vroegst-ingeschreven (`EnrolledAt`, tie-break `StudentName`) overblijvend lid wordt leider.
- Blokkeren (`Conflict`) als het lid `Confirmed`/`PendingPayment` is.
- ≥2 leden blijven → groeps-`ScheduleAssignment` ongemoeid.
- Autorisatie: admin + hoofdtrainer, zoals de cancel-endpoints op deze branch (`HeadTrainerAccess.EnsureSerieAccessAsync` + `EnsureManualEnrollmentAllowed`).
- **EF-tracking:** `ScheduleAssignmentRepository.GetBySeriesAsync` is `AsNoTracking` mét includes → verwijder toewijzingen via **key-only stubs** (`new ScheduleAssignment { Id = a.Id }`), nooit de include-dragende instances (anders "another instance with the same key is already tracked").

---

## Referentie (bestaande code)

`AssignmentService` ctor: `(ILessonSerieRepository lessonSeriesRepo, IEnrollmentRepository enrollmentRepo, IEnrollmentGroupRepository enrollmentGroupRepo, IScheduleAssignmentRepository scheduleAssignmentRepo)` — **ongewijzigd** (alle nodige repos zitten er al).

`EnrollmentGroup`: `Id`, `OrganizationId`, `LessonSerieId`, `Name`, `LeaderEnrollmentId`, `ICollection<Enrollment> Members`.
`Enrollment` (lid): `Id`, `Status` (`EnrollmentStatus`), `EnrolledAt` (DateTime), `EnrollmentGroupId`.
`ScheduleAssignment`: `Id`, `OrganizationId`, `LessonSerieId`, `WeeklyTemplateEntryId`, `EnrollmentGroupId?`, `EnrollmentId?`, `Status`, `IsAutoMerged`, `IsLocked`.
`IScheduleAssignmentRepository`: `GetBySeriesAsync` (AsNoTracking), `AddRangeAsync`, `RemoveRange`, `SaveChangesAsync`.
`IEnrollmentGroupRepository`: `GetByIdAsync` (tracked, laadt Members), `Delete`, `SaveChangesAsync`.
`DissolveGroupAsync` (bestaand, ter referentie): zet elk lid `EnrollmentGroupId=null`, `scheduleAssignmentRepo.RemoveRange(groupAssignments)`, `enrollmentGroupRepo.Delete(group)`, `SaveChangesAsync`.
`HeadTrainerAccess.EnsureManualEnrollmentAllowed(ctx)`: Ok bij admin of ≥1 hoofdtrainer-club, anders `Forbidden`.

---

## Task 1: Backend — service + interface + endpoint + unit tests

**Files:**
- Modify: `backend/CoachOS.Application/Planning/IAssignmentService.cs`
- Modify: `backend/CoachOS.Application/Planning/AssignmentService.cs`
- Create: `backend/CoachOS.API/Endpoints/LessonSerie/RemoveGroupMemberEndpoint.cs`
- Modify: `backend/CoachOS.Tests/Services/AssignmentServiceTests.cs`

**Interfaces:**
- Produces: `IAssignmentService.RemoveMemberFromGroupAsync(Guid seriesId, Guid groupId, Guid enrollmentId, Guid organizationId, CancellationToken ct = default) → Task<Result<bool>>`.
- Endpoint: `DELETE /lessonseries/{id}/enrollment-groups/{groupId}/members/{enrollmentId}` → `204`.

- [ ] **Step 1: Schrijf de falende tests**

Voeg tests toe aan `AssignmentServiceTests.cs` (fixture heeft al `_seriesRepo`, `_enrollmentRepo`, `_groupRepo`, `_assignmentRepo` en `_service = new AssignmentService(...)`). Gebruik constanten `OrgId`/`SeriesId` zoals in de fixture (of definieer lokaal wanneer afwezig). Helper om een groep te bouwen:

```csharp
private (EnrollmentGroup Group, Enrollment Leader, List<Enrollment> Members) BuildGroup(
    int size, EnrollmentStatus status = EnrollmentStatus.Pending)
{
    Guid groupId = Guid.NewGuid();
    List<Enrollment> members = [];
    for (int i = 0; i < size; i++)
    {
        members.Add(new Enrollment
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId,
            StudentName = $"Lid {i}", EnrolledAt = new DateTime(2026, 1, 1).AddDays(i),
            Status = status, EnrollmentGroupId = groupId,
        });
    }
    EnrollmentGroup group = new()
    {
        Id = groupId, OrganizationId = OrgId, LessonSerieId = SeriesId,
        Name = "Groep A", LeaderEnrollmentId = members[0].Id, Members = members,
    };
    _groupRepo.Setup(r => r.GetByIdAsync(groupId, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(group);
    _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<ScheduleAssignment>());
    return (group, members[0], members);
}

[Test]
public async Task RemoveMember_GroupOf3_RegularMember_DetachesOnly()
{
    var (group, leader, members) = BuildGroup(3);
    Enrollment target = members[2]; // geen leider

    Result<bool> result = await _service.RemoveMemberFromGroupAsync(
        SeriesId, group.Id, target.Id, OrgId, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    target.EnrollmentGroupId.Should().BeNull();
    group.LeaderEnrollmentId.Should().Be(leader.Id);           // leider onveranderd
    _groupRepo.Verify(r => r.Delete(It.IsAny<EnrollmentGroup>()), Times.Never); // niet ontbonden
    _assignmentRepo.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<ScheduleAssignment>>()), Times.Never);
    _groupRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}

[Test]
public async Task RemoveMember_GroupOf3_Leader_PromotesEarliestEnrolled()
{
    var (group, leader, members) = BuildGroup(3);
    // members[1] is vroeger ingeschreven dan members[2] (EnrolledAt oplopend) -> die wordt leider

    Result<bool> result = await _service.RemoveMemberFromGroupAsync(
        SeriesId, group.Id, leader.Id, OrgId, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    leader.EnrollmentGroupId.Should().BeNull();
    group.LeaderEnrollmentId.Should().Be(members[1].Id);
    _groupRepo.Verify(r => r.Delete(It.IsAny<EnrollmentGroup>()), Times.Never);
}

[Test]
public async Task RemoveMember_GroupOf2_Dissolves_AndConvertsAssignmentToRemainingMember()
{
    var (group, leader, members) = BuildGroup(2);
    Enrollment remaining = members[1];
    // groep is ingepland: één groeps-toewijzing (Proposed)
    ScheduleAssignment groupAssignment = new()
    {
        Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId,
        WeeklyTemplateEntryId = Guid.NewGuid(), EnrollmentGroupId = group.Id,
        Status = ScheduleAssignmentStatus.Proposed, IsLocked = false,
    };
    _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<ScheduleAssignment> { groupAssignment });

    List<ScheduleAssignment>? added = null;
    _assignmentRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleAssignment>>(), It.IsAny<CancellationToken>()))
        .Callback<IEnumerable<ScheduleAssignment>, CancellationToken>((a, _) => added = a.ToList())
        .Returns(Task.CompletedTask);

    Result<bool> result = await _service.RemoveMemberFromGroupAsync(
        SeriesId, group.Id, leader.Id, OrgId, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    remaining.EnrollmentGroupId.Should().BeNull();                       // laatste lid wordt solo
    _groupRepo.Verify(r => r.Delete(group), Times.Once);                 // groep ontbonden
    _assignmentRepo.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<ScheduleAssignment>>()), Times.Once);
    added.Should().NotBeNull();
    added!.Should().ContainSingle();
    added![0].EnrollmentId.Should().Be(remaining.Id);                    // plek behouden als individueel
    added![0].EnrollmentGroupId.Should().BeNull();
    added![0].WeeklyTemplateEntryId.Should().Be(groupAssignment.WeeklyTemplateEntryId);
    added![0].Status.Should().Be(ScheduleAssignmentStatus.Proposed);
}

[Test]
public async Task RemoveMember_Confirmed_ReturnsConflict()
{
    var (group, leader, members) = BuildGroup(3, EnrollmentStatus.Confirmed);

    Result<bool> result = await _service.RemoveMemberFromGroupAsync(
        SeriesId, group.Id, members[2].Id, OrgId, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
    members[2].EnrollmentGroupId.Should().Be(group.Id);                 // niets gemuteerd
    _groupRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
}

[Test]
public async Task RemoveMember_NotAMember_ReturnsNotFound()
{
    var (group, _, _) = BuildGroup(3);

    Result<bool> result = await _service.RemoveMemberFromGroupAsync(
        SeriesId, group.Id, Guid.NewGuid(), OrgId, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
}
```

(Voeg ontbrekende `using`-regels toe: `CoachOS.Domain.Entities`, `CoachOS.Domain.Enums`, `CoachOS.Domain.Models`.)

- [ ] **Step 2: Run de tests → falen (methode bestaat niet)**

Run: `cd backend && dotnet build CoachOS.slnx -clp:ErrorsOnly`
Expected: FAIL — `IAssignmentService` / `AssignmentService` heeft geen `RemoveMemberFromGroupAsync`.

- [ ] **Step 3: Voeg de methode toe aan de interface**

In `IAssignmentService.cs`, na `DissolveGroupAsync`:

```csharp
    Task<Result<bool>> RemoveMemberFromGroupAsync(
        Guid seriesId, Guid groupId, Guid enrollmentId, Guid organizationId, CancellationToken ct = default);
```

- [ ] **Step 4: Implementeer `RemoveMemberFromGroupAsync` in `AssignmentService`**

Voeg toe (naast `DissolveGroupAsync`). Gebruik `using CoachOS.Domain.Enums;` indien nodig voor `EnrollmentStatus`/`ScheduleAssignmentStatus`.

```csharp
    public async Task<Result<bool>> RemoveMemberFromGroupAsync(
        Guid seriesId, Guid groupId, Guid enrollmentId, Guid organizationId, CancellationToken ct = default)
    {
        EnrollmentGroup? group = await enrollmentGroupRepo.GetByIdAsync(groupId, organizationId, ct);
        if (group is null || group.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

        Enrollment? member = group.Members.FirstOrDefault(m => m.Id == enrollmentId);
        if (member is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Dit lid zit niet in deze groep."));

        // Gate: een betaalde/bevestigde groep niet meer herschikken (de groep deelt de status).
        if (member.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.PendingPayment)
            return Result<bool>.Fail(new Error(ErrorCodes.Conflict,
                "Dit lid kan niet uit de groep gehaald worden: de groep is al betaald of bevestigd."));

        // Detach het lid → wordt een losse (solo) inschrijving.
        member.EnrollmentGroupId = null;

        List<Enrollment> remaining = group.Members.Where(m => m.Id != enrollmentId).ToList();

        if (remaining.Count <= 1)
        {
            // Ontbinden: laatste lid (indien er één is) wordt ook solo, en behoudt z'n planningsplek
            // doordat de groeps-toewijzing omgezet wordt naar een individuele toewijzing.
            Enrollment? last = remaining.FirstOrDefault();
            if (last is not null)
                last.EnrollmentGroupId = null;

            List<ScheduleAssignment> groupAssignments =
                (await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct))
                .Where(a => a.EnrollmentGroupId == groupId)
                .ToList();

            if (groupAssignments.Count > 0)
            {
                // Verwijder via key-only stubs (GetBySeriesAsync is AsNoTracking mét includes:
                // de include-dragende instances zouden botsen met de getrackte group/members).
                scheduleAssignmentRepo.RemoveRange(
                    groupAssignments.Select(a => new ScheduleAssignment { Id = a.Id }).ToList());

                if (last is not null)
                {
                    // Zet elke groeps-toewijzing om naar een individuele toewijzing voor het overblijvende lid.
                    List<ScheduleAssignment> individual = groupAssignments.Select(a => new ScheduleAssignment
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = a.OrganizationId,
                        LessonSerieId = a.LessonSerieId,
                        WeeklyTemplateEntryId = a.WeeklyTemplateEntryId,
                        EnrollmentGroupId = null,
                        EnrollmentId = last.Id,
                        Status = a.Status,
                        IsAutoMerged = false,
                        IsLocked = a.IsLocked,
                    }).ToList();
                    await scheduleAssignmentRepo.AddRangeAsync(individual, ct);
                }
            }

            enrollmentGroupRepo.Delete(group);
        }
        else
        {
            // ≥2 leden blijven: groeps-toewijzing ongemoeid (het lid valt er automatisch uit).
            // Was het verwijderde lid de leider, promoveer het vroegst-ingeschreven overblijvende lid.
            if (group.LeaderEnrollmentId == enrollmentId)
            {
                Enrollment newLeader = remaining
                    .OrderBy(m => m.EnrolledAt)
                    .ThenBy(m => m.StudentName)
                    .First();
                group.LeaderEnrollmentId = newLeader.Id;
            }
        }

        await enrollmentGroupRepo.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
```

- [ ] **Step 5: Voeg het endpoint toe**

Create `backend/CoachOS.API/Endpoints/LessonSerie/RemoveGroupMemberEndpoint.cs` (spiegelt de scope van `CancelEnrollmentGroupEndpoint`):

```csharp
using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.Planning;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.LessonSerie;

public class RemoveGroupMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{id:guid}/enrollment-groups/{groupId:guid}/members/{enrollmentId:guid}",
            async (Guid id, Guid groupId, Guid enrollmentId, IAssignmentService service,
                ILessonSerieService series, HttpContext ctx, CancellationToken ct) =>
            {
                Result access = await HeadTrainerAccess.EnsureSerieAccessAsync(ctx, series, id, ct);
                if (!access.IsSuccess) return access.ToErrorResult();
                Result writeAccess = HeadTrainerAccess.EnsureManualEnrollmentAllowed(ctx);
                if (!writeAccess.IsSuccess) return writeAccess.ToErrorResult();

                var result = await service.RemoveMemberFromGroupAsync(
                    id, groupId, enrollmentId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Enrollments");
    }
}
```

- [ ] **Step 6: Build + run de tests → groen**

Run: `cd backend && dotnet build CoachOS.slnx -clp:ErrorsOnly && dotnet test CoachOS.slnx --no-build --filter "FullyQualifiedName~AssignmentService"`
Expected: PASS (incl. de 5 nieuwe tests).

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Application/Planning/IAssignmentService.cs \
        backend/CoachOS.Application/Planning/AssignmentService.cs \
        backend/CoachOS.API/Endpoints/LessonSerie/RemoveGroupMemberEndpoint.cs \
        backend/CoachOS.Tests/Services/AssignmentServiceTests.cs
git commit -m "feat(enrollments): lid uit een groep halen (leider-promotie, ontbinden, gate)"
```

---

## Task 2: Frontend — actie + bevestiging in de ledenrij

**Files:**
- Modify: `frontend/lib/api/enrollments.ts`
- Modify: `frontend/app/(dashboard)/dashboard/lessons/[id]/_components/enrollments-table.tsx`
- Modify: `frontend/messages/nl.json`

**Interfaces:**
- Consumes: het nieuwe endpoint uit Task 1.
- Produces: `removeGroupMember(seriesId, groupId, enrollmentId)` en een "Uit groep halen"-actie in `PersonRow`.

- [ ] **Step 1: API-client**

In `frontend/lib/api/enrollments.ts`, naast `cancelEnrollmentGroup`:

```typescript
export async function removeGroupMember(
  seriesId: string,
  groupId: string,
  enrollmentId: string,
): Promise<void> {
  await apiClient.delete(
    `/lessonseries/${seriesId}/enrollment-groups/${groupId}/members/${enrollmentId}`,
  );
}
```

- [ ] **Step 2: `PersonRow` — mutation + confirm-state**

In `enrollments-table.tsx`, in `PersonRow`:
1. Import aanvullen: voeg `removeGroupMember` toe aan de bestaande import uit `@/lib/api/enrollments`. Voeg een icoon toe (bv. `UserMinus`) aan de bestaande `lucide-react`-import.
2. Naast `cancelMutation`, een mutation + confirm-state:

```typescript
  const [confirmRemoveOpen, setConfirmRemoveOpen] = useState(false);
  const removeMemberMutation = useMutation({
    mutationFn: () =>
      removeGroupMember(seriesId, enrollment.enrollmentGroupId!, enrollment.id),
    onSuccess: () => {
      toast.success(t("removeFromGroupSuccess"));
      queryClient.invalidateQueries({ queryKey: ["enrollments", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["planning", seriesId] });
      queryClient.invalidateQueries({ queryKey: ["lessonSeries", seriesId] });
    },
    onError: () => toast.error(t("removeFromGroupError")),
  });
```

- [ ] **Step 3: Menu-actie (enkel bij een groepslid)**

In het acties-menu van `PersonRow`, na de "Annuleren"-knop (binnen dezelfde `canManage && !isCancelled`-context), enkel tonen bij een groepslid:

```tsx
                {canManage && !isCancelled && enrollment.enrollmentGroupId && (
                  <button
                    type="button"
                    disabled={removeMemberMutation.isPending}
                    onClick={() => {
                      setOpenMenuId(null);
                      setConfirmRemoveOpen(true);
                    }}
                    className="flex w-full items-center gap-2 px-3 py-2 text-left text-gray-700 hover:bg-tennis-green/5 hover:text-tennis-green disabled:opacity-50"
                  >
                    <UserMinus size={13} />
                    {t("removeFromGroup")}
                  </button>
                )}
```

- [ ] **Step 4: Bevestigingsdialog**

Naast de bestaande cancel-`AlertDialog` in `PersonRow`, een tweede:

```tsx
      <AlertDialog open={confirmRemoveOpen} onOpenChange={setConfirmRemoveOpen}>
        <AlertDialogContent onClick={(e) => e.stopPropagation()}>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("removeFromGroupTitle")}</AlertDialogTitle>
            <AlertDialogDescription>
              {t("removeFromGroupBody", { name: enrollment.studentName })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("back")}</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => removeMemberMutation.mutate()}
              className="bg-tennis-green hover:bg-tennis-green/90"
            >
              {t("removeFromGroupConfirm")}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
```

(Hergebruik de bestaande `back`-key indien aanwezig in `enrollmentsTable`; zo niet, voeg `"back": "Terug"` toe in Step 5.)

- [ ] **Step 5: nl.json-keys**

In `frontend/messages/nl.json`, in het `enrollmentsTable`-object:

```json
    "removeFromGroup": "Uit groep halen",
    "removeFromGroupTitle": "Uit groep halen?",
    "removeFromGroupBody": "{name} wordt uit de groep gehaald en wordt een losse inschrijving.",
    "removeFromGroupConfirm": "Uit groep halen",
    "removeFromGroupSuccess": "Lid uit de groep gehaald",
    "removeFromGroupError": "Kon het lid niet uit de groep halen"
```

- [ ] **Step 6: Typecheck + lint**

Run: `cd frontend && bunx tsc --noEmit && bunx eslint "app/(dashboard)/dashboard/lessons/[id]/_components/enrollments-table.tsx"`
Expected: geen NIEUWE fouten (bekende pre-existing `set-state-in-effect`-meldingen in dit bestand tellen niet mee).

- [ ] **Step 7: Commit**

```bash
git add frontend/lib/api/enrollments.ts \
        "frontend/app/(dashboard)/dashboard/lessons/[id]/_components/enrollments-table.tsx" \
        frontend/messages/nl.json
git commit -m "feat(enrollments): 'Uit groep halen' actie in de ledenrij"
```

---

## Task 3: Verificatie — build, tests, reset + seed, live E2E

**Files:** geen.

- [ ] **Step 1: Volledige backend-suite** — `cd backend && dotnet test CoachOS.slnx` → alle groen.
- [ ] **Step 2: Frontend build** — `cd frontend && bun run build` → slaagt.
- [ ] **Step 3: Seed-scripts** — controleer of iets in `backend/Scripts/` de nieuwe route nodig heeft (additief endpoint → wellicht niets).
- [ ] **Step 4: Reset + seed**

```bash
cd backend
bash Scripts/reset-db.sh --no-frontend
# wacht tot http://localhost:5142/health → 200
bash Scripts/seed-demo-data.sh
```

- [ ] **Step 5: Live E2E (API)** — met de admin-token op een reeks met een groep:
  - groep ≥3, gewoon lid verwijderen → 204; lid weg uit groep, groep blijft.
  - groep-leider verwijderen → 204; nieuwe leider gezet.
  - groep van 2, lid verwijderen → 204; groep weg, overblijvend lid solo (en, indien ingepland, behoudt toewijzing).
  - een `Confirmed` groep → 409.

---

## Self-Review

- **Spec-dekking:** leider-promotie (T1 §else), ontbinden→solo+behoud plek (T1 §remaining≤1 + AddRange), gate (T1), ≥2 ongemoeid (T1 §else, geen assignment-calls), endpoint-scope admin+hoofdtrainer (T1 §5), FE actie enkel bij groepslid (T2 §3). ✓
- **EF-tracking gotcha** expliciet afgedekt via key-only stubs (Global Constraints + T1 §4). ✓
- **Types consistent:** service `Result<bool>`; endpoint `204`; FE `removeGroupMember` → `DELETE`. `enrollment.enrollmentGroupId` (string|null) al aanwezig op de FE-DTO. ✓
- **Geen placeholders:** alle stappen bevatten concrete code/commando's. ✓
