# Gedeeld contactadres bij inschrijvingen — Implementatieplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Meerdere deelnemers kunnen één e-mailadres delen binnen een lessenreeks, zonder dat die mailbox een mail per deelnemer krijgt.

**Architecture:** `Enrollment` krijgt `ContactEmail` (verplicht, waar alle mail heen gaat) naast een nullable `StudentEmail` (identiteit van de deelnemer). De unique index op `(reeks, StudentEmail)` wordt vervangen door `(reeks, ContactEmail, StudentNameNormalized, DateOfBirth)`. Alle verzendcode gaat van `StudentEmail` naar `ContactEmail`; de planningsmail groepeert per contactadres in één mail met een aparte bevestigingslink per deelnemer.

**Tech Stack:** .NET 10, EF Core (PostgreSQL), FluentValidation, NUnit + Moq + FluentAssertions, MJML-templates, Next.js 15 + React Query + Tailwind, Playwright.

**Spec:** `docs/superpowers/specs/2026-07-22-gedeeld-contactadres-inschrijvingen-design.md`

## Global Constraints

- Services geven `Result<T>` terug; nooit exceptions voor businessfouten.
- Elke service filtert op `OrganizationId`; endpoints halen die uit `ctx.GetOrganizationId()`.
- Geen hardgecodeerde Nederlandse strings in de frontend — alles via `frontend/messages/nl.json` en `useTranslations`.
- Geen cascade deletes; EF-configuratie hoort in een `IEntityTypeConfiguration<T>`, niet in `OnModelCreating`.
- E-mailadressen worden overal genormaliseerd als `trim().ToLowerInvariant()`.
- `ContactEmail` is het enige verzendadres. `StudentEmail` mag nooit als ontvanger gebruikt worden.
- Actieve statussen zijn `Pending = 1`, `Confirmed = 2`, `PendingPayment = 5`. `Cancelled = 3` en `Waitlisted = 4` tellen niet mee voor capaciteit of dubbeldetectie.
- Backend-tests: `cd backend && dotnet test CoachOS.slnx`. Eén test: `dotnet test --filter "FullyQualifiedName~<naam>"`.
- Commit per taak, conventional commits, Nederlands of Engels consistent met de bestaande historie. Nooit `git push`.

---

## File Structure

**Backend — gewijzigd:**
- `CoachOS.Domain/Entities/Enrollment.cs` — `ContactEmail` erbij, `StudentEmail` nullable
- `CoachOS.Domain/Interfaces/IEnrollmentRepository.cs` — `IsDuplicateAsync` → `IsDuplicateParticipantAsync`
- `CoachOS.Infrastructure/Persistence/Configurations/EnrollmentConfiguration.cs` — kolommen, computed kolom, indexen
- `CoachOS.Infrastructure/Repositories/EnrollmentRepository.cs` — nieuwe dubbelcheck
- `CoachOS.Infrastructure/Repositories/ScheduleAssignmentRepository.cs` — lookup op `ContactEmail`
- `CoachOS.Domain/Interfaces/IScheduleAssignmentRepository.cs` — methodenaam
- `CoachOS.Application/Enrollments/EnrollmentEmails.cs` — contactadres-resolutie + dubbeldetectie in verzoek
- `CoachOS.Application/Enrollments/EnrollmentService.cs` — submit-flow
- `CoachOS.Application/Enrollments/DTOs/*.cs` — nullable `studentEmail`, `contactEmail`, `hasOwnEmail`
- `CoachOS.Application/Enrollments/Validators/SubmitEnrollmentRequestValidator.cs`
- `CoachOS.Application/Payments/PaymentService.cs`, `LessonSerie/LessonSerieService.cs`, `LessonReschedule/LessonRescheduleService.cs`, `Export/PlanningExportService.cs`, `Planning/PlanningService.cs`, `Reschedule/RescheduleService.cs` — verzendadres + ontdubbelen
- `CoachOS.Application/Students/StudentLessonsService.cs` + `DTOs/StudentLessonDto.cs` — `ParticipantName`
- `CoachOS.Application/Planning/ConfirmationOrchestrationService.cs` — bundeling
- `CoachOS.Domain/Interfaces/IEmailService.cs` + `CoachOS.Infrastructure/Email/EmailService.cs` — nieuwe methode

**Backend — nieuw:**
- `CoachOS.Infrastructure/Migrations/<timestamp>_AddContactEmailToEnrollment.cs` (via `dotnet ef`)
- `CoachOS.Infrastructure/Email/Templates/schedule-confirmation-multi.mjml`
- `CoachOS.Tests/Services/SharedContactEmailTests.cs`
- `CoachOS.Tests/Services/ConfirmationBundlingTests.cs`

**Frontend — gewijzigd:**
- `frontend/lib/api/enrollments.ts` — types
- `frontend/app/(public)/enroll/[seriesId]/page.tsx` — checkbox per groepslid
- `frontend/app/(dashboard)/dashboard/lessons/[id]/page.tsx` — "via"-adres + dubbelbadge
- `frontend/messages/nl.json` — nieuwe strings
- `frontend/app/(student)/...` lessenlijst — deelnemersnaam tonen

**Scripts:**
- `backend/Scripts/seed-data.json`, `backend/Scripts/seed-demo-data.py`

---

## Task 1: Datamodel en migratie

**Files:**
- Modify: `backend/CoachOS.Domain/Entities/Enrollment.cs:13`
- Modify: `backend/CoachOS.Infrastructure/Persistence/Configurations/EnrollmentConfiguration.cs:20-66`
- Create: `backend/CoachOS.Infrastructure/Migrations/<timestamp>_AddContactEmailToEnrollment.cs` (gegenereerd)

**Interfaces:**
- Produces: `Enrollment.ContactEmail` (`string`, verplicht), `Enrollment.StudentEmail` (`string?`), shadow property `"StudentNameNormalized"` (`string`, computed, stored), unique index `IX_Enrollments_Participant`.

- [ ] **Step 1: Entity aanpassen**

In `Enrollment.cs`, vervang de regel `public string StudentEmail { get; set; } = string.Empty;` door:

```csharp
    /// <summary>
    /// Waar élke mail voor deze inschrijving heen gaat. Genormaliseerd opgeslagen
    /// (trim + lowercase). Bij een deelnemer zonder eigen adres is dit het adres van
    /// de groepsleider — die neemt de communicatie voor de hele groep op zich.
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Eigen adres van de deelnemer. Null wanneer de communicatie via de
    /// contactpersoon loopt. Puur identiteit en weergave — nooit een verzendadres;
    /// gebruik daarvoor altijd <see cref="ContactEmail"/>.
    /// </summary>
    public string? StudentEmail { get; set; }
```

- [ ] **Step 2: EF-configuratie aanpassen**

In `EnrollmentConfiguration.cs`, vervang het `StudentEmail`-blok (regel 20-22) door:

```csharp
        builder.Property(e => e.ContactEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.StudentEmail)
            .HasMaxLength(200);

        // Genormaliseerde naam als stored computed kolom: de unique index hieronder
        // moet deterministisch zijn en mag niet van de Postgres-collatie afhangen.
        builder.Property<string>("StudentNameNormalized")
            .HasComputedColumnSql("lower(btrim(\"StudentName\"))", stored: true);
```

Vervang daarna het indexblok (regel 60-66) door:

```csharp
        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.ContactEmail);
        builder.HasIndex(e => e.LessonId);
        builder.HasIndex(e => e.LessonSerieId);

        // Dezelfde persoon mag niet twee keer in dezelfde reeks staan; verschillende
        // personen op één contactadres mogen wél. Partieel op DateOfBirth: rijen van
        // vóór de geboortedatum-feature zouden de index anders blokkeren.
        // Statussen 1, 2, 5 = Pending, Confirmed, PendingPayment.
        builder.HasIndex(nameof(Enrollment.LessonSerieId), nameof(Enrollment.ContactEmail),
                "StudentNameNormalized", nameof(Enrollment.DateOfBirth))
            .IsUnique()
            .HasDatabaseName("IX_Enrollments_Participant")
            .HasFilter("\"DateOfBirth\" IS NOT NULL AND \"Status\" IN (1, 2, 5)");
```

