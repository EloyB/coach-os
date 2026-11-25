using System.Data;
using System.Text.Json;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Common;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.Enrollments.Submit;

/// <summary>
/// De publieke inschrijving: aanvraag lezen, regels toetsen, en in één serializable transactie
/// plaats en dubbels controleren en alles wegschrijven, inclusief de mails in de outbox.
/// </summary>
public class EnrollmentSubmissionService(
    ILessonSerieRepository lessonSeriesRepo,
    IEnrollmentFormRepository enrollmentFormRepo,
    IOrganizationSettingsRepository orgSettingsRepo,
    IEnrollmentRepository enrollmentRepo,
    IEnrollmentGroupRepository enrollmentGroupRepo,
    IEmailOutboxRepository emailOutboxRepository,
    IUserLookupService userLookup,
    IUnitOfWork unitOfWork,
    ILogger<EnrollmentSubmissionService> logger,
    TimeProvider timeProvider) : IEnrollmentSubmissionService
{
    public async Task<Result<Guid>> SubmitAsync(
        Guid lessonSeriesId, SubmitEnrollmentRequest request, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdPublicAsync(lessonSeriesId, ct);
        if (series is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        // Vóór al de rest: na de deadline valt er niets meer te lezen of te toetsen.
        if (timeProvider.GetUtcNow().UtcDateTime > series.RegistrationDeadline)
            return Result<Guid>.Fail(new Error(ErrorCodes.Validation, "De inschrijvingsdeadline is verstreken."));

        var form = await enrollmentFormRepo.GetBySeriesIdReadOnlyAsync(lessonSeriesId, ct);
        var submission = Submission.From(series, form, request);

        if (SubmissionRules.FirstViolation(submission) is { } violation)
            return Result<Guid>.Fail(violation);

        // Deze flow is anoniem: de org-settings komen via de reeks, niet uit een JWT.
        var orgSettings = await orgSettingsRepo.GetByOrganizationReadOnlyAsync(series.OrganizationId, ct);
        var youthMaxAge = orgSettings?.YouthMaxAge ?? 17;
        var trainer = await TryLookupFirstTrainerAsync(series, ct);

        try
        {
            // Serializable: capaciteit en dubbelcheck moeten atomair zijn met de insert, anders passeren
            // twee gelijktijdige inschrijvingen allebei de check en overschrijden ze samen het maximum.
            return await unitOfWork.ExecuteInTransactionAsync(IsolationLevel.Serializable, async () =>
            {
                if (await FindConflictAsync(submission, ct) is { } conflict)
                    return Result<Guid>.Fail(conflict);

                var existingGroupCount = submission.Members.Count > 0
                    ? await enrollmentGroupRepo.CountBySeriesAsync(lessonSeriesId, series.OrganizationId, ct)
                    : 0;
                var rows = submission.BuildRows(
                    existingGroupCount, youthMaxAge, timeProvider.GetBrusselsToday(), timeProvider.GetUtcNow().UtcDateTime);

                foreach (var enrollment in rows.All)
                    await enrollmentRepo.AddAsync(enrollment, ct);
                if (rows.Group is not null)
                    await enrollmentGroupRepo.AddAsync(rows.Group, ct);
                // In dezelfde transactie: een bevestigde inschrijving heeft altijd duurzaam mailwerk.
                await emailOutboxRepository.AddRangeAsync(BuildNotifications(submission, rows.Leader, trainer), ct);
                await unitOfWork.SaveChangesAsync(ct);

                if (rows.Group is not null)
                {
                    rows.LinkLeaderToGroup();
                    await unitOfWork.SaveChangesAsync(ct);
                }

                return Result<Guid>.Ok(rows.Leader.Id);
            }, ct);
        }
        catch (Exception ex)
        {
            return TranslateFailure(ex, lessonSeriesId);
        }
    }

    /// <summary>
    /// Volzet, of een deelnemer die al in de reeks staat. Binnen de transactie. De dubbelcheck is op persoon,
    /// niet op adres: één contactadres mag meerdere deelnemers dragen (ouder met kinderen), maar dezelfde
    /// persoon niet twee keer. De partiële unique index IX_Enrollments_Participant dekt hetzelfde; dit geeft
    /// een nette 409 vóór de insert in plaats van een 23505 halverwege. Capaciteit eerst, dan per deelnemer.
    /// </summary>
    private async Task<Error?> FindConflictAsync(Submission submission, CancellationToken ct)
    {
        var series = submission.Series;
        if (series.MaxRegistrations.HasValue)
        {
            var activeCount = await enrollmentRepo.CountActiveBySeriesAsync(series.Id, ct);
            if (activeCount + submission.Participants.Count > series.MaxRegistrations.Value)
                return new Error(ErrorCodes.Conflict, "Deze lessenreeks is volzet.");
        }

        foreach (var participant in submission.Participants)
        {
            if (await enrollmentRepo.IsDuplicateParticipantAsync(
                    series.Id, participant.ContactEmail, participant.Name,
                    DateOfBirthRules.ParseOrNull(participant.DateOfBirth), ct))
            {
                return new Error(ErrorCodes.Conflict, $"{participant.Name} is al ingeschreven voor deze lessenreeks.");
            }
        }

        return null;
    }

    /// <summary>
    /// Trainer van de eerste les met een trainer. Een mislukte lookup mag de inschrijving niet blokkeren:
    /// dan wordt enkel de trainernotificatie overgeslagen.
    /// </summary>
    private async Task<(string FullName, string Email)?> TryLookupFirstTrainerAsync(
        Domain.Entities.LessonSerie series, CancellationToken ct)
    {
        var firstTrainerId = series.Lessons
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
                series.Id);
            return null;
        }
    }

    /// <summary>
    /// Eén bevestigingsmail per uniek contactadres (met alle deelnemers op dat adres) en, als er een trainer
    /// is, een trainernotificatie met de antwoorden van de leider.
    /// </summary>
    private static List<EmailOutboxMessage> BuildNotifications(
        Submission submission, Enrollment leader, (string FullName, string Email)? trainer)
    {
        var series = submission.Series;
        var messages = submission.Participants
            .GroupBy(p => p.ContactEmail)
            .Select(contact =>
            {
                var names = contact.Select(p => p.Name).ToList();
                return Outbox(EmailOutboxMessageTypes.EnrollmentConfirmation,
                    new EnrollmentConfirmationEmailPayload(contact.Key, names[0], series.Name, names));
            })
            .ToList();

        if (trainer.HasValue)
        {
            // Antwoorden op velden die niet (meer) in het formulier staan, vallen weg.
            List<EmailResponseItem> responses = submission.Form is null
                ? []
                : submission.Leader.Responses
                    .Select(r => (Response: r, Field: submission.Form.Fields.FirstOrDefault(f => f.Id == r.FormFieldId)))
                    .Where(x => x.Field is not null)
                    .Select(x => new EmailResponseItem(x.Field!.Label, x.Response.Value))
                    .ToList();

            messages.Add(Outbox(EmailOutboxMessageTypes.TrainerNotification,
                new TrainerEnrollmentNotificationEmailPayload(
                    trainer.Value.Email, trainer.Value.FullName,
                    submission.Leader.Name, submission.Leader.ContactEmail, series.Name, responses)));
        }

        return messages;

        EmailOutboxMessage Outbox<TPayload>(string type, TPayload payload) => new()
        {
            OrganizationId = series.OrganizationId,
            EnrollmentId = leader.Id,
            Type = type,
            Payload = JsonSerializer.Serialize(payload),
        };
    }

    /// <summary>
    /// Een unique violation betekent dat een gelijktijdige inschrijving ons voor was tussen de check en de
    /// insert. Dat is een conflict (409), geen serverfout: opnieuw proberen met dezelfde gegevens lukt nooit.
    /// </summary>
    private Result<Guid> TranslateFailure(Exception exception, Guid lessonSeriesId)
    {
        if (IsUniqueViolation(exception))
        {
            logger.LogWarning(exception, "Dubbele inschrijving voor reeks {SeriesId}", lessonSeriesId);
            return Result<Guid>.Fail(new Error(
                ErrorCodes.Conflict, "Dit e-mailadres is al ingeschreven voor deze lessenreeks."));
        }

        logger.LogError(exception, "Inschrijving mislukt voor reeks {SeriesId}", lessonSeriesId);
        return Result<Guid>.Fail(new Error(ErrorCodes.Unexpected, "Inschrijving mislukt. Probeer het opnieuw."));
    }

    /// <summary>
    /// Herkent een PostgreSQL unique violation (SQLSTATE 23505) ergens in de exception-keten.
    /// Bewust via reflectie: de Application-laag kent Npgsql noch EF Core.
    /// </summary>
    private static bool IsUniqueViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current.GetType().GetProperty("SqlState")?.GetValue(current) as string == "23505")
                return true;
        }

        return false;
    }
}
