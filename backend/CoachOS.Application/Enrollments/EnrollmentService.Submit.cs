using CoachOS.Application.Abstractions;
using System.Data;
using System.Text.Json;
using CoachOS.Application.Common;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

// Voorkomt ambiguity met de gelijknamige namespace CoachOS.Application.OrganizationSettings.
using OrganizationSettingsEntity = CoachOS.Domain.Entities.OrganizationSettings;

namespace CoachOS.Application.Enrollments;

/// <summary>
/// De publieke inschrijving (<see cref="SubmitEnrollmentAsync"/>), opgedeeld in benoemde stappen.
/// De volgorde van de stappen en van alle repository- en unit-of-work-aanroepen is bewust gelijk
/// aan die van de oorspronkelijke methode; <c>SubmitEnrollmentCharacterizationTests</c> bewaakt dat.
/// </summary>
public partial class EnrollmentService
{
    /// <summary>Alles wat de stappen binnen de transactie nodig hebben, op één plek.</summary>
    private sealed record SubmitContext(
        Guid LessonSeriesId,
        Domain.Entities.LessonSerie Series,
        SubmitEnrollmentRequest Request,
        int YouthMaxAge,
        DateOnly EnrolledOn);

    public async Task<Result<Guid>> SubmitEnrollmentAsync(
        Guid lessonSeriesId, SubmitEnrollmentRequest request, CancellationToken ct = default)
    {
        // 1. Load active lesson series
        Domain.Entities.LessonSerie? series = await lessonSeriesRepo.GetByIdPublicAsync(lessonSeriesId, ct);
        if (series is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        // 2. Registration deadline check
        if (timeProvider.GetUtcNow().UtcDateTime > series.RegistrationDeadline)
            return Result<Guid>.Fail(
                new Error(ErrorCodes.Validation, "De inschrijvingsdeadline is verstreken."));

        // 3. Load enrollment form with fields (may be null)
        EnrollmentForm? form = await enrollmentFormRepo.GetBySeriesIdReadOnlyAsync(lessonSeriesId, ct);

        // 4. Validaties zonder DB-werk, vóór de transactie (fail-fast, geen race-risico).
        Error? validationError = ValidateSubmission(series, form, request);
        if (validationError is not null)
            return Result<Guid>.Fail(validationError);

        int groupSize = CalculateGroupSize(request);

        // 4b. Tariefcategorie afleiden. De leeftijdsgrens is org-specifiek; deze flow is
        //     anoniem, dus de settings komen via series.OrganizationId en niet uit een JWT.
        //     De categorie wordt één keer vastgelegd bij inschrijving: wie tijdens de reeks
        //     jarig is, mag niet halverwege van tarief wisselen.
        OrganizationSettingsEntity? orgSettings =
            await orgSettingsRepo.GetByOrganizationReadOnlyAsync(series.OrganizationId, ct);
        SubmitContext context = new(
            lessonSeriesId, series, request, orgSettings?.YouthMaxAge ?? 17, timeProvider.GetBrusselsToday());

        // Prepare notification payloads before opening the transaction. The messages themselves
        // are persisted inside the transaction below, so a committed enrollment always has
        // durable notification work attached to it.
        (string FullName, string Email)? trainerInfo = await LookupFirstTrainerAsync(series, lessonSeriesId, ct);
        List<EmailResponseItem> responseItems = BuildResponseItems(form, request);
        List<(string Email, string Name)> confirmationRecipients = BuildConfirmationRecipients(request);

        // 5. Begin SERIALIZABLE transaction: capacity + duplicate checks moeten ATOMIC zijn
        //    met de insert, anders kunnen twee parallelle submitters beide de check passeren
        //    en samen MaxRegistrations overschrijden of een duplicate email creëren.
        Enrollment enrollment;
        try
        {
            await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            // 6. Capacity check INSIDE transaction (accounts for group size)
            if (!await HasCapacityAsync(series, lessonSeriesId, groupSize, ct))
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(
                    new Error(ErrorCodes.Conflict, "Deze lessenreeks is volzet."));
            }

            // 7. Dubbelcheck INSIDE transaction op persoon, niet op adres.
            string? duplicateName = await FindAlreadyEnrolledParticipantAsync(lessonSeriesId, request, ct);
            if (duplicateName is not null)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(new Error(
                    ErrorCodes.Conflict, $"{duplicateName} is al ingeschreven voor deze lessenreeks."));
            }

            // 8-9. Leider + antwoorden, 10. groep + leden, 12. tijdvoorkeuren.
            enrollment = await CreateLeaderEnrollmentAsync(context, ct);
            await CreateGroupWithMembersAsync(context, enrollment, ct);
            await SaveTimeSlotPreferencesAsync(context, enrollment, ct);

            await QueueNotificationsAsync(
                context, enrollment, confirmationRecipients, trainerInfo, responseItems, ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch (Exception ex)
        {
            return await HandleSubmitFailureAsync(ex, lessonSeriesId, ct);
        }

        return Result<Guid>.Ok(enrollment.Id);
    }

    // ── Validatie vóór de transactie ──────────────────────────────────────────

    /// <summary>
    /// Formulierantwoorden, inschrijfwijze, leeftijdsgrens en minimum aantal tijdvakken, in die volgorde.
    /// Geeft de eerste fout terug, of null als alles klopt.
    /// </summary>
    private static Error? ValidateSubmission(
        Domain.Entities.LessonSerie series, EnrollmentForm? form, SubmitEnrollmentRequest request)
    {
        // 4. Validate form responses against actual form fields (pre-transaction: cheap, no race risk)
        if (form is not null)
        {
            Error? formError = FormResponseValidator.Validate(
                form.Fields.Select(f => (f.Id, f.IsRequired, f.Label)),
                request.Responses.Select(r => (r.FormFieldId, r.Value)));
            if (formError is not null)
                return formError;
        }

        // 4-bis. Elk groepslid beantwoordt dezelfde vragen als de leider (zelfde verplicht-regels).
        Error? memberFormError = ValidateGroupMemberResponses(form, request);
        if (memberFormError is not null)
            return memberFormError;

        // 4a. Inschrijfwijze afdwingen: de admin bepaalt per reeks of solo, groep of
        //     beide toegelaten zijn. Vóór de transactie — geen DB-werk nodig voor een
        //     wijze die sowieso niet mag.
        bool wantsGroup = request.EnrollmentType == "group";
        if (wantsGroup && !series.AllowGroupEnrollment)
            return new Error(ErrorCodes.Validation, "Inschrijven in groep is niet mogelijk voor deze lessenreeks.");
        if (!wantsGroup && !series.AllowSoloEnrollment)
            return new Error(ErrorCodes.Validation, "Solo inschrijven is niet mogelijk voor deze lessenreeks.");

        // Leeftijdsgrens: toets elke deelnemer op de startdatum van de reeks.
        Error? ageError = CheckAgeEligibility(request, series);
        if (ageError is not null)
            return ageError;

        // 4a-bis. Minimum aantal beschikbare tijdvakken: enkel "Beschikbaar" en "Voorkeur" tellen mee,
        //     enkel voor weekslots van déze reeks. Zonder weekslots valt er niets te kiezen.
        return CheckMinimumAvailableSlots(request, series);
    }

    /// <summary>
    /// Valideert de antwoorden van elk groepslid tegen de velden van het formulier, net als die van de leider:
    /// verplichte vragen moeten beantwoord zijn en onbekende velden worden geweigerd. Ook zonder formulier,
    /// zodat een antwoord op een veld dat niet bestaat een nette fout geeft in plaats van een opslagfout.
    /// Enkel bij een groepsinschrijving; bij solo worden meegestuurde leden genegeerd.
    /// </summary>
    private static Error? ValidateGroupMemberResponses(EnrollmentForm? form, SubmitEnrollmentRequest request)
    {
        if (request.EnrollmentType != "group" || request.GroupMembers is null)
            return null;

        List<(Guid Id, bool IsRequired, string Label)> fields =
            (form?.Fields ?? []).Select(f => (f.Id, f.IsRequired, f.Label)).ToList();

        foreach (GroupMemberDto member in request.GroupMembers)
        {
            Error? error = FormResponseValidator.Validate(
                fields,
                (member.Responses ?? []).Select(r => (r.FormFieldId, r.Value)));
            if (error is not null)
                return new Error(error.Code, $"{member.StudentName}: {error.Message}");
        }

        return null;
    }

    /// <summary>Aantal plaatsen dat deze inschrijving inneemt: leider plus groepsleden.</summary>
    private static int CalculateGroupSize(SubmitEnrollmentRequest request)
        => request.EnrollmentType == "group" && request.GroupMembers is not null
            ? request.GroupMembers.Count + 1
            : 1;

    // ── Voorbereiding van de notificaties (vóór de transactie) ────────────────

    /// <summary>
    /// Trainer van de eerste les met een trainer. Een mislukte lookup mag de inschrijving niet
    /// blokkeren: dan wordt enkel de trainernotificatie overgeslagen.
    /// </summary>
    private async Task<(string FullName, string Email)?> LookupFirstTrainerAsync(
        Domain.Entities.LessonSerie series, Guid lessonSeriesId, CancellationToken ct)
    {
        Guid? firstTrainerId = series.Lessons
            .OrderBy(l => l.Date).ThenBy(l => l.StartTime)
            .Select(l => l.TrainerId)
            .FirstOrDefault(id => id.HasValue);
        if (!firstTrainerId.HasValue)
            return null;

        try
        {
            return await userLookup.GetUserInfoByIdAsync(firstTrainerId.Value, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Trainergegevens konden niet worden geladen voor inschrijving in reeks {SeriesId}; " +
                "de trainernotificatie wordt overgeslagen",
                lessonSeriesId);
            return null;
        }
    }

    /// <summary>De ingevulde antwoorden met hun vraag, voor de trainernotificatie. Onbekende velden vallen weg.</summary>
    private static List<EmailResponseItem> BuildResponseItems(EnrollmentForm? form, SubmitEnrollmentRequest request)
    {
        List<EmailResponseItem> responseItems = new();
        if (form is null)
            return responseItems;

        foreach (FormResponseValueDto response in request.Responses)
        {
            FormField? field = form.Fields.FirstOrDefault(f => f.Id == response.FormFieldId);
            if (field is not null)
                responseItems.Add(new EmailResponseItem(field.Label, response.Value));
        }

        return responseItems;
    }

    /// <summary>Wie een bevestigingsmail krijgt: de leider en elk groepslid, op het juiste contactadres.</summary>
    private static List<(string Email, string Name)> BuildConfirmationRecipients(SubmitEnrollmentRequest request)
    {
        List<(string Email, string Name)> recipients =
            [(EnrollmentEmails.ResolveContactEmail(request, null), request.StudentName)];
        if (request.EnrollmentType == "group" && request.GroupMembers is { Count: > 0 })
        {
            recipients.AddRange(request.GroupMembers.Select(member =>
                (EnrollmentEmails.ResolveContactEmail(request, member), member.StudentName)));
        }

        return recipients;
    }

    // ── Controles binnen de transactie ────────────────────────────────────────

    /// <summary>Past de inschrijving (incl. groepsleden) nog binnen het maximum van de reeks?</summary>
    private async Task<bool> HasCapacityAsync(
        Domain.Entities.LessonSerie series, Guid lessonSeriesId, int groupSize, CancellationToken ct)
    {
        if (!series.MaxRegistrations.HasValue)
            return true;

        int activeCount = await enrollmentRepo.CountActiveBySeriesAsync(lessonSeriesId, ct);
        return activeCount + groupSize <= series.MaxRegistrations.Value;
    }

    /// <summary>
    /// Dubbelcheck op persoon, niet op adres: één contactadres mag meerdere deelnemers dragen
    /// (ouder met kinderen, vriend die alles regelt), maar dezelfde persoon mag niet twee keer
    /// in de reeks staan. De partiële unique index IX_Enrollments_Participant dekt hetzelfde; dit
    /// vangt het als een nette 409 vóór de insert i.p.v. een 23505 halverwege de transactie.
    /// Geeft de naam van de eerste al ingeschreven deelnemer terug, of null.
    /// </summary>
    private async Task<string?> FindAlreadyEnrolledParticipantAsync(
        Guid lessonSeriesId, SubmitEnrollmentRequest request, CancellationToken ct)
    {
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
            if (await enrollmentRepo.IsDuplicateParticipantAsync(lessonSeriesId, contactEmail, name, dob, ct))
                return name;
        }

        return null;
    }