- [ ] **Step 3: Migratie genereren**

```bash
cd backend
dotnet ef migrations add AddContactEmailToEnrollment --project CoachOS.Infrastructure --startup-project CoachOS.API
```

Verwacht: nieuw bestand onder `CoachOS.Infrastructure/Migrations/`.

- [ ] **Step 4: Backfill in de migratie zetten**

EF genereert `AddColumn<string>(name: "ContactEmail", ..., nullable: false, defaultValue: "")`. Dat is fout — bestaande rijen zouden een leeg adres krijgen. Pas `Up()` handmatig aan zodat de kolom eerst nullable is, gevuld wordt en pas dan verplicht wordt. Zet dit blok bovenaan `Up()`, vóór het aanmaken van de index:

```csharp
    migrationBuilder.AddColumn<string>(
        name: "ContactEmail",
        table: "Enrollments",
        type: "character varying(200)",
        maxLength: 200,
        nullable: true);

    migrationBuilder.Sql(
        "UPDATE \"Enrollments\" SET \"ContactEmail\" = lower(btrim(\"StudentEmail\"));");

    migrationBuilder.AlterColumn<string>(
        name: "ContactEmail",
        table: "Enrollments",
        type: "character varying(200)",
        maxLength: 200,
        nullable: false,
        oldNullable: true);
```

Verwijder de door EF gegenereerde `AddColumn` voor `ContactEmail` (die met `defaultValue: ""`). Controleer dat `DropIndex` voor `IX_Enrollments_LessonSerieId_StudentEmail` en `IX_Enrollments_StudentEmail` aanwezig is en dat de nieuwe indexen ná de backfill worden aangemaakt.

In `Down()`: de omgekeerde volgorde, met `StudentEmail` terug verplicht via
`migrationBuilder.Sql("UPDATE \"Enrollments\" SET \"StudentEmail\" = \"ContactEmail\" WHERE \"StudentEmail\" IS NULL;");` vóór de `AlterColumn`.

- [ ] **Step 5: Build**

```bash
cd backend && dotnet build CoachOS.slnx
```

Verwacht: build slaagt. Compilerfouten over `StudentEmail` (nu `string?`) in andere projecten zijn verwacht — die worden in taak 4 en 5 opgelost. Als de build daardoor faalt, ga door naar taak 2 en 3 en kom hier terug; commit deze taak pas als de oplossing in taak 5 rond is.

- [ ] **Step 6: Commit**

```bash
git add backend/CoachOS.Domain/Entities/Enrollment.cs \
        backend/CoachOS.Infrastructure/Persistence/Configurations/EnrollmentConfiguration.cs \
        backend/CoachOS.Infrastructure/Migrations/
git commit -m "feat(enrollments): ContactEmail naast nullable StudentEmail"
```

---

## Task 2: Dubbelcheck op deelnemer in plaats van adres

**Files:**
- Modify: `backend/CoachOS.Domain/Interfaces/IEnrollmentRepository.cs:23-24`
- Modify: `backend/CoachOS.Infrastructure/Repositories/EnrollmentRepository.cs:52-63`
- Test: `backend/CoachOS.Tests/Services/SharedContactEmailTests.cs` (aangemaakt in taak 4)

**Interfaces:**
- Consumes: `Enrollment.ContactEmail` uit taak 1.
- Produces: `Task<bool> IsDuplicateParticipantAsync(Guid lessonSeriesId, string contactEmail, string studentName, DateOnly? dateOfBirth, CancellationToken ct = default)` op `IEnrollmentRepository`.

- [ ] **Step 1: Interface aanpassen**

Vervang in `IEnrollmentRepository.cs` de declaratie van `IsDuplicateAsync` door:

```csharp
    /// <summary>
    /// Staat deze persoon al in de reeks? Identiteit = contactadres + genormaliseerde
    /// naam + geboortedatum, zodat twee kinderen op het adres van hun ouder allebei
    /// mogen inschrijven maar dezelfde persoon niet twee keer.
    /// Zonder geboortedatum is de persoon niet te identificeren; dan `false`, in lijn
    /// met de partiële unique index IX_Enrollments_Participant.
    /// </summary>
    Task<bool> IsDuplicateParticipantAsync(
        Guid lessonSeriesId, string contactEmail, string studentName,
        DateOnly? dateOfBirth, CancellationToken ct = default);
```

- [ ] **Step 2: Implementatie vervangen**

Vervang `IsDuplicateAsync` in `EnrollmentRepository.cs` door:

```csharp
    public async Task<bool> IsDuplicateParticipantAsync(
        Guid lessonSeriesId, string contactEmail, string studentName,
        DateOnly? dateOfBirth, CancellationToken ct = default)
    {
        if (dateOfBirth is null) return false;

        string normalizedEmail = contactEmail.Trim().ToLower();
        string normalizedName = studentName.Trim().ToLower();

        return await context.Enrollments
            .AsNoTracking()
            .AnyAsync(e =>
                e.LessonSerieId == lessonSeriesId &&
                e.ContactEmail.ToLower() == normalizedEmail &&
                e.StudentName.ToLower() == normalizedName &&
                e.DateOfBirth == dateOfBirth &&
                (e.Status == EnrollmentStatus.Confirmed
                    || e.Status == EnrollmentStatus.Pending
                    || e.Status == EnrollmentStatus.PendingPayment), ct);
    }
```

- [ ] **Step 3: Build**

```bash
cd backend && dotnet build CoachOS.slnx
```

Verwacht: alleen nog fouten in `EnrollmentService` (oude aanroep) en tests — die volgen in taak 3 en 4.

- [ ] **Step 4: Commit**

```bash
git add backend/CoachOS.Domain/Interfaces/IEnrollmentRepository.cs \
        backend/CoachOS.Infrastructure/Repositories/EnrollmentRepository.cs
git commit -m "feat(enrollments): dubbelcheck op deelnemer i.p.v. e-mailadres"
```

---

## Task 3: Contract en validatie

**Files:**
- Modify: `backend/CoachOS.Application/Enrollments/DTOs/GroupMemberDto.cs`
- Modify: `backend/CoachOS.Application/Enrollments/DTOs/LessonSerieEnrollmentDto.cs`
- Modify: `backend/CoachOS.Application/Enrollments/EnrollmentEmails.cs`
- Modify: `backend/CoachOS.Application/Enrollments/Validators/SubmitEnrollmentRequestValidator.cs:57-62`
- Test: `backend/CoachOS.Tests/Validators/SubmitEnrollmentRequestValidatorTests.cs`

**Interfaces:**
- Produces: `GroupMemberDto.StudentEmail` (`string?`), `EnrollmentEmails.Normalize(string)`, `EnrollmentEmails.ResolveContactEmail(SubmitEnrollmentRequest, GroupMemberDto?)`, `EnrollmentEmails.HasDuplicateParticipants(SubmitEnrollmentRequest)`, `LessonSerieEnrollmentDto.ContactEmail` / `.HasOwnEmail`.

- [ ] **Step 1: Falende validatortests schrijven**

Voeg toe aan `SubmitEnrollmentRequestValidatorTests.cs`:

```csharp
    [Test]
    public void Group_Members_May_Share_The_Leader_Email()
    {
        SubmitEnrollmentRequest request = ValidGroupRequest() with
        {
            StudentEmail = "ouder@example.com",
            GroupMembers =
            [
                new GroupMemberDto
                {
                    StudentName = "Lotte Peeters",
                    StudentEmail = "ouder@example.com",
                    DateOfBirth = "2015-03-04",
                },
            ],
        };

        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Test]
    public void Group_Member_Without_Own_Email_Is_Valid()
    {
        SubmitEnrollmentRequest request = ValidGroupRequest() with
        {
            GroupMembers =
            [
                new GroupMemberDto
                {
                    StudentName = "Lotte Peeters",
                    StudentEmail = null,
                    DateOfBirth = "2015-03-04",
                },
            ],
        };

        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Test]
    public void Same_Participant_Twice_In_One_Group_Is_Rejected()
    {
        SubmitEnrollmentRequest request = ValidGroupRequest() with
        {
            StudentName = "Lotte Peeters",
            DateOfBirth = "2015-03-04",
            GroupMembers =
            [
                new GroupMemberDto
                {
                    StudentName = "  lotte peeters ",
                    StudentEmail = null,
                    DateOfBirth = "2015-03-04",
                },
            ],
        };

        _validator.Validate(request).Errors
            .Should().Contain(e => e.ErrorMessage == "Deze deelnemer staat al in de groep.");
    }

    [Test]
    public void Leader_Email_Remains_Required()
    {
        SubmitEnrollmentRequest request = ValidGroupRequest() with { StudentEmail = "" };

        _validator.Validate(request).IsValid.Should().BeFalse();
    }
```

