# Plan — Bug 1 (duplicate scheduling) + Bug 2 (off-by-one dag in mail)

**Datum:** 2026-05-06
**Branch:** `fix/scheduling-duplicates-and-day-name`
**Status:** Plan, nog niet uitgevoerd
**Context:** [Sessie 2026-05-06](../../../) — twee bugs ontdekt in production tijdens manuele test van trainer-uitnodiging-flow.

---

## Bugs samengevat

### Bug 1 — Dubbele toewijzing
Wanneer een leerling de planning bevestigt en admin daarna nog eens "Bevestig planning" klikt (of de UI roept onbedoeld `GenerateProposalAsync` opnieuw), komt dezelfde leerling **dubbel** in hetzelfde tijdslot te staan.

**Root cause:**
1. `PlanningService.GenerateProposalAsync` regel 27 — guard checkt alleen `PlanningStatus.Planning` en laat de regenereer-flow doorgaan in `AwaitingConfirmation` / `Scheduled` states.
2. `RemoveProposedBySeriesAsync` cleant alleen `Proposed` assignments, niet `AwaitingConfirmation` of `Confirmed`. Resultaat: oude bevestigde rij blijft staan, nieuwe Proposed rij komt erbij.
3. `ScheduleAssignmentConfiguration` heeft geen unique constraint op `(LessonSerieId, WeeklyTemplateEntryId, EnrollmentId)` of `(…, EnrollmentGroupId)`. DB-niveau zou de duplicate hebben afgewezen.

### Bug 2 — Off-by-one dag in mail
Leerling ingepland op dinsdag, mail zegt "maandag". Eerste leerling kreeg "toevallig" goed.

**Root cause:** `EmailService.cs` regel 16-17 — `DaysNl = ["zondag", "maandag", "dinsdag", …]` (= .NET-conventie, 0=zondag). Maar `WeeklyTemplateEntry.DayOfWeek` wordt opgeslagen in EU-conventie (0=maandag) — zo staat 't ook in `LessonSerieService.cs:107` `dayNames = ["ma", "di", "wo", …]`. `SendScheduleConfirmationAsync` indexeert `DaysNl[dayOfWeek]` zonder conversie → off-by-one.

---

## Constraint-keuze (belangrijk!)