    // ── Aanmaken van de inschrijving(en) ──────────────────────────────────────

    /// <summary>
    /// Maakt de inschrijving van de leider (of de solo-deelnemer) aan, samen met zijn antwoorden.
    /// Status blijft Pending tot de student via de confirmation-mail bevestigt: de Mollie payment
    /// wordt dan getriggerd door StudentConfirmationService, niet hier. De groep heeft deze rij
    /// nodig voor <c>LeaderEnrollmentId</c>, dus eerst opslaan.
    /// </summary>
    private async Task<Enrollment> CreateLeaderEnrollmentAsync(SubmitContext context, CancellationToken ct)
    {
        SubmitEnrollmentRequest request = context.Request;

        Enrollment enrollment = new()
        {
            OrganizationId = context.Series.OrganizationId,
            LessonSerieId = context.Series.Id,
            StudentName = request.StudentName,
            ContactEmail = EnrollmentEmails.ResolveContactEmail(request, null),
            StudentEmail = EnrollmentEmails.Normalize(request.StudentEmail),
            StudentPhone = request.StudentPhone,
            Status = EnrollmentStatus.Pending,
            EnrolledAt = timeProvider.GetUtcNow().UtcDateTime,
            IsOpenToGrouping = request.IsOpenToGrouping,
            DateOfBirth = ParseBirthDate(request.DateOfBirth),
            Category = ResolveCategory(request.DateOfBirth, context.YouthMaxAge, context.EnrolledOn),
            SelectedPriceOptionId = request.SelectedPriceOptionId,
        };

        await enrollmentRepo.AddAsync(enrollment, ct);
        await AddFormResponsesAsync(enrollment.Id, request.Responses, ct);

        // Save enrollment + form responses first (needed before group creation to avoid circular FK)
        await unitOfWork.SaveChangesAsync(ct);

        return enrollment;
    }