Voeg onderaan de fixture de helper toe (of hergebruik een bestaande factory als die er al is — controleer eerst bovenaan het bestand):

```csharp
    private static SubmitEnrollmentRequest ValidGroupRequest() => new()
    {
        StudentName = "Els Peeters",
        StudentEmail = "ouder@example.com",
        DateOfBirth = "1985-01-01",
        EnrollmentType = "group",
        GroupMembers = [],
    };
```

- [ ] **Step 2: Tests draaien, verwacht falen**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~SubmitEnrollmentRequestValidatorTests"
```

Verwacht: compileerfout op `StudentEmail = null` (nog `string`), of falende asserties.

- [ ] **Step 3: DTO's aanpassen**

In `GroupMemberDto.cs`, vervang `public string StudentEmail { get; init; } = string.Empty;` door:

```csharp
    /// <summary>
    /// Eigen adres van dit groepslid. Null of leeg betekent: communicatie loopt via
    /// de groepsleider, wiens adres dan als ContactEmail wordt opgeslagen.
    /// </summary>
    public string? StudentEmail { get; init; }
```

In `LessonSerieEnrollmentDto.cs`, maak `StudentEmail` nullable en voeg toe:

```csharp
    public string? StudentEmail { get; set; }

    /// <summary>Adres waar de communicatie voor deze inschrijving heen gaat.</summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>False = communicatie loopt via de contactpersoon van de groep.</summary>
    public bool HasOwnEmail { get; set; }
```

- [ ] **Step 4: EnrollmentEmails herschrijven**

Vervang de inhoud van `EnrollmentEmails.cs` (behoud namespace en `internal static class`) door:

```csharp
using CoachOS.Application.Enrollments.DTOs;

namespace CoachOS.Application.Enrollments;

/// <summary>
/// Bepaalt welk contactadres bij een inschrijving hoort en of er binnen één verzoek
/// dezelfde deelnemer twee keer staat. Adressen mogen gedeeld worden — een ouder of
/// een vriend kan de communicatie voor meerdere deelnemers op zich nemen — dus de
/// identiteit van een deelnemer is naam + geboortedatum, niet het e-mailadres.
/// </summary>
internal static class EnrollmentEmails
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Contactadres voor een groepslid: het eigen adres wanneer ingevuld, anders dat
    /// van de leider. Voor de leider zelf: geef <paramref name="member"/> als null mee.
    /// </summary>
    public static string ResolveContactEmail(SubmitEnrollmentRequest request, GroupMemberDto? member)
        => string.IsNullOrWhiteSpace(member?.StudentEmail)
            ? Normalize(request.StudentEmail)
            : Normalize(member.StudentEmail);

    /// <summary>
    /// Staat dezelfde persoon (genormaliseerde naam + geboortedatum) meer dan één keer
    /// in het verzoek? Vangt de typfout in het formulier zelf af, zonder server-lookup
    /// en dus zonder te lekken wie er al ingeschreven staat.
    /// </summary>
    public static bool HasDuplicateParticipants(SubmitEnrollmentRequest request)
    {
        List<(string Name, string Dob)> people =
            [(NormalizeName(request.StudentName), request.DateOfBirth ?? string.Empty)];

        if (request.EnrollmentType == "group" && request.GroupMembers is not null)
        {
            people.AddRange(request.GroupMembers.Select(m =>
                (NormalizeName(m.StudentName), m.DateOfBirth ?? string.Empty)));
        }

        return people.Distinct().Count() != people.Count;
    }

    public static string NormalizeName(string name) => name.Trim().ToLowerInvariant();
}
```

- [ ] **Step 5: Validator aanpassen**

In `SubmitEnrollmentRequestValidator.cs`, vervang binnen het `When(x => x.EnrollmentType == "group", ...)`-blok de regel met `EnrollmentEmails.AreUnique` door:

```csharp
            // Adressen mogen gedeeld worden; dezelfde persoon twee keer niet.
            RuleFor(x => x)
                .Must(request => !EnrollmentEmails.HasDuplicateParticipants(request))
                .WithMessage("Deze deelnemer staat al in de groep.");
```

Vervang in hetzelfde bestand de `StudentEmail`-regel binnen `RuleForEach(x => x.GroupMembers).ChildRules(m => ...)` door:

```csharp
                m.RuleFor(v => v.StudentEmail)
                    .EmailAddress().WithMessage("Ongeldig e-mailadres")
                    .MaximumLength(200).WithMessage("E-mailadres is te lang")
                    .When(v => !string.IsNullOrWhiteSpace(v.StudentEmail));
```

De regels voor `x.StudentEmail` (de leider) blijven ongewijzigd — dat adres blijft verplicht.

- [ ] **Step 6: Tests draaien**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~SubmitEnrollmentRequestValidatorTests"
```

Verwacht: alle tests slagen. Tests die `AreUnique`-gedrag afdwongen ("Elk groepslid moet een uniek e-mailadres hebben") bestaan mogelijk nog — verwijder die; ze beschrijven het oude contract.

- [ ] **Step 7: Commit**

```bash
git add backend/CoachOS.Application/Enrollments/ backend/CoachOS.Tests/Validators/
git commit -m "feat(enrollments): groepsleden mogen contactadres delen"
```

---

## Task 4: Submit-flow

**Files:**
- Modify: `backend/CoachOS.Application/Enrollments/EnrollmentService.cs:100-120, 232-300, 340-360, 420-470, 530-550`
- Create: `backend/CoachOS.Tests/Services/SharedContactEmailTests.cs`

**Interfaces:**
- Consumes: `IsDuplicateParticipantAsync` (taak 2), `EnrollmentEmails.ResolveContactEmail` (taak 3).
- Produces: inschrijvingen met correct gevulde `ContactEmail` en `StudentEmail`.

- [ ] **Step 1: Falende tests schrijven**

Maak `backend/CoachOS.Tests/Services/SharedContactEmailTests.cs`. Kopieer de volledige `SetUp` uit `backend/CoachOS.Tests/Services/EnrollmentServiceTests.cs` (mocks + `_service`-constructie) zodat deze fixture zelfstandig draait, en voeg toe:

