using System.Globalization;
using CoachOS.Application.Common;
using CoachOS.Application.LessonReschedule.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.LessonReschedule;

public class LessonRescheduleService(
    ILessonRepository lessonRepo,
    ILessonInvitationRepository invitationRepo,
    IEnrollmentRepository enrollmentRepo,
    ILessonSerieRepository serieRepo,
    IEmailService emailService,
    ILogger<LessonRescheduleService> logger) : ILessonRescheduleService
{
    public async Task<Result<RescheduleLessonResultDto>> RescheduleAsync(
        Guid organizationId,
        Guid lessonId,
        RescheduleLessonRequest request,
        CancellationToken ct = default)
    {
        Lesson? lesson = await lessonRepo.GetByIdInOrganizationAsync(lessonId, organizationId, ct);
        if (lesson is null)
            return Result<RescheduleLessonResultDto>.Fail(
                new Error(ErrorCodes.NotFound, "Les niet gevonden."));

        if (lesson.IsCancelled)
            return Result<RescheduleLessonResultDto>.Fail(
                new Error(ErrorCodes.Validation, "Geannuleerde les kan niet verplaatst worden."));

        DateOnly newDate = DateOnly.ParseExact(request.NewDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        TimeOnly newStart = TimeOnly.ParseExact(request.NewStartTime, "HH:mm", CultureInfo.InvariantCulture);
        TimeOnly newEnd = TimeOnly.ParseExact(request.NewEndTime, "HH:mm", CultureInfo.InvariantCulture);

        // Trainer-conflict (cross-org). Sluit de huidige les uit (die wordt in-place verplaatst).
        if (lesson.TrainerId.HasValue)
        {
            Lesson? conflict = await lessonRepo.FindTrainerConflictAsync(
                lesson.TrainerId.Value, newDate, newStart, newEnd,
                excludeLessonId: lesson.Id, ct);
            if (conflict is not null)
                return Result<RescheduleLessonResultDto>.Fail(new Error(ErrorCodes.Conflict,
                    "Trainer heeft al een les op dit nieuwe tijdstip."));
        }

        // Baan-conflict (binnen de org, en binnen de club — baannamen zijn vrije tekst per club).
        // Reeks-lessen bepalen hun club via de reeks; losse lessen dragen hun eigen (mogelijk
        // legacy-null) TennisClubId. Sluit de huidige les uit (die wordt in-place verplaatst).
        Guid? tennisClubId = lesson.TennisClubId;
        if (!string.IsNullOrWhiteSpace(lesson.CourtName) && lesson.LessonSerieId.HasValue)
        {
            Domain.Entities.LessonSerie? lessonSeries =
                await serieRepo.GetByIdAsync(lesson.LessonSerieId.Value, organizationId, ct);
            tennisClubId = lessonSeries?.TennisClubId;
        }

        Error? courtConflictError = await lessonRepo.CheckCourtConflictAsync(
            organizationId, lesson.CourtName, newDate, newStart, newEnd,
            excludeLessonId: lesson.Id, tennisClubId: tennisClubId, ct: ct);
        if (courtConflictError is not null)
            return Result<RescheduleLessonResultDto>.Fail(courtConflictError);

        string? trimmedReason = string.IsNullOrWhiteSpace(request.Reason)
            ? null
            : request.Reason!.Trim();

        // Bewaar de oude datum/tijd voor de mail vóór we in-place aanpassen.
        DateOnly oldDate = lesson.Date;
        TimeOnly oldStart = lesson.StartTime;

        // In-place herplannen: pas datum/tijd aan op dezelfde les. Inschrijvingen,
        // uitnodigingen en (bij reeksen) de planning blijven eraan gekoppeld — geen
        // spook-les meer. De reden loggen we op de les zelf.
        lesson.Date = newDate;
        lesson.StartTime = newStart;
        lesson.EndTime = newEnd;

        await lessonRepo.SaveChangesAsync(ct);

        // Mailen — buiten de transactie. Falen mag het replan-resultaat niet roleren.
        int notified = await NotifyRecipientsAsync(
            organizationId, lesson, oldDate, oldStart, trimmedReason, ct);

        return Result<RescheduleLessonResultDto>.Ok(
            new RescheduleLessonResultDto(lesson.Id, notified));
    }

    private async Task<int> NotifyRecipientsAsync(
        Guid organizationId,
        Lesson lesson,
        DateOnly oldDate,
        TimeOnly oldStart,
        string? reason,
        CancellationToken ct)
    {
        string? seriesName = null;
        if (lesson.LessonSerieId.HasValue)
        {
            Domain.Entities.LessonSerie? series = await serieRepo.GetByIdAsync(lesson.LessonSerieId.Value, organizationId, ct);
            seriesName = series?.Name;
        }

        // Verzamel ontvangers. Standalone: invitations (Pending + Accepted).
        // Serie-instance: enrollments (Pending + Confirmed) — gekoppeld aan de serie.
        List<(string Email, string Name)> recipients = new();

        if (lesson.LessonSerieId.HasValue)
        {
            List<Enrollment> enrollments = await enrollmentRepo.GetBySeriesAsync(
                lesson.LessonSerieId.Value, organizationId, ct);
            foreach (Enrollment e in enrollments)
            {
                if (e.Status is EnrollmentStatus.Pending or EnrollmentStatus.Confirmed or EnrollmentStatus.PendingPayment)
                    recipients.Add((e.ContactEmail, e.StudentName));
            }
        }
        else
        {
            // De les blijft dezelfde (in-place verplaatst) — invitations hangen er nog aan.
            IReadOnlyList<LessonInvitation> invitations = await invitationRepo.GetByLessonAsync(
                lesson.Id, organizationId, ct);
            foreach (LessonInvitation inv in invitations)
            {
                if (inv.Status is LessonInvitationStatus.Pending or LessonInvitationStatus.Accepted)
                    recipients.Add((inv.Email, inv.FirstName ?? inv.Email));
            }
        }

        // Eén mail per contactadres: wie meerdere deelnemers draagt, krijgt niet
        // per deelnemer een apart verzet-bericht.
        recipients = recipients.DistinctBy(r => r.Email).ToList();

        int sent = 0;
        foreach ((string email, string name) in recipients)
        {
            try
            {
                await emailService.SendLessonRescheduledAsync(
                    email, name, seriesName,
                    oldDate, oldStart,
                    lesson.Date, lesson.StartTime, lesson.EndTime,
                    lesson.CourtName, reason, ct);
                sent++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Replan-mail faalde voor {Email} (lesson {LessonId})",
                    email, lesson.Id);
            }
        }

        return sent;
    }
}