Eén leerling kan **wél** meerdere slots binnen dezelfde reeks volgen ("ma + di in dezelfde week" of "2× achter elkaar op dinsdag" — verschillende `WeeklyTemplateEntry`'s). Daarom is de unique key:

```
(LessonSerieId, WeeklyTemplateEntryId, EnrollmentId)
(LessonSerieId, WeeklyTemplateEntryId, EnrollmentGroupId)
```

Niet `(LessonSerieId, EnrollmentId)` — dat zou de bovenstaande use-case kapotmaken.

---

## A. Bug 2 fix (klein, los te testen)

### A.1 Code-fix in `EmailService.cs:53`

**Vóór:**
```csharp
var dayName = DaysNl[Math.Clamp(dayOfWeek, 0, 6)];
```

**Na:**
```csharp
// dayOfWeek arriveert in EU-conventie (0 = maandag, 6 = zondag) vanuit
// WeeklyTemplateEntry. DaysNl is .NET-conventie (0 = zondag). Converteren
// voor we indexeren — anders krijg je dinsdag → maandag in de mail.
int safeEu = Math.Clamp(dayOfWeek, 0, 6);
int netIndex = (safeEu + 1) % 7;
var dayName = DaysNl[netIndex];
```

De andere `DaysNl[(int)X.DayOfWeek]` calls in dit bestand (regels 129, 156, 157, 200) gebruiken `DateTime.DayOfWeek` rechtstreeks — dat IS .NET-conventie, dus die zijn correct en blijven ongemoeid.

### A.2 Test toevoegen

**Locatie:** `backend/CoachOS.Tests/Services/EmailServiceTests.cs` (verifieer of bestand bestaat — zo niet, maak nieuw met xUnit + NSubstitute + FluentAssertions per `~/.claude/rules/dotnet/testing.md`).

**Pattern:** Substitute voor `IMjmlTemplateRenderer`, capture de `Dictionary<string, string>` die naar `Render` gaat, assert dat `["dayName"]` correct is voor alle 7 dagen.

```csharp
[Theory]
[InlineData(0, "maandag")]    // EU 0
[InlineData(1, "dinsdag")]    // EU 1 — de bug-case
[InlineData(2, "woensdag")]
[InlineData(3, "donderdag")]
[InlineData(4, "vrijdag")]
[InlineData(5, "zaterdag")]
[InlineData(6, "zondag")]     // EU 6
public async Task SendScheduleConfirmation_RendersCorrectDutchDayName(int euDay, string expectedDayName)
{
    Dictionary<string, string>? captured = null;
    _renderer.Render("schedule-confirmation", Arg.Do<Dictionary<string, string>>(d => captured = d))
             .Returns("<html/>");

    await _sut.SendScheduleConfirmationAsync(
        "a@b.be", "Anna", "Tennisreeks 1", euDay, "18:00", "19:00", null, "https://x", default);

    captured.Should().NotBeNull();
    captured!["dayName"].Should().Be(expectedDayName);
}
```

---

## B. Bug 1 fix (groter, vereist EF migration)

### B.1 Vóór code-changes: verifieer wat de UI aanroept

```bash
grep -rn "GenerateProposalAsync\|generateProposal\|/planning/generate\|/planning/confirm" frontend/lib/ frontend/app/
```

Als de UI op page-refresh van de planningspagina automatisch `generate` aanroept → **dat is de trigger** waardoor de bug ook ongewild kan opduiken zonder dat admin explicit klikt. Documenteer in PR-body. Frontend-fix valt evt. in een aparte mini-PR; backend-vangrails uit dit plan dekken het sowieso.

### B.2 Verifieer `PlanningStatus` enum-waarden

```bash
grep -rn "enum PlanningStatus" backend/CoachOS.Domain/
```

Bevestig dat de states zijn: `Draft` (of `0` default), `Planning`, `AwaitingConfirmation`, `Scheduled`. Als `Draft` afwijkt of niet bestaat, pas de guard in B.3 aan.

### B.3 Laag 1 — strengere guard in `PlanningService.cs:27`

**Bestand:** `backend/CoachOS.Application/Planning/PlanningService.cs`

**Vóór:**
```csharp
if (series.PlanningStatus == PlanningStatus.Planning && !force)
    return await GetPlanningOverviewAsync(seriesId, organizationId, ct);
```

**Na:**
```csharp
// Hergenereren is alleen automatisch toegestaan vóór de eerste bevestigingsronde.
// Zodra de planning naar AwaitingConfirmation/Scheduled is gegaan, kan een
// onbedoelde call (UI refresh, dubbele klik, race) niet meer per ongeluk de
// planning hergenereren — dat zou bestaande Confirmed assignments dupliceren.
// Admin kan nog steeds expliciet hergeneren via force=true.
if (series.PlanningStatus is PlanningStatus.Planning
                          or PlanningStatus.AwaitingConfirmation
                          or PlanningStatus.Scheduled
    && !force)
{
    return await GetPlanningOverviewAsync(seriesId, organizationId, ct);
}
```

### B.4 Laag 2 — bestaande non-Proposed assignments locken (regels 47-48)

**Bestand:** `backend/CoachOS.Application/Planning/PlanningService.cs`

**Vóór:**
```csharp
var lockedAssignments = existingAssignments
    .Where(a => a.IsLocked && a.Status == ScheduleAssignmentStatus.Proposed)
    .ToList();
```

**Na:**
```csharp
// Lock = "deze plek staat vast, niet aanraken bij hergenerate":
//   1. Manueel locked Proposed assignments (admin heeft slot vastgezet)
//   2. AwaitingConfirmation / Confirmed assignments — deze zijn al verstuurd
//      naar leerlingen of betaald; opnieuw inplannen veroorzaakt duplicates.
var lockedAssignments = existingAssignments
    .Where(a =>
        (a.IsLocked && a.Status == ScheduleAssignmentStatus.Proposed)
        || a.Status == ScheduleAssignmentStatus.AwaitingConfirmation
        || a.Status == ScheduleAssignmentStatus.Confirmed)
    .ToList();
```

**Belangrijk:** regel 79 (`RemoveProposedBySeriesAsync`) hoeft niet aangepast — die mag `Proposed` blijven cleanen. Onze nieuwe lock-set zorgt ervoor dat het algoritme **geen nieuwe Proposed** maakt voor mensen die al `AwaitingConfirmation`/`Confirmed` zijn, dus de oude rijen blijven correct staan.

### B.5 Laag 3 — Configuration + EF migration met wipe

**Stap 1**: aanpassen `backend/CoachOS.Infrastructure/Persistence/Configurations/ScheduleAssignmentConfiguration.cs`. Toevoegen onderaan de bestaande `HasIndex` regels:

```csharp
// Voorkom dat dezelfde leerling 2× op hetzelfde slot binnen dezelfde reeks
// terechtkomt. (1 leerling kan wél meerdere DIFFERENT slots hebben — daarom
// is WeeklyTemplateEntryId onderdeel van de key.)
builder.HasIndex(a => new { a.LessonSerieId, a.WeeklyTemplateEntryId, a.EnrollmentId })
    .IsUnique()
    .HasFilter("\"EnrollmentId\" IS NOT NULL");

builder.HasIndex(a => new { a.LessonSerieId, a.WeeklyTemplateEntryId, a.EnrollmentGroupId })
    .IsUnique()
    .HasFilter("\"EnrollmentGroupId\" IS NOT NULL");
```

**Stap 2**: migration genereren.

```bash
cd backend
dotnet ef migrations add AddScheduleAssignmentUniqueConstraints \
  --project CoachOS.Infrastructure --startup-project CoachOS.API
```

**Stap 3**: wipe-SQL toevoegen aan de gegenereerde migration. EF zal `CreateIndex`-calls in `Up()` zetten — voeg **bovenaan** `Up()` toe (vóór de `CreateIndex`):

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Productie heeft testdata-duplicates die de unique indexes hieronder
    // zouden afwijzen. MVP-fase: geen echte users, alle scheduling-data is
    // wegwerpbaar. Lokaal/CI: no-op want lege DB.
    // Volgorde: tokens eerst (FK naar ScheduleAssignments).
    migrationBuilder.Sql(@"DELETE FROM ""AssignmentConfirmationTokens"";");
    migrationBuilder.Sql(@"DELETE FROM ""ScheduleAssignments"";");

    // [bestaande gegenereerde CreateIndex calls hieronder laten staan]
    migrationBuilder.CreateIndex(
        name: "IX_ScheduleAssignments_LessonSerieId_WeeklyTemplateEntryId_EnrollmentId",
        ...);
    // ...
}
```

**`Down()` ongemoeid laten** (alleen `DropIndex`-calls). Geen poging tot data-restore — onmogelijk.

### B.6 Tests voor PlanningService

**Locatie:** `backend/CoachOS.Tests/Services/PlanningServiceTests.cs` (verifieer of bestaat).

```csharp
[Fact]
public async Task GenerateProposalAsync_AwaitingConfirmation_ReturnsExistingOverview()
{
    // Arrange: series in AwaitingConfirmation, force=false
    // Act: GenerateProposalAsync(force: false)
    // Assert:
    //   - scheduleAssignmentRepo.RemoveProposedBySeriesAsync NEVER called
    //   - scheduleAssignmentRepo.AddRangeAsync NEVER called
}