```csharp
    [Test]
    public async Task Group_Members_Without_Own_Email_Inherit_The_Leader_Address()
    {
        List<Enrollment> added = CaptureAddedEnrollments();

        SubmitEnrollmentRequest request = GroupRequest(
            leaderEmail: "ouder@example.com",
            members:
            [
                ("Lotte Peeters", null, "2015-03-04"),
                ("Sofie Peeters", null, "2017-06-11"),
            ]);

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeTrue();
        added.Should().HaveCount(3);
        added.Should().OnlyContain(e => e.ContactEmail == "ouder@example.com");
        added.Where(e => e.StudentName != "Els Peeters")
            .Should().OnlyContain(e => e.StudentEmail == null);
    }

    [Test]
    public async Task Group_Member_With_Own_Email_Keeps_It_As_Contact()
    {
        List<Enrollment> added = CaptureAddedEnrollments();

        SubmitEnrollmentRequest request = GroupRequest(
            leaderEmail: "els@example.com",
            members: [("Jan Peeters", "jan@example.com", "1990-02-02")]);

        await _service.SubmitEnrollmentAsync(SeriesId, request);

        Enrollment member = added.Single(e => e.StudentName == "Jan Peeters");
        member.ContactEmail.Should().Be("jan@example.com");
        member.StudentEmail.Should().Be("jan@example.com");
    }

    [Test]
    public async Task Same_Participant_Already_Enrolled_Is_Rejected()
    {
        _enrollmentRepo
            .Setup(r => r.IsDuplicateParticipantAsync(
                SeriesId, "ouder@example.com", "Lotte Peeters",
                new DateOnly(2015, 3, 4), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SubmitEnrollmentRequest request = GroupRequest(
            leaderEmail: "ouder@example.com",
            members: [("Lotte Peeters", null, "2015-03-04")]);

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        _enrollmentRepo.Verify(r => r.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Confirmation_Email_Is_Sent_Once_Per_Contact_Address()
    {
        SubmitEnrollmentRequest request = GroupRequest(
            leaderEmail: "ouder@example.com",
            members:
            [
                ("Lotte Peeters", null, "2015-03-04"),
                ("Sofie Peeters", null, "2017-06-11"),
            ]);

        await _service.SubmitEnrollmentAsync(SeriesId, request);

        _emailService.Verify(s => s.SendEnrollmentConfirmationAsync(
            "ouder@example.com", It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private List<Enrollment> CaptureAddedEnrollments()
    {
        List<Enrollment> added = [];
        _enrollmentRepo
            .Setup(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
            .Callback<Enrollment, CancellationToken>((e, _) => added.Add(e))
            .Returns(Task.CompletedTask);
        return added;
    }

    private static SubmitEnrollmentRequest GroupRequest(
        string leaderEmail,
        List<(string Name, string? Email, string Dob)> members) => new()
    {
        StudentName = "Els Peeters",
        StudentEmail = leaderEmail,
        DateOfBirth = "1985-01-01",
        EnrollmentType = "group",
        GroupMembers = members
            .Select(m => new GroupMemberDto
            {
                StudentName = m.Name,
                StudentEmail = m.Email,
                DateOfBirth = m.Dob,
            })
            .ToList(),
    };
```

Zorg dat de `SetUp` de reeks laat bestaan met genoeg capaciteit (kijk hoe `EnrollmentServiceTests` `_lessonSeriesRepo.Setup(...GetByIdPublicAsync...)` opzet en neem dat over).

- [ ] **Step 2: Tests draaien, verwacht falen**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~SharedContactEmailTests"
```

Verwacht: FAIL — `ContactEmail` is leeg en `IsDuplicateParticipantAsync` wordt nog niet aangeroepen.

- [ ] **Step 3: Dubbelcheck in de transactie vervangen**

In `EnrollmentService.SubmitEnrollmentAsync`, vervang het volledige blok dat begint bij `List<string> emails = EnrollmentEmails.CollectNormalized(request);` tot en met de afsluitende `}` van de `foreach (string email in emails)`-lus door:

```csharp
            // Dubbelcheck op persoon, niet op adres: één contactadres mag meerdere
            // deelnemers dragen (ouder met kinderen, vriend die alles regelt), maar
            // dezelfde persoon mag niet twee keer in de reeks staan.
            List<(string ContactEmail, string Name, DateOnly? Dob)> participants =
            [
                (EnrollmentEmails.ResolveContactEmail(request, null),
                 request.StudentName, ParseBirthDate(request.DateOfBirth)),
            ];

            if (request.EnrollmentType == "group" && request.GroupMembers is not null)
            {
                participants.AddRange(request.GroupMembers.Select(m =>
                    (EnrollmentEmails.ResolveContactEmail(request, m),
                     m.StudentName, ParseBirthDate(m.DateOfBirth))));
            }

            foreach ((string contactEmail, string name, DateOnly? dob) in participants)
            {
                if (!await enrollmentRepo.IsDuplicateParticipantAsync(
                        lessonSeriesId, contactEmail, name, dob, ct))
                    continue;

                await enrollmentRepo.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(new Error(
                    ErrorCodes.Conflict, $"{name} is al ingeschreven voor deze lessenreeks."));
            }
```

- [ ] **Step 4: ContactEmail vullen bij het aanmaken**

In hetzelfde bestand, bij het aanmaken van de leider-`Enrollment` (`StudentEmail = request.StudentEmail,`), vervang die regel door:

```csharp
            ContactEmail = EnrollmentEmails.ResolveContactEmail(request, null),
            StudentEmail = EnrollmentEmails.Normalize(request.StudentEmail),
```

En bij het aanmaken van `memberEnrollment` (`StudentEmail = member.StudentEmail,`):

```csharp
                    ContactEmail = EnrollmentEmails.ResolveContactEmail(request, member),
                    StudentEmail = string.IsNullOrWhiteSpace(member.StudentEmail)
                        ? null
                        : EnrollmentEmails.Normalize(member.StudentEmail),
```

- [ ] **Step 5: Bevestigingsmails ontdubbelen**

Vervang het mailblok (de aanroep `SendEnrollmentConfirmationAsync` voor de leider plus de `foreach`-lus over `request.GroupMembers`) door:

```csharp
            // Eén mail per contactadres: wie de communicatie voor meerdere deelnemers
            // draagt, hoort niet drie keer dezelfde bevestiging te krijgen.
            List<(string Email, string Name)> confirmationRecipients =
                [(EnrollmentEmails.ResolveContactEmail(request, null), request.StudentName)];

            if (request.EnrollmentType == "group" && request.GroupMembers is { Count: > 0 })
            {
                confirmationRecipients.AddRange(request.GroupMembers.Select(m =>
                    (EnrollmentEmails.ResolveContactEmail(request, m), m.StudentName)));
            }

            foreach ((string email, string name) in confirmationRecipients.DistinctBy(r => r.Email))
            {
                try
                {
                    await emailService.SendEnrollmentConfirmationAsync(
                        email, name, series.Name, trainerInfo?.FullName ?? string.Empty, ct);
                }
                catch (Exception memberEx)
                {
                    logger.LogError(memberEx,
                        "Bevestigingsmail naar {Email} mislukt voor inschrijving {EnrollmentId}",
                        email, enrollment.Id);
                }
            }
```

De trainer-notificatie eronder blijft ongewijzigd, behalve dat `request.StudentEmail` daar het contactadres wordt: `EnrollmentEmails.ResolveContactEmail(request, null)`.

- [ ] **Step 6: DTO-projectie aanvullen**

In de methode die `LessonSerieEnrollmentDto` opbouwt (rond regel 108, `StudentEmail = e.StudentEmail,`), vervang door:

```csharp
            StudentEmail = e.StudentEmail,
            ContactEmail = e.ContactEmail,
            HasOwnEmail = e.StudentEmail is not null,
```

Doe hetzelfde in de projectie naar `EnrollmentWithPreferencesDto` (rond regel 543): gebruik `e.ContactEmail` waar het DTO-veld `StudentEmail` heet, want dat wordt in de planning gebruikt om te mailen.

- [ ] **Step 7: Tests draaien**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~SharedContactEmailTests"
```

Verwacht: PASS, 4 tests.

- [ ] **Step 8: Commit**

```bash
git add backend/CoachOS.Application/Enrollments/EnrollmentService.cs \
        backend/CoachOS.Tests/Services/SharedContactEmailTests.cs
git commit -m "feat(enrollments): submit-flow vult contactadres en ontdubbelt mails"
```

---

## Task 5: Verzendadressen omzetten

**Files:**
- Modify: `backend/CoachOS.Application/Payments/PaymentService.cs:444`
- Modify: `backend/CoachOS.Application/LessonSerie/LessonSerieService.cs:325-334`
- Modify: `backend/CoachOS.Application/LessonReschedule/LessonRescheduleService.cs:138-142`
- Modify: `backend/CoachOS.Application/Planning/PlanningService.cs:176`
- Modify: `backend/CoachOS.Application/Planning/ConfirmationOrchestrationService.cs:143`
- Modify: `backend/CoachOS.Application/Export/PlanningExportService.cs:87, 148, 152`
- Modify: `backend/CoachOS.Application/Reschedule/RescheduleService.cs:62`

**Interfaces:**
- Consumes: `Enrollment.ContactEmail` (taak 1).
- Produces: geen nieuwe API — na deze taak compileert de oplossing weer volledig.

