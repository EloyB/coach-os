using CoachOS.Application.Reschedule.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Reschedule;

public class RescheduleService(
    IRescheduleRequestRepository rescheduleRepo,
    IScheduleAssignmentRepository assignmentRepo) : IRescheduleService
{
    private static readonly string[] DayNames = ["Zondag", "Maandag", "Dinsdag", "Woensdag", "Donderdag", "Vrijdag", "Zaterdag"];

    public async Task<Result<Guid>> RequestAsync(
        Guid assignmentId, Guid organizationId, CreateRescheduleRequest request, CancellationToken ct = default)
    {
        ScheduleAssignment? assignment = await assignmentRepo.GetByIdAsync(assignmentId, organizationId, ct);
        if (assignment is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Toewijzing niet gevonden."));

        if (assignment.Status != ScheduleAssignmentStatus.Confirmed)
            return Result<Guid>.Fail(new Error(ErrorCodes.Validation, "Alleen bevestigde toewijzingen kunnen verplaatst worden."));

        bool hasPending = await rescheduleRepo.HasPendingForAssignmentAsync(assignmentId, ct);
        if (hasPending)
            return Result<Guid>.Fail(new Error(ErrorCodes.Conflict, "Er loopt al een ruilverzoek voor deze toewijzing."));

        Guid enrollmentId = assignment.EnrollmentId ?? assignment.EnrollmentGroupId ?? Guid.Empty;
        if (enrollmentId == Guid.Empty)
            return Result<Guid>.Fail(new Error(ErrorCodes.Validation, "Toewijzing heeft geen gekoppelde inschrijving."));

        RescheduleRequest reschedule = new()
        {
            OrganizationId = organizationId,
            EnrollmentId = enrollmentId,
            ScheduleAssignmentId = assignmentId,
            AlternativeWeeklyTemplateEntryId = request.AlternativeWeeklyTemplateEntryId,
            Reason = request.Reason,
            Status = RescheduleRequestStatus.Pending,
        };

        await rescheduleRepo.AddAsync(reschedule, ct);
        await rescheduleRepo.SaveChangesAsync(ct);

        return Result<Guid>.Ok(reschedule.Id);
    }

    public async Task<Result<List<RescheduleRequestDto>>> GetPendingAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        List<RescheduleRequest> requests = await rescheduleRepo.GetPendingByOrganizationAsync(organizationId, ct);

        List<RescheduleRequestDto> dtos = requests.Select(r =>
        {
            WeeklyTemplateEntry? entry = r.ScheduleAssignment.WeeklyTemplateEntry;
            return new RescheduleRequestDto
            {
                Id = r.Id,
                EnrollmentId = r.EnrollmentId,
                StudentName = r.Enrollment.StudentName,
                StudentEmail = r.Enrollment.StudentEmail,
                ScheduleAssignmentId = r.ScheduleAssignmentId,
                CurrentSlotDay = entry is not null ? DayNames[(int)entry.DayOfWeek] : string.Empty,
                CurrentSlotTime = entry is not null ? entry.StartTime.ToString("HH:mm") : string.Empty,
                AlternativeWeeklyTemplateEntryId = r.AlternativeWeeklyTemplateEntryId,
                AlternativeSlotDay = r.AlternativeWeeklyTemplateEntry is not null
                    ? DayNames[(int)r.AlternativeWeeklyTemplateEntry.DayOfWeek] : null,
                AlternativeSlotTime = r.AlternativeWeeklyTemplateEntry?.StartTime.ToString("HH:mm"),
                Reason = r.Reason,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt,
            };
        }).ToList();

        return Result<List<RescheduleRequestDto>>.Ok(dtos);
    }

    public async Task<Result> ResolveAsync(
        Guid id, Guid organizationId, Guid resolvedByUserId, ResolveRescheduleRequest request, CancellationToken ct = default)
    {
        RescheduleRequest? reschedule = await rescheduleRepo.GetByIdAsync(id, organizationId, ct);
        if (reschedule is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Ruilverzoek niet gevonden."));

        if (reschedule.Status != RescheduleRequestStatus.Pending)
            return Result.Fail(new Error(ErrorCodes.Conflict, "Dit ruilverzoek is al verwerkt."));

        RescheduleRequestStatus newStatus = request.State == "approved"
            ? RescheduleRequestStatus.Approved
            : RescheduleRequestStatus.Declined;

        reschedule.Status = newStatus;
        reschedule.ResolvedAt = DateTime.UtcNow;
        reschedule.ResolvedByUserId = resolvedByUserId;
        reschedule.ResolverNote = request.Note;

        await rescheduleRepo.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
