using CoachOS.Application.Pricing;
using CoachOS.Application.Students.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Students;

public class StudentLessonsService(
    IScheduleAssignmentRepository assignmentRepo,
    IPaymentRepository paymentRepo,
    IPricingService pricingService,
    IUserLookupService userLookup) : IStudentLessonsService
{
    public async Task<Result<List<StudentLessonDto>>> GetMyLessonsAsync(
        string email, CancellationToken ct = default)
    {
        var assignments = await assignmentRepo.GetByContactEmailAsync(email, ct);
        return await BuildDtosAsync(assignments, ct);
    }

    public async Task<Result<StudentLessonDto>> GetMyLessonAsync(
        string email, Guid assignmentId, CancellationToken ct = default)
    {
        var assignments = await assignmentRepo.GetByContactEmailAsync(email, ct);
        var match = assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (match is null)
            return Result<StudentLessonDto>.Fail(new Error(ErrorCodes.NotFound, "Les niet gevonden."));

        Result<List<StudentLessonDto>> dtos = await BuildDtosAsync([match], ct);
        if (!dtos.IsSuccess) return Result<StudentLessonDto>.Fail(dtos.Errors);

        return Result<StudentLessonDto>.Ok(dtos.Value![0]);
    }

    private async Task<Result<List<StudentLessonDto>>> BuildDtosAsync(
        List<ScheduleAssignment> assignments, CancellationToken ct)
    {
        if (assignments.Count == 0) return Result<List<StudentLessonDto>>.Ok([]);

        var trainerIds = assignments
            .Select(a => a.WeeklyTemplateEntry.TrainerId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var trainerNames = await userLookup.GetUserNamesByIdsAsync(trainerIds, ct);

        // Betaling wordt opgeslagen tegen de leader (solo: eigen enrollment, group: LeaderEnrollmentId).
        // Lookup moet dezelfde leader gebruiken — anders krijgen niet-leader-members PaymentStatus=null.
        var enrollmentIds = assignments
            .Select(a => a.EnrollmentGroupId.HasValue && a.EnrollmentGroup is not null
                ? a.EnrollmentGroup.LeaderEnrollmentId
                : a.EnrollmentId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var paymentStatus = await paymentRepo.GetLatestStatusByEnrollmentIdsAsync(enrollmentIds, ct);

        // Prijzen vooraf ophalen: CalculateForGroupAsync is async en kan dus niet
        // binnen de Select-projectie hieronder aangeroepen worden.
        Dictionary<Guid, decimal> priceByAssignment = [];
        foreach (ScheduleAssignment assignment in assignments)
        {
            Result<PriceBreakdown> priceResult = await pricingService.CalculateForGroupAsync(
                assignment.LessonSerieId, ResolveParticipants(assignment), ct);
            if (!priceResult.IsSuccess)
                return Result<List<StudentLessonDto>>.Fail(priceResult.Errors);

            priceByAssignment[assignment.Id] = priceResult.Value!.Total;
        }

        return Result<List<StudentLessonDto>>.Ok(assignments
            .OrderBy(a => a.WeeklyTemplateEntry.DayOfWeek)
            .ThenBy(a => a.WeeklyTemplateEntry.StartTime)
            .Select(a =>
            {
                var slot = a.WeeklyTemplateEntry;
                var series = a.LessonSerie;
                var isGroup = a.EnrollmentGroupId.HasValue && a.EnrollmentGroup is not null;
                var size = isGroup ? a.EnrollmentGroup!.Members.Count : 1;

                // Payment is altijd tegen de leader geboekt — gebruik LeaderEnrollmentId voor groups.
                Guid? enrId = a.EnrollmentGroupId.HasValue && a.EnrollmentGroup is not null
                    ? a.EnrollmentGroup.LeaderEnrollmentId
                    : a.EnrollmentId;

                return new StudentLessonDto
                {
                    AssignmentId = a.Id,
                    SeriesId = series.Id,
                    SeriesName = series.Name,
                    SeriesStartDate = series.StartDate.ToString("yyyy-MM-dd"),
                    SeriesEndDate = series.EndDate.ToString("yyyy-MM-dd"),
                    DayOfWeek = slot.DayOfWeek,
                    StartTime = slot.StartTime.ToString("HH:mm"),
                    EndTime = slot.EndTime.ToString("HH:mm"),
                    CourtName = slot.CourtName,
                    TrainerName = slot.TrainerId.HasValue
                        ? trainerNames.GetValueOrDefault(slot.TrainerId.Value)
                        : null,
                    Status = a.Status.ToString(),
                    ParticipantName = a.Enrollment?.StudentName
                        ?? a.EnrollmentGroup?.Members.FirstOrDefault()?.StudentName
                        ?? string.Empty,
                    IsGroup = isGroup,
                    GroupSize = size,
                    Price = priceByAssignment[a.Id],
                    PaymentStatus = enrId.HasValue && paymentStatus.TryGetValue(enrId.Value, out var s)
                        ? s.ToString()
                        : null,
                };
            })
            .ToList());
    }

    /// <summary>
    /// Alle deelnemers van de toewijzing, inclusief de leider. De prijsmatrix
    /// tarifeert per categorie, dus de echte inschrijvingen moeten meegegeven
    /// worden — niet alleen een aantal.
    /// </summary>
    private static IReadOnlyList<Enrollment> ResolveParticipants(ScheduleAssignment assignment)
    {
        if (assignment.EnrollmentGroupId.HasValue
            && assignment.EnrollmentGroup is not null
            && assignment.EnrollmentGroup.Members.Count > 0)
        {
            return assignment.EnrollmentGroup.Members.ToList();
        }

        return assignment.Enrollment is not null ? [assignment.Enrollment] : [];
    }
}