- [ ] **Step 1: Falende test schrijven**

Voeg toe aan `backend/CoachOS.Tests/Services/SharedContactEmailTests.cs`:

```csharp
    [Test]
    public void No_Sender_Uses_StudentEmail_Anymore()
    {
        string[] senderFiles =
        [
            "Payments/PaymentService.cs",
            "LessonSerie/LessonSerieService.cs",
            "LessonReschedule/LessonRescheduleService.cs",
            "Planning/ConfirmationOrchestrationService.cs",
        ];

        string root = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "CoachOS.Application");

        foreach (string file in senderFiles)
        {
            string source = File.ReadAllText(Path.Combine(root, file));
            source.Should().NotContain(".StudentEmail,",
                because: $"{file} moet naar ContactEmail sturen, niet naar het adres van de deelnemer");
        }
    }
```

- [ ] **Step 2: Test draaien, verwacht falen**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~No_Sender_Uses_StudentEmail_Anymore"
```

Verwacht: FAIL op elk van de vier bestanden.

- [ ] **Step 3: Verzendadressen omzetten**

Vervang in elk van deze regels `StudentEmail` door `ContactEmail`:

- `PaymentService.cs:444` — `enrollment.StudentEmail` in `SendEnrollmentConfirmationAsync`
- `LessonSerieService.cs:328` — `enrollment.StudentEmail` in `SendLessonCancellationAsync`
- `LessonRescheduleService.cs:140` — `recipients.Add((e.StudentEmail, e.StudentName))`
- `ConfirmationOrchestrationService.cs:143` — `StudentEmail = token.Enrollment.StudentEmail` in `NonResponderDto` (de admin ziet zo het adres waar de mail heen ging)
- `PlanningService.cs:176`, `PlanningExportService.cs:87, 148, 152`, `RescheduleService.cs:62` — idem; dit zijn weergaves en exports, en `ContactEmail` is daar het bruikbare adres.

- [ ] **Step 4: Annulering- en verzetmails ontdubbelen**

In `LessonSerieService.cs`, vervang de `foreach`-lus over `activeEnrollments` door:

```csharp
            // Eén mail per contactadres: een ouder met drie kinderen in de reeks hoort
            // één annuleringsbericht te krijgen, geen drie.
            foreach (Domain.Entities.Enrollment enrollment in activeEnrollments.DistinctBy(e => e.ContactEmail))
            {
                _ = emailService.SendLessonCancellationAsync(
                    enrollment.ContactEmail,
                    enrollment.StudentName,
                    series.Name,
                    lesson.Date,
                    lesson.StartTime,
                    lesson.CancellationReason);
            }
```

In `LessonRescheduleService.cs`, laat de `recipients`-lijst opbouwen zoals nu, maar ontdubbel vlak vóór het versturen. Zoek de lus die over `recipients` itereert en zet ervoor:

```csharp
        recipients = recipients.DistinctBy(r => r.Item1).ToList();