[Fact]
public async Task GenerateProposalAsync_Scheduled_ReturnsExistingOverview()
{
    // Idem voor PlanningStatus.Scheduled
}

[Fact]
public async Task GenerateProposalAsync_ForceWithConfirmedAssignment_DoesNotDuplicateConfirmedEnrollment()
{
    // Arrange:
    //   - Series in AwaitingConfirmation (force=true scenario)
    //   - Anna heeft een Confirmed ScheduleAssignment op slot 1
    //   - Bart heeft alleen een Pending Enrollment, geen assignment
    // Act: GenerateProposalAsync(force: true)
    // Assert:
    //   - Anna's Confirmed assignment blijft staan (niet verwijderd)
    //   - Algoritme kreeg Bart maar NIET Anna in zijn input
    //   - Geen nieuwe assignment voor Anna gemaakt
    //   - Wél een nieuwe Proposed voor Bart
}
```

### B.7 Reset-test (verplicht — `feedback_reset_is_definitive_test`)

```bash
cd backend
bash Scripts/reset-db.sh --no-frontend
# Wacht tot http://localhost:5142/health → 200
bash Scripts/seed-demo-data.sh
```

Als seed faalt op een unique-violation → seed-data of algoritme creëert duplicates die niet zouden mogen. Fix nodig vóór PR (kan zijn in seed-script óf in algoritme).

---

## C. Volgorde van uitvoeren

1. **Branch maken**:
   ```bash
   git checkout main && git pull --ff-only
   git checkout -b fix/scheduling-duplicates-and-day-name
   ```

2. **Bug 2** (klein, los):
   - A.1 (`EmailService.cs:53`)
   - A.2 (test)
   - Build + test:
     ```bash
     cd backend && dotnet test CoachOS.slnx --filter "FullyQualifiedName~EmailServiceTests"
     ```

3. **Bug 1** stap voor stap:
   - B.1 + B.2 (verificatie, alleen lezen, evt. notes voor PR-body)
   - B.3 (guard)
   - B.4 (locked-uitbreiding)
   - B.5 (configuration + `dotnet ef migrations add` + wipe-SQL toevoegen)
   - B.6 (tests)

4. **Build + alle tests**:
   ```bash
   cd backend && dotnet build CoachOS.slnx && dotnet test CoachOS.slnx
   ```

5. **Reset-test** (B.7).

6. **Commit + push** via git-commit subagent (Haiku):
   ```
   commit-message:
     fix(planning): prevent duplicate assignments + correct day-of-week in confirmation email

   body:
     Bug 1 — re-running planning generation while a series is in
     AwaitingConfirmation or Scheduled state produced duplicate assignments
     for already-confirmed students. Tightens the guard so non-Draft/Planning
     states only regenerate with explicit force=true, and treats existing
     AwaitingConfirmation/Confirmed assignments as locks so the algorithm
     can no longer re-emit them.

     Adds unique partial indexes on (LessonSerieId, WeeklyTemplateEntryId,
     EnrollmentId) and on (..., EnrollmentGroupId) as defense-in-depth.
     Migration wipes existing scheduling data — MVP-only, prod has no real
     users yet.

     Bug 2 — confirmation email rendered the wrong day-of-week (e.g.
     Tuesday → "maandag") because EmailService.DaysNl is .NET-indexed
     (0=Sunday) but WeeklyTemplateEntry.DayOfWeek arrives EU-indexed
     (0=Monday). Convert at the call site.
   ```

7. **PR openen** richting `main`.

8. **Mergen** → triggert `backend-build-push.yml`:
   - Build → push naar registry
   - Migrate-stap draait `dotnet ef database update` op productie-DB → wipet `ScheduleAssignments` + `AssignmentConfirmationTokens`, voegt unique indexes toe
   - SSH pull-and-restart container

9. **Verificatie post-deploy** (handmatig in app):
   - Maak testreeks, nodig 2 leerlingen uit, bevestig planning, laat 1 bevestigen, klik "Bevestig planning" opnieuw → mag GEEN dubbele toewijzing geven
   - Check één bevestigingsmail: dag moet kloppen (dinsdag = "dinsdag", niet "maandag")

---

## D. Wat bewust NIET in deze fix zit

- **Frontend-fix als de UI onnodig `GenerateProposalAsync` aanroept** — als B.1 dat aantoont, opent dat een aparte mini-PR. Dit plan dekt de backend-vangrails ook als de UI fout blijft.
- **Idempotency-token op `ConfirmScheduleAsync`** — `series.PlanningStatus != Planning` guard in `ConfirmationOrchestrationService:30-32` is daar al voldoende.
- **Audit logging van wie de regenerate heeft getriggerd** — los issue, niet voor nu.

---

## E. Risico's & rollback

- **Migration faalt op prod** (bv. nog FK-references die ik mis): backend container blijft op vorige versie staan want auto-migrate is at startup, container start gewoon niet. Geen data-corruptie. Fix: rollback PR, fix migration, redeploy.
- **Unique constraint te streng**: als er een legitieme use-case is die ik mis (bv. "vervang-een-Declined-met-een-nieuwe-Proposed-voor-dezelfde-persoon-zelfde-slot"), dan crasht de save. Mitigatie: tests in B.6 zijn er om dit op te vangen. Snelle hotfix: constraint relaxeren naar partial-met-extra-Status-filter (bv. alleen uniek over `Status IN (Proposed, AwaitingConfirmation, Confirmed)`).
- **B.1 toont UI-bug**: dan is na deploy de duplicate weg, maar de UI doet onverwachte calls. Backend-guards blokkeren ze stil. Frontend-PR plannen.

---

## F. Memory-relevante regels die hier spelen

- `feedback_reset_is_definitive_test.md` — sectie B.7 + reset draaien vóór PR.
- `feedback_migrations_update_seed.md` — controleer of `seed-demo-data.*` of `setup.sh` ergens duplicates creëert die door de nieuwe constraint worden afgewezen.
- `feedback_tests_for_every_feature.md` — tests in A.2 + B.6 zijn verplicht, niet optioneel.
- `feedback_create_branch_before_work.md` — sectie C stap 1.
- `feedback_delegate_git_to_haiku.md` — sectie C stap 6.
