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
    IUserLookupService userLookup,
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

        if (lesson.RescheduledToLessonId.HasValue)
            return Result<RescheduleLessonResultDto>.Fail(
                new Error(ErrorCodes.Conflict, "Deze les is al verplaatst."));

        DateOnly newDate = DateOnly.ParseExact(request.NewDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        TimeOnly newStart = TimeOnly.ParseExact(request.NewStartTime, "HH:mm", CultureInfo.InvariantCulture);
        TimeOnly newEnd = TimeOnly.ParseExact(request.NewEndTime, "HH:mm", CultureInfo.InvariantCulture);

        // Trainer-conflict (cross-org). Sluit de huidige les uit, want die wordt geannuleerd.
        if (lesson.TrainerId.HasValue)
        {
            Lesson? conflict = await lessonRepo.FindTrainerConflictAsync(
                lesson.TrainerId.Value, newDate, newStart, newEnd,
                excludeLessonId: lesson.Id, ct);
            if (conflict is not null)
                return Result<RescheduleLessonResultDto>.Fail(new Error(ErrorCodes.Conflict,
                    "Trainer heeft al een les op dit nieuwe tijdstip."));
        }

        // Baan-conflict (binnen de org, en binnen de club als de les bij een reeks hoort — baannamen
        // zijn vrije tekst per club). Sluit de huidige les uit, want die wordt geannuleerd.
        Guid? tennisClubId = null;
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

        // Nieuwe les aanmaken — kopieer onveranderlijke metadata.
        Lesson newLesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            LessonSerieId = lesson.LessonSerieId,
            Date = newDate,
            StartTime = newStart,
            EndTime = newEnd,
            TrainerId = lesson.TrainerId,
            CourtName = lesson.CourtName,
            MaxStudents = lesson.MaxStudents,
            Level = lesson.Level,
            Notes = lesson.Notes,
            IsCancelled = false,
        };

        await lessonRepo.AddAsync(newLesson, ct);

        // Origineel cancellen + linken naar vervanger.
        lesson.IsCancelled = true;
        lesson.CancellationReason = trimmedReason ?? "Verplaatst naar andere datum";
        lesson.RescheduledToLessonId = newLesson.Id;

        await lessonRepo.SaveChangesAsync(ct);

        // Nu invitations / enrollments naar de nieuwe les verplaatsen via ExecuteUpdate.
        await invitationRepo.ReassignToLessonAsync(lesson.Id, newLesson.Id, ct);
        await enrollmentRepo.ReassignLessonLinkAsync(lesson.Id, newLesson.Id, ct);

        // Mailen — buiten de transactie. Falen mag het replan-resultaat niet roleren.
        int notified = await NotifyRecipientsAsync(
            organizationId, lesson, newLesson, trimmedReason, ct);

        return Result<RescheduleLessonResultDto>.Ok(
            new RescheduleLessonResultDto(newLesson.Id, notified));
    }

    private async Task<int> NotifyRecipientsAsync(
        Guid organizationId,
        Lesson oldLesson,
        Lesson newLesson,
        string? reason,
        CancellationToken ct)
    {
        string trainerName = oldLesson.TrainerId.HasValue
            ? (await userLookup.GetUserNameByIdAsync(oldLesson.TrainerId.Value, ct)) ?? "Trainer"
            : "Trainer";

        string? seriesName = null;
        if (oldLesson.LessonSerieId.HasValue)
        {
            Domain.Entities.LessonSerie? series = await serieRepo.GetByIdAsync(oldLesson.LessonSerieId.Value, organizationId, ct);
            seriesName = series?.Name;
        }

        // Verzamel ontvangers. Standalone: invitations (Pending + Accepted).
        // Serie-instance: enrollments (Pending + Confirmed) — gekoppeld aan de serie.
        List<(string Email, string Name)> recipients = new();

        if (oldLesson.LessonSerieId.HasValue)
        {
            List<Enrollment> enrollments = await enrollmentRepo.GetBySeriesAsync(
                oldLesson.LessonSerieId.Value, organizationId, ct);
            foreach (Enrollment e in enrollments)
            {
                if (e.Status is EnrollmentStatus.Pending or EnrollmentStatus.Confirmed or EnrollmentStatus.PendingPayment)
                    recipients.Add((e.ContactEmail, e.StudentName));
            }
        }
        else
        {
            // Invitations zijn nu reeds verplaatst naar de nieuwe les — ophalen via newLessonId.
            IReadOnlyList<LessonInvitation> invitations = await invitationRepo.GetByLessonAsync(
                newLesson.Id, organizationId, ct);
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
                    oldLesson.Date, oldLesson.StartTime,
                    newLesson.Date, newLesson.StartTime, newLesson.EndTime,
                    newLesson.CourtName, trainerName, reason, ct);
                sent++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Replan-mail faalde voor {Email} (lesson {LessonId} → {NewLessonId})",
                    email, oldLesson.Id, newLesson.Id);
            }
        }

        return sent;
    }
}