```

Als `recipients` een `List<(string, string)>` is die read-only wordt gebruikt, pas dan de declaratie aan naar een lokale variabele die herbindbaar is.

- [ ] **Step 5: Volledige build en tests**

```bash
cd backend && dotnet build CoachOS.slnx && dotnet test CoachOS.slnx
```

Verwacht: build slaagt, alle tests groen. Bestaande tests die `StudentEmail` als verzendadres verwachtten, moeten aangepast worden naar `ContactEmail` — dat is een contractwijziging, geen testfout.

- [ ] **Step 6: Commit**

```bash
git add backend/CoachOS.Application/ backend/CoachOS.Tests/
git commit -m "refactor(email): verstuur altijd naar ContactEmail"
```

---

## Task 6: Portaal-lookup en deelnemersnaam

**Files:**
- Modify: `backend/CoachOS.Domain/Interfaces/IScheduleAssignmentRepository.cs`
- Modify: `backend/CoachOS.Infrastructure/Repositories/ScheduleAssignmentRepository.cs:35-56`
- Modify: `backend/CoachOS.Application/Students/StudentLessonsService.cs:16-30, 60-115`
- Modify: `backend/CoachOS.Application/Students/DTOs/StudentLessonDto.cs`
- Modify: `frontend/lib/api/student.ts` en de student-lessenpagina onder `frontend/app/(student)/student/`
- Test: `backend/CoachOS.Tests/Services/StudentLessonsServiceTests.cs`

**Interfaces:**
- Consumes: `Enrollment.ContactEmail`.
- Produces: `IScheduleAssignmentRepository.GetByContactEmailAsync(string email, CancellationToken ct = default)`, `StudentLessonDto.ParticipantName` (`string`).

- [ ] **Step 1: Falende test schrijven**

Voeg toe aan `StudentLessonsServiceTests.cs`:

```csharp
    [Test]
    public async Task Lessons_Are_Listed_Per_Participant_For_A_Shared_Contact_Address()
    {
        ScheduleAssignment lotte = AssignmentFor("Lotte Peeters", "ouder@example.com");
        ScheduleAssignment sofie = AssignmentFor("Sofie Peeters", "ouder@example.com");

        _assignmentRepo
            .Setup(r => r.GetByContactEmailAsync("ouder@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync([lotte, sofie]);

        Result<List<StudentLessonDto>> result =
            await _service.GetMyLessonsAsync("ouder@example.com");

        result.Value!.Select(l => l.ParticipantName)
            .Should().BeEquivalentTo(["Lotte Peeters", "Sofie Peeters"]);
    }
```

Bouw `AssignmentFor` naar het model van de bestaande helpers in dat testbestand: een `ScheduleAssignment` met `Enrollment` (naam + `ContactEmail`), `WeeklyTemplateEntry` en `LessonSerie`.

- [ ] **Step 2: Test draaien, verwacht falen**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~StudentLessonsServiceTests"
```

Verwacht: compileerfout — `GetByContactEmailAsync` en `ParticipantName` bestaan nog niet.

- [ ] **Step 3: Repository omzetten**

Hernoem in `IScheduleAssignmentRepository.cs` en `ScheduleAssignmentRepository.cs` de methode `GetByStudentEmailAsync` naar `GetByContactEmailAsync` en vervang de `Where`-conditie door:

```csharp
            .Where(a => a.Status != ScheduleAssignmentStatus.Declined
                && a.LessonSerie.IsActive
                && (
                    (a.Enrollment != null && a.Enrollment.ContactEmail.ToLower() == normalized)
                    || (a.EnrollmentGroup != null
                        && a.EnrollmentGroup.Members.Any(m => m.ContactEmail.ToLower() == normalized))
                ))
```

- [ ] **Step 4: DTO en service aanvullen**

Voeg toe aan `StudentLessonDto.cs`:

```csharp
    /// <summary>
    /// Naam van de deelnemer. Nodig zodra één contactadres meerdere deelnemers draagt:
    /// anders staan er in het portaal meerdere identieke rijen.
    /// </summary>
    public string ParticipantName { get; set; } = string.Empty;
```

In `StudentLessonsService.cs`: hernoem beide aanroepen naar `GetByContactEmailAsync` en vul in de projectie:

```csharp
                    ParticipantName = a.Enrollment?.StudentName
                        ?? a.EnrollmentGroup?.Members.FirstOrDefault()?.StudentName
                        ?? string.Empty,
```

- [ ] **Step 5: Tests draaien**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~StudentLessonsServiceTests"
```

Verwacht: PASS.

- [ ] **Step 6: Frontend deelnemersnaam tonen**

Voeg `participantName: string;` toe aan het `StudentLessonDto`-type in `frontend/lib/api/student.ts`. Toon de naam in de leskaart onder `frontend/app/(student)/student/` boven de reeksnaam:

```tsx
<p className="text-xs text-gray-500">{lesson.participantName}</p>
```

Gebruik geen nieuwe vertaalsleutel — het is data, geen label.

- [ ] **Step 7: Frontend build**

```bash
cd frontend && bun run build
```

Verwacht: build slaagt.

- [ ] **Step 8: Commit**

```bash
git add backend/ frontend/
git commit -m "feat(portal): lessen opzoeken op contactadres met deelnemersnaam"
```

---

## Task 7: Planningsmail bundelen

**Files:**
- Modify: `backend/CoachOS.Domain/Interfaces/IEmailService.cs:25-34`
- Modify: `backend/CoachOS.Infrastructure/Email/EmailService.cs:62-88`
- Create: `backend/CoachOS.Infrastructure/Email/Templates/schedule-confirmation-multi.mjml`
- Modify: `backend/CoachOS.Application/Planning/ConfirmationOrchestrationService.cs:44-105, 329-347`
- Create: `backend/CoachOS.Tests/Services/ConfirmationBundlingTests.cs`

**Interfaces:**
- Consumes: `Enrollment.ContactEmail`.
- Produces: `IEmailService.SendScheduleConfirmationBundleAsync(string contactEmail, string seriesName, IReadOnlyList<ScheduleConfirmationItem> items, CancellationToken ct = default)` en `public record ScheduleConfirmationItem(string ParticipantName, int DayOfWeek, string StartTime, string EndTime, string? CourtName, string ConfirmationUrl)` in `CoachOS.Domain.Models`.

- [ ] **Step 1: Falende test schrijven**

Maak `backend/CoachOS.Tests/Services/ConfirmationBundlingTests.cs`. Neem de mock-opzet over uit `backend/CoachOS.Tests/Services/ConfirmationOrchestrationServiceTests.cs` en voeg toe:

```csharp
    [Test]
    public async Task Three_Assignments_On_One_Contact_Address_Produce_One_Email()
    {
        SetUpSeriesWithAssignments(
            ("Lotte Peeters", "ouder@example.com"),
            ("Sofie Peeters", "ouder@example.com"),
            ("Jan Peeters", "ouder@example.com"));

        await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        _emailService.Verify(s => s.SendScheduleConfirmationBundleAsync(
            "ouder@example.com",
            It.IsAny<string>(),
            It.Is<IReadOnlyList<ScheduleConfirmationItem>>(items => items.Count == 3),
            It.IsAny<CancellationToken>()), Times.Once);

        _emailService.Verify(s => s.SendScheduleConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task A_Single_Recipient_Keeps_The_Existing_Template()
    {
        SetUpSeriesWithAssignments(("Jan Peeters", "jan@example.com"));

        await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        _emailService.Verify(s => s.SendScheduleConfirmationAsync(
            "jan@example.com", "Jan Peeters", It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Each_Participant_Keeps_Their_Own_Confirmation_Link()
    {
        SetUpSeriesWithAssignments(
            ("Lotte Peeters", "ouder@example.com"),
            ("Sofie Peeters", "ouder@example.com"));

        IReadOnlyList<ScheduleConfirmationItem>? captured = null;
        _emailService
            .Setup(s => s.SendScheduleConfirmationBundleAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ScheduleConfirmationItem>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<ScheduleConfirmationItem>, CancellationToken>(
                (_, _, items, _) => captured = items)
            .Returns(Task.CompletedTask);

        await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        captured!.Select(i => i.ConfirmationUrl).Distinct().Should().HaveCount(2);
    }
```

`SetUpSeriesWithAssignments` bouwt per tuple een `ScheduleAssignment` met status `Proposed`, een `Enrollment` met die naam en dat contactadres, en een bijbehorende `WeeklyTemplateEntry` in `series.WeeklyTemplate`.

- [ ] **Step 2: Test draaien, verwacht falen**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~ConfirmationBundlingTests"
```

Verwacht: compileerfout — `SendScheduleConfirmationBundleAsync` bestaat niet.

- [ ] **Step 3: Model en interface toevoegen**

Maak `backend/CoachOS.Domain/Models/ScheduleConfirmationItem.cs`:

```csharp
namespace CoachOS.Domain.Models;

/// <summary>
/// Eén deelnemer in een gebundelde planningsmail. Elke deelnemer houdt een eigen
/// bevestigingslink: de token- en betaalflow blijven per toewijzing werken.
/// </summary>
public record ScheduleConfirmationItem(
    string ParticipantName,
    int DayOfWeek,
    string StartTime,
    string EndTime,
    string? CourtName,
    string ConfirmationUrl);
```

Voeg toe aan `IEmailService.cs`, direct onder `SendScheduleConfirmationAsync`:

```csharp
    /// <summary>
    /// Eén mail voor meerdere deelnemers die hetzelfde contactadres delen. Elke
    /// deelnemer krijgt een eigen blok met een eigen bevestigingsknop.
    /// </summary>
    Task SendScheduleConfirmationBundleAsync(
        string contactEmail,
        string seriesName,
        IReadOnlyList<ScheduleConfirmationItem> items,
        CancellationToken ct = default);
```

- [ ] **Step 4: Template maken**

Maak `backend/CoachOS.Infrastructure/Email/Templates/schedule-confirmation-multi.mjml`. Kopieer `schedule-confirmation.mjml` en vervang het kaartblok (`<mj-section css-class="cos-card" ...>`) door:

```xml
    <mj-section css-class="cos-card" background-color="#FFFFFF" padding="40px">
      <mj-column>
        <mj-text mj-class="heading">Bevestig de lesmomenten</mj-text>
        <mj-text padding-top="16px">
          Hoi, we hebben tijdsloten gereserveerd voor {{participantNames}} in
          <strong style="color:#111827">{{seriesName}}</strong>. Bevestig elk lesmoment
          binnen 72 uur.
        </mj-text>
        {{participantBlocks}}
        <mj-text mj-class="fine" padding-top="20px">
          Geen antwoord binnen 72 uur? Dan neemt je club contact op.
        </mj-text>
      </mj-column>
    </mj-section>
```

- [ ] **Step 5: EmailService implementeren**

Voeg toe aan `EmailService.cs`, onder `SendScheduleConfirmationAsync`:

```csharp
    public async Task SendScheduleConfirmationBundleAsync(
        string contactEmail, string seriesName,
        IReadOnlyList<ScheduleConfirmationItem> items, CancellationToken ct = default)
    {
        var blocks = new StringBuilder();
        foreach (ScheduleConfirmationItem item in items)
        {
            int safeEu = Math.Clamp(item.DayOfWeek, 0, 6);
            string dayName = DaysNl[(safeEu + 1) % 7];
            string courtLine = string.IsNullOrWhiteSpace(item.CourtName)
                ? string.Empty
                : $"Baan: {WebUtility.HtmlEncode(item.CourtName)}";

            // Handmatig encoderen: dit blok gaat als raw: token de renderer in,
            // dus de automatische encoding van Render() slaat het over.
            blocks.Append($"""
                <div style="background:#FAFAF8;border-left:4px solid #D0FF14;border-radius:8px;padding:16px 20px;margin:16px 0">
                  <div style="font-size:14px;font-weight:600;color:#111827">{WebUtility.HtmlEncode(item.ParticipantName)}</div>
                  <div style="font-size:14px;color:#111827;padding-top:4px">{dayName} {WebUtility.HtmlEncode(item.StartTime)} — {WebUtility.HtmlEncode(item.EndTime)}</div>
                  <div style="font-size:13px;color:#6b7280;padding-top:4px">{courtLine}</div>
                  <a href="{WebUtility.HtmlEncode(item.ConfirmationUrl)}" style="display:inline-block;margin-top:12px;background:#2D5016;color:#FFFFFF;border-radius:8px;font-weight:600;font-size:14px;padding:10px 20px;text-decoration:none">Bevestigen of wijzigen</a>
                </div>
                """);
        }

        string names = string.Join(", ", items.Select(i => i.ParticipantName));

        var html = renderer.Render("schedule-confirmation-multi", new Dictionary<string, string>
        {
            ["seriesName"] = seriesName,
            ["participantNames"] = names,
            ["raw:participantBlocks"] = blocks.ToString(),
            ["year"] = DateTime.UtcNow.Year.ToString(),
        });

        await SendAsync(contactEmail, names,
            $"Bevestig de lesmomenten voor {names} — {seriesName}", html, ct);
    }
```

Voeg bovenaan het bestand `using System.Net;` en `using System.Text;` toe als die er nog niet staan.

- [ ] **Step 6: Orchestration bundelen**

In `ConfirmationOrchestrationService.ConfirmScheduleAsync`, vervang de verzendlus (`foreach (var (recipient, assignment, rawToken) in emailsToSend)`) door:

```csharp
        // Groeperen op contactadres: wie meerdere deelnemers draagt, krijgt één mail
        // met een eigen bevestigingsknop per deelnemer. Tokens blijven per toewijzing.
        var byContact = emailsToSend
            .Where(x => slotById.ContainsKey(x.assignment.WeeklyTemplateEntryId))
            .GroupBy(x => x.recipient.ContactEmail.Trim().ToLowerInvariant());

        var baseUrl = appOptions.Value.ConfirmationBaseUrl.TrimEnd('/');

        foreach (var group in byContact)
        {
            var entries = group.ToList();
            try
            {
                if (entries.Count == 1)
                {
                    var (recipient, assignment, rawToken) = entries[0];
                    await SendConfirmationEmailAsync(
                        recipient, series, slotById[assignment.WeeklyTemplateEntryId], rawToken, ct);
                    continue;
                }

                List<ScheduleConfirmationItem> items = entries
                    .Select(e =>
                    {
                        WeeklyTemplateEntry slot = slotById[e.assignment.WeeklyTemplateEntryId];
                        return new ScheduleConfirmationItem(
                            e.recipient.StudentName,
                            slot.DayOfWeek,
                            slot.StartTime.ToString("HH:mm"),
                            slot.EndTime.ToString("HH:mm"),
                            slot.CourtName,
                            $"{baseUrl}/{e.rawToken}");
                    })
                    .ToList();

                await emailService.SendScheduleConfirmationBundleAsync(
                    group.Key, series.Name, items, ct);
            }
            catch (Exception ex)
            {
                // Eén fout raakt nu meerdere deelnemers; log daarom alle assignment-id's,
                // anders is niet te achterhalen wie geen mail kreeg.
                logger.LogError(ex, "E-mail naar {ContactEmail} mislukt voor toewijzingen {AssignmentIds}.",
                    group.Key, string.Join(", ", entries.Select(e => e.assignment.Id)));
            }
        }
```

Pas `SendConfirmationEmailAsync` aan zodat het naar `recipient.ContactEmail` stuurt in plaats van `recipient.StudentEmail`.

- [ ] **Step 7: Tests draaien**

```bash
cd backend && dotnet test --filter "FullyQualifiedName~ConfirmationBundlingTests"
```

Verwacht: PASS, 3 tests.

- [ ] **Step 8: Template-compilatie verifiëren**

De renderer compileert alle templates bij het opstarten en gooit bij een MJML-fout een exception. Start de API en controleer de logregel:

```bash
cd backend/CoachOS.API && dotnet run
```

Verwacht: logregel `Compiled MJML template: schedule-confirmation-multi`. Stop de API daarna.

- [ ] **Step 9: Commit**

```bash
git add backend/
git commit -m "feat(planning): één planningsmail per contactadres"
```

---

## Task 8: Publiek inschrijfformulier

**Files:**
- Modify: `frontend/lib/api/enrollments.ts:70-90`
- Modify: `frontend/app/(public)/enroll/[seriesId]/page.tsx:61, 178-260, 783-860`
- Modify: `frontend/messages/nl.json`
- Test: `frontend/e2e/enrollment.spec.ts`

**Interfaces:**
- Consumes: backend-contract uit taak 3 (`studentEmail` nullable op groepslid).
- Produces: `GroupMember` type met `hasOwnEmail: boolean`.

- [ ] **Step 1: Vertaalsleutels toevoegen**

Voeg toe in `frontend/messages/nl.json` binnen de `enroll`-namespace (controleer de exacte naam bovenaan het bestand):

```json
    "member_has_own_email": "Dit lid heeft een eigen e-mailadres",
    "member_contact_via_leader": "Alle communicatie loopt via {email}",
    "group_contact_explainer": "De contactpersoon ontvangt alle e-mails en de betaallink voor de hele groep.",
    "duplicate_participant": "Deze deelnemer staat al in de groep."
```

- [ ] **Step 2: API-types aanpassen**

In `frontend/lib/api/enrollments.ts`, maak `studentEmail` op `GroupMemberRequest` optioneel:

```ts
export interface GroupMemberRequest {
  studentName: string;
  /** Weglaten of null = communicatie loopt via de groepsleider. */
  studentEmail?: string | null;
  studentPhone?: string;
  /** yyyy-MM-dd — verplicht, bepaalt het tarief (volwassene/jeugd). */
  dateOfBirth: string;
  responses: { formFieldId: string; value: string }[];
}
```

Vul `LessonSeriesEnrollmentDto` aan:

```ts
  studentEmail: string | null;
  contactEmail: string;
  hasOwnEmail: boolean;
```

- [ ] **Step 3: Formulierstate uitbreiden**

In `page.tsx`, vervang het `GroupMember`-type:

```tsx
type GroupMember = {
  name: string;
  email: string;
  dateOfBirth: string;
  hasOwnEmail: boolean;
};
```

Pas `addGroupMember` aan:

```tsx
    setGroupMembers((prev) => [
      ...prev,
      { name: "", email: "", dateOfBirth: "", hasOwnEmail: false },
    ]);
```

Pas de signatuur van `updateGroupMember` aan zodat het veld `"name" | "email" | "dateOfBirth"` blijft, en voeg een aparte functie toe:

```tsx
  function toggleMemberOwnEmail(index: number, hasOwnEmail: boolean) {
    setGroupMembers((prev) =>
      prev.map((m, i) =>
        i === index ? { ...m, hasOwnEmail, email: hasOwnEmail ? m.email : "" } : m
      )
    );
  }
```

- [ ] **Step 4: Validatie aanpassen**

In de `validate()`-functie, vervang het e-mailblok binnen `groupMembers.forEach` door:

```tsx
        if (m.hasOwnEmail) {
          if (!m.email.trim()) e.email = "E-mailadres is verplicht";
          else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(m.email))
            e.email = "Ongeldig e-mailadres";
        }
```

Voeg na de `forEach` de dubbelcontrole op deelnemer toe (spiegelt `EnrollmentEmails.HasDuplicateParticipants`):

```tsx
      const people = [
        `${firstName.trim().toLowerCase()} ${lastName.trim().toLowerCase()}|${dateOfBirth}`,
        ...groupMembers.map(
          (m) => `${m.name.trim().toLowerCase()}|${m.dateOfBirth}`
        ),
      ];
      groupMembers.forEach((m, i) => {
        const key = `${m.name.trim().toLowerCase()}|${m.dateOfBirth}`;
        if (!m.name.trim() || !m.dateOfBirth) return;
        if (people.filter((p) => p === key).length > 1) {
          mErrors[i] = { ...mErrors[i], name: t("duplicate_participant") };
        }
      });
```

- [ ] **Step 5: Payload aanpassen**

In `handleSubmit`, vervang de mapping van `groupMembers`:

```tsx
            ? groupMembers.map((m) => ({
                studentName: m.name.trim(),
                studentEmail: m.hasOwnEmail ? m.email.trim() : null,
                dateOfBirth: m.dateOfBirth,
                responses: [],
              }))
```

- [ ] **Step 6: UI aanpassen**

Vervang in het groepslid-blok het e-mail-`<div>` (het blok met `placeholder={t("member_email")}`) door:

```tsx
                            <div className="sm:col-span-2">
                              <label className="flex items-center gap-2 text-xs text-gray-600">
                                <input
                                  type="checkbox"
                                  checked={member.hasOwnEmail}
                                  onChange={(e) =>
                                    toggleMemberOwnEmail(i, e.target.checked)
                                  }
                                  className="rounded border-gray-300 text-tennis-green focus:ring-tennis-green/20"
                                />
                                {t("member_has_own_email")}
                              </label>

                              {member.hasOwnEmail ? (
                                <div className="mt-2">
                                  <input
                                    type="email"
                                    value={member.email}
                                    onChange={(e) =>
                                      updateGroupMember(i, "email", e.target.value)
                                    }
                                    placeholder={t("member_email")}
                                    className={inputClass(!!memberErrors[i]?.email)}
                                  />
                                  {memberErrors[i]?.email && (
                                    <p className="text-xs text-red-500 mt-1">
                                      {memberErrors[i].email}
                                    </p>
                                  )}
                                </div>
                              ) : (
                                <p className="text-xs text-gray-400 mt-1">
                                  {t("member_contact_via_leader", {
                                    email: email.trim() || "…",
                                  })}
                                </p>
                              )}
                            </div>
```

Voeg boven de `groupMembers.map(...)` één regel uitleg toe:

```tsx
                      <p className="text-xs text-gray-500">
                        {t("group_contact_explainer")}
                      </p>
```

- [ ] **Step 7: E2E-test uitbreiden**

Voeg toe binnen `test.describe("Public Enrollment", ...)` in `frontend/e2e/enrollment.spec.ts`:

```ts
  test("group member without own email posts null and shows the leader address", async ({ page }) => {
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}`, TEST_PUBLIC_SERIES);
    await mockPublicApi(page, "GET", `/public/lessonseries/${seriesId}/form`, null, 204);

    let submitted: Record<string, unknown> | null = null;
    await page.route(`${API_BASE}/public/lessonseries/${seriesId}/enrollments`, (route) => {
      submitted = route.request().postDataJSON();
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ enrollmentId: "11111111-1111-1111-1111-111111111111" }),
      });
    });

    await page.goto(`/enroll/${seriesId}`);

    await page.getByPlaceholder("Voornaam").fill("Els");
    await page.getByPlaceholder("Achternaam").fill("Peeters");
    await page.getByPlaceholder("E-mailadres").fill("ouder@example.com");
    await page.getByLabel(/geboortedatum/i).first().fill("1985-01-01");

    await page.getByText("Samen met vrienden").click();
    await page.getByText("Groepslid toevoegen").click();

    await page.getByPlaceholder("Naam groepslid").fill("Lotte Peeters");
    await page.getByLabel(/geboortedatum groepslid 1/i).fill("2015-03-04");

    await expect(page.getByText("Alle communicatie loopt via ouder@example.com")).toBeVisible();

    await page.getByRole("button", { name: /inschrijven/i }).click();

    await expect.poll(() => submitted).not.toBeNull();
    expect(submitted!.groupMembers).toEqual([
      expect.objectContaining({ studentName: "Lotte Peeters", studentEmail: null }),
    ]);
  });
```

Controleer de placeholders en knopteksten tegen `frontend/messages/nl.json` en pas de selectors aan als de labels afwijken — de bestaande tests in dit bestand tonen welke teksten kloppen.

- [ ] **Step 8: Build en tests**

```bash
cd frontend && bun run build && bun run test:e2e
```

Verwacht: build slaagt, E2E groen (stack moet draaien).

- [ ] **Step 9: Commit**

```bash
git add frontend/
git commit -m "feat(enroll): groepslid kan communicatie via de leider laten lopen"
```

---

## Task 9: Admin-weergave

**Files:**
- Modify: `frontend/app/(dashboard)/dashboard/lessons/[id]/page.tsx:1447-1530` (`EnrollmentRow`, `EnrollmentsSection`)
- Modify: `frontend/messages/nl.json`

**Interfaces:**
- Consumes: `LessonSeriesEnrollmentDto.contactEmail` / `.hasOwnEmail` (taak 3 en 8).

- [ ] **Step 1: Vertaalsleutels toevoegen**

In `frontend/messages/nl.json`, binnen de dashboard-namespace die de lessenreeksdetail gebruikt:

```json
    "enrollment_contact_via": "via {email}",
    "enrollment_possible_duplicate": "mogelijk dubbel"
```

- [ ] **Step 2: Dubbeldetectie in de sectie**

In `EnrollmentsSection`, bereken vóór de `map` welke rijen verdacht zijn:

```tsx
  const duplicateIds = new Set(
    enrollments
      .map((e) => ({
        id: e.id,
        key: `${e.contactEmail}|${e.studentName.trim().toLowerCase()}`,
      }))
      .filter(
        (row, _, all) => all.filter((other) => other.key === row.key).length > 1
      )
      .map((row) => row.id)
  );
```

Geef `isPossibleDuplicate={duplicateIds.has(enrollment.id)}` mee aan `EnrollmentRow` en breid de props van die component uit met `isPossibleDuplicate: boolean`.

- [ ] **Step 3: Rij aanpassen**

In `EnrollmentRow`, vervang de regel die het adres toont (`<p className="text-xs text-gray-400 truncate">`) door:

```tsx
          <p className="text-xs text-gray-400 truncate">
            {enrollment.hasOwnEmail
              ? enrollment.studentEmail
              : t("enrollment_contact_via", { email: enrollment.contactEmail })}
          </p>
```

Voeg naast de categorie-badge toe:

```tsx
          {isPossibleDuplicate && (
            <Badge className="border-0 text-xs bg-amber-100 text-amber-700">
              {t("enrollment_possible_duplicate")}
            </Badge>
          )}
```

Als `EnrollmentRow` nog geen `useTranslations` gebruikt, voeg de hook toe met dezelfde namespace als de rest van de pagina.

- [ ] **Step 4: Build**

```bash
cd frontend && bun run build
```

Verwacht: build slaagt.

- [ ] **Step 5: Commit**

```bash
git add frontend/
git commit -m "feat(dashboard): toon contactadres en mogelijke dubbels bij inschrijvingen"
```

---

## Task 10: Seed en volledige reset

**Files:**
- Modify: `backend/Scripts/seed-data.json`
- Modify: `backend/Scripts/seed-demo-data.py`

**Interfaces:**
- Consumes: het volledige contract uit taken 1 tot 9.

- [ ] **Step 1: Seed-data uitbreiden**

Voeg in `seed-data.json` aan één bestaande groepsinschrijving een lid toe zonder eigen adres, zodat de reset het gedeelde contactadres echt raakt. Zoek het blok met `"enrollmentType": "group"` en voeg aan `groupMembers` toe:

```json
        {
          "studentName": "Lotte Peeters",
          "studentEmail": null,
          "dateOfBirth": "2015-03-04"
        }
```

- [ ] **Step 2: Seed-script controleren**

Controleer in `seed-demo-data.py` of `studentEmail` van een groepslid ongewijzigd wordt doorgegeven wanneer het `null` is (dus niet met een lege string wordt vervangen of weggefilterd). Pas de payload-opbouw aan waar nodig zodat `null` als `null` in de JSON belandt.

- [ ] **Step 3: Volledige reset draaien**

```bash
cd backend
bash Scripts/reset-db.sh --no-frontend
```

Wacht tot `http://localhost:5142/health` een 200 geeft, en draai dan:

```bash
bash Scripts/seed-demo-data.sh
```

Verwacht: het script loopt volledig door zonder 4xx of 5xx. De migratie past toe op een lege database, en de nieuwe unique index wordt aangemaakt.

- [ ] **Step 4: Handmatige controle op gedeeld adres**

Log in op het dashboard, open de gezaaide reeks en controleer bij Inschrijvingen dat het lid zonder eigen adres het contactadres toont met "via" ervoor. Bevestig daarna de planning en controleer in de maillogs (of Resend-dashboard) dat er één mail per contactadres uitgaat met meerdere blokken.

- [ ] **Step 5: Volledige testsuite**

```bash
cd backend && dotnet test CoachOS.slnx
cd ../frontend && bun run build && bun run test:e2e
```

Verwacht: alles groen.

- [ ] **Step 6: Commit**

```bash
git add backend/Scripts/
git commit -m "chore(scripts): seed groep met gedeeld contactadres"
```

---

## Volgorde en afhankelijkheden

Taken 1 tot 5 moeten in volgorde: tussen taak 1 en taak 5 compileert de oplossing niet volledig, omdat `StudentEmail` nullable wordt terwijl de verzendcode nog het oude type verwacht. Taak 6 en 7 zijn onafhankelijk van elkaar en kunnen na taak 5 in willekeurige volgorde. Taak 8 en 9 (frontend) hebben taak 3 nodig voor het contract. Taak 10 sluit af en is de definitieve check.
