using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Dashboard;

public class DashboardService(
    ILessonSeriesRepository lessonSeriesRepo,
    ILessonRepository lessonRepo,
    IEnrollmentRepository enrollmentRepo,
    ITennisClubRepository tennisClubRepo,
    IUserLookupService userLookup) : IDashboardService
{
    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        IReadOnlyList<Domain.Entities.LessonSeries> allSeries =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, ct: ct);

        int activeSeriesCount = allSeries.Count(s => s.IsActive);

        // Lessons this week
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        int dayOfWeek = ((int)today.DayOfWeek + 6) % 7; // Monday=0
        DateOnly weekStart = today.AddDays(-dayOfWeek);
        DateOnly weekEnd = weekStart.AddDays(6);

        List<Domain.Entities.Lesson> upcomingLessons = await lessonRepo.GetUpcomingByOrganizationAsync(
            organizationId, today, 5, ct);

        int lessonsThisWeek = await lessonRepo.CountByOrganizationAndDateRangeAsync(
            organizationId, weekStart, weekEnd, ct);

        // Enrollment count
        int totalEnrollments = await enrollmentRepo.CountActiveByOrganizationAsync(organizationId, ct);

        // Trainer count
        List<(Guid Id, string FullName)> trainers =
            await userLookup.GetOrganizationMembersAsync(organizationId, ct);
        int activeTrainerCount = trainers.Count;

        // Tennis club count
        IReadOnlyList<Domain.Entities.TennisClub> clubs =
            await tennisClubRepo.GetByOrganizationAsync(organizationId, ct);

        // Build upcoming lesson DTOs
        List<Guid> trainerIds = upcomingLessons.Select(l => l.TrainerId).Distinct().ToList();
        Dictionary<Guid, string> trainerNames =
            await userLookup.GetUserNamesByIdsAsync(trainerIds, ct);

        List<UpcomingLessonDto> upcomingDtos = upcomingLessons.Select(l => new UpcomingLessonDto
        {
            Id = l.Id,
            SeriesName = l.LessonSeries?.Name ?? string.Empty,
            Date = l.Date.ToString("yyyy-MM-dd"),
            StartTime = l.StartTime.ToString("HH:mm"),
            EndTime = l.EndTime.ToString("HH:mm"),
            CourtName = l.CourtName,
            TrainerName = trainerNames.GetValueOrDefault(l.TrainerId, string.Empty),
        }).ToList();

        DashboardSummaryDto summary = new()
        {
            ActiveSeriesCount = activeSeriesCount,
            LessonsThisWeekCount = lessonsThisWeek,
            TotalEnrollmentCount = totalEnrollments,
            ActiveTrainerCount = activeTrainerCount,
            TennisClubCount = clubs.Count,
            UpcomingLessons = upcomingDtos,
        };

        return Result<DashboardSummaryDto>.Ok(summary);
    }
}