    /// <summary>
    /// Groepsinschrijving: maakt de groep en een inschrijving per lid aan. Doet niets zonder leden.
    /// Leden erven de gekozen prijsoptie van de leider.
    /// </summary>
    private async Task CreateGroupWithMembersAsync(
        SubmitContext context, Enrollment leader, CancellationToken ct)
    {
        SubmitEnrollmentRequest request = context.Request;
        if (request.EnrollmentType != "group" || request.GroupMembers is not { Count: > 0 })
            return;

        int existingGroupCount = await enrollmentGroupRepo.CountBySeriesAsync(
            context.LessonSeriesId, context.Series.OrganizationId, ct);
        // Naming: A–Z voor eerste 26 groepen, dan AA, AB, …, BA, BB, … zodat de namen
        // leesbaar blijven (geen non-printables zoals [ of \ bij 'A'+26).
        string groupName = BuildGroupName(existingGroupCount);

        EnrollmentGroup group = new()
        {
            OrganizationId = context.Series.OrganizationId,
            LessonSerieId = context.Series.Id,
            Name = $"Groep {groupName}",
            LeaderEnrollmentId = leader.Id,
        };

        await enrollmentGroupRepo.AddAsync(group, ct);
        await unitOfWork.SaveChangesAsync(ct);

        leader.EnrollmentGroupId = group.Id;

        foreach (GroupMemberDto member in request.GroupMembers)
        {
            Enrollment memberEnrollment = new()
            {
                OrganizationId = context.Series.OrganizationId,
                LessonSerieId = context.Series.Id,
                StudentName = member.StudentName,
                ContactEmail = EnrollmentEmails.ResolveContactEmail(request, member),
                StudentEmail = string.IsNullOrWhiteSpace(member.StudentEmail)
                    ? null
                    : EnrollmentEmails.Normalize(member.StudentEmail),
                StudentPhone = member.StudentPhone,
                Status = EnrollmentStatus.Pending,
                EnrolledAt = timeProvider.GetUtcNow().UtcDateTime,
                EnrollmentGroupId = group.Id,
                DateOfBirth = ParseBirthDate(member.DateOfBirth),
                Category = ResolveCategory(member.DateOfBirth, context.YouthMaxAge, context.EnrolledOn),
                SelectedPriceOptionId = request.SelectedPriceOptionId,
            };

            await enrollmentRepo.AddAsync(memberEnrollment, ct);

            if (member.Responses is { Count: > 0 })
                await AddFormResponsesAsync(memberEnrollment.Id, member.Responses, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task AddFormResponsesAsync(
        Guid enrollmentId, IEnumerable<FormResponseValueDto> responses, CancellationToken ct)
    {
        foreach (FormResponseValueDto responseDto in responses)
        {
            FormResponse response = new()
            {
                EnrollmentId = enrollmentId,
                FormFieldId = responseDto.FormFieldId,
                Value = responseDto.Value,
            };
            await enrollmentRepo.AddFormResponseAsync(response, ct);
        }
    }

    /// <summary>Tijdvoorkeuren worden enkel op de inschrijving van de leider bewaard.</summary>
    private async Task SaveTimeSlotPreferencesAsync(
        SubmitContext context, Enrollment enrollment, CancellationToken ct)
    {
        if (context.Request.TimeSlotPreferences is not { Count: > 0 })
            return;

        IEnumerable<TimeSlotPreference> preferences = context.Request.TimeSlotPreferences.Select(p => new TimeSlotPreference
        {
            OrganizationId = context.Series.OrganizationId,
            EnrollmentId = enrollment.Id,
            WeeklyTemplateEntryId = p.WeeklyTemplateEntryId,
            Preference = (SlotPreference)p.Preference,
        });

        await timeSlotPreferenceRepo.AddRangeAsync(preferences, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    // ── Notificaties en foutafhandeling ───────────────────────────────────────

    /// <summary>
    /// Zet de bevestigingsmails (één per uniek contactadres) en de trainernotificatie in de outbox,
    /// binnen dezelfde transactie: een bevestigde inschrijving heeft altijd duurzaam mailwerk.
    /// </summary>
    private async Task QueueNotificationsAsync(
        SubmitContext context,
        Enrollment enrollment,
        List<(string Email, string Name)> confirmationRecipients,
        (string FullName, string Email)? trainerInfo,
        List<EmailResponseItem> responseItems,
        CancellationToken ct)
    {
        List<EmailOutboxMessage> outboxMessages = new();
        foreach (IGrouping<string, (string Email, string Name)> contact in
                 confirmationRecipients.GroupBy(r => r.Email))
        {
            List<string> names = contact.Select(r => r.Name).ToList();
            EnrollmentConfirmationEmailPayload payload = new(
                contact.Key,
                names[0],
                context.Series.Name,
                names);
            outboxMessages.Add(new EmailOutboxMessage
            {
                OrganizationId = context.Series.OrganizationId,
                EnrollmentId = enrollment.Id,
                Type = EmailOutboxMessageTypes.EnrollmentConfirmation,
                Payload = JsonSerializer.Serialize(payload),
            });
        }

        if (trainerInfo.HasValue)
        {
            TrainerEnrollmentNotificationEmailPayload payload = new(
                trainerInfo.Value.Email,
                trainerInfo.Value.FullName,
                context.Request.StudentName,
                EnrollmentEmails.ResolveContactEmail(context.Request, null),
                context.Series.Name,
                responseItems);
            outboxMessages.Add(new EmailOutboxMessage
            {
                OrganizationId = context.Series.OrganizationId,
                EnrollmentId = enrollment.Id,
                Type = EmailOutboxMessageTypes.TrainerNotification,
                Payload = JsonSerializer.Serialize(payload),
            });
        }

        await emailOutboxRepository.AddRangeAsync(outboxMessages, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Zet de transactie terug en vertaalt de fout. Een unique violation betekent dat een parallelle
    /// submitter ons voor was tussen de check en de insert. Dat is een conflict (409), geen serverfout:
    /// retryen met dezelfde gegevens gaat nooit lukken, dus zeg dat ook.
    /// </summary>
    private async Task<Result<Guid>> HandleSubmitFailureAsync(
        Exception exception, Guid lessonSeriesId, CancellationToken ct)
    {
        await unitOfWork.RollbackTransactionAsync(ct);

        if (IsUniqueViolation(exception))
        {
            logger.LogWarning(exception, "Dubbele inschrijving voor reeks {SeriesId}", lessonSeriesId);
            return Result<Guid>.Fail(new Error(
                ErrorCodes.Conflict, "Dit e-mailadres is al ingeschreven voor deze lessenreeks."));
        }

        logger.LogError(exception, "Inschrijving mislukt voor reeks {SeriesId}", lessonSeriesId);
        return Result<Guid>.Fail(new Error(ErrorCodes.Unexpected, "Inschrijving mislukt. Probeer het opnieuw."));
    }
}
