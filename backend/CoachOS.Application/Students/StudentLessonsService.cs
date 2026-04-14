using CoachOS.Application.Students.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Students;

public class StudentLessonsService(
    IScheduleAssignmentRepository assignmentRepo,
    IPaymentRepository paymentRepo,
    IUserLookupService userLookup) : IStudentLessonsService
{
    public async Task<Result<List<StudentLessonDto>>> GetMyLessonsAsync(
        string email, CancellationToken ct = default)
    {
        var assignments = await assignmentRepo.GetByStudentEmailAsync(email, ct);
        var dtos = await BuildDtosAsync(assignments, ct);
        return Result<List<StudentLessonDto>>.Ok(dtos);
    }

    public async Task<Result<StudentLessonDto>> GetMyLessonAsync(
        string email, Guid assignmentId, CancellationToken ct = default)
    {
        var assignments = await assignmentRepo.GetByStudentEmailAsync(email, ct);
        var match = assignments.FirstOrDefault(a => a.Id == assignmentId);
        if (match is null)
            return Result<StudentLessonDto>.Fail(new Error(ErrorCodes.NotFound, "Les niet gevonden."));

        var dtos = await BuildDtosAsync([match], ct);
        return Result<StudentLessonDto>.Ok(dtos[0]);
    }

    private async Task<List<StudentLessonDto>> BuildDtosAsync(
        List<ScheduleAssignment> assignments, CancellationToken ct)
    {
        if (assignments.Count == 0) return [];

        var trainerIds = assignments
            .Select(a => a.WeeklyTemplateEntry.TrainerId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var trainerNames = await userLookup.GetUserNamesByIdsAsync(trainerIds, ct);

        var enrollmentIds = assignments
            .SelectMany(a => a.EnrollmentGroupId.HasValue && a.EnrollmentGroup is not null
                ? a.EnrollmentGroup.Members.Select(m => m.Id)
                : a.EnrollmentId.HasValue ? [a.EnrollmentId.Value] : Enumerable.Empty<Guid>())
            .Distinct()
            .ToList();

        var paymentStatus = await paymentRepo.GetLatestStatusByEnrollmentIdsAsync(enrollmentIds, ct);

        return assignments
            .OrderBy(a => a.WeeklyTemplateEntry.DayOfWeek)
            .ThenBy(a => a.WeeklyTemplateEntry.StartTime)
            .Select(a =>
            {
                var slot = a.WeeklyTemplateEntry;
                var series = a.LessonSerie;
                var isGroup = a.EnrollmentGroupId.HasValue && a.EnrollmentGroup is not null;
                var size = isGroup ? a.EnrollmentGroup!.Members.Count : 1;

                // Use the current student's own enrollment id for payment lookup when possible.
                Guid? enrId = a.EnrollmentId
                    ?? a.EnrollmentGroup?.Members.FirstOrDefault()?.Id;

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
                    IsGroup = isGroup,
                    GroupSize = size,
                    Price = series.Price * size,
                    PaymentStatus = enrId.HasValue && paymentStatus.TryGetValue(enrId.Value, out var s)
                        ? s.ToString()
                        : null,
                };
            })
            .ToList();
    }
}
