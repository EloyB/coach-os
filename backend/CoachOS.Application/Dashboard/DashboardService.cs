using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Dashboard;

public class DashboardService(
    ILessonSerieRepository lessonSeriesRepo,
    ILessonRepository lessonRepo,
    IEnrollmentRepository enrollmentRepo,
    ITennisClubRepository tennisClubRepo,
    IUserLookupService userLookup) : IDashboardService
{
    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var allSeries =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, ct: ct);

        var activeSeriesCount = allSeries.Count(s => s.IsActive);

        // Lessons this week
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dayOfWeek = ((int)today.DayOfWeek + 6) % 7; // Monday=0
        var weekStart = today.AddDays(-dayOfWeek);
        var weekEnd = weekStart.AddDays(6);

        var upcomingLessons = await lessonRepo.GetUpcomingByOrganizationAsync(
            organizationId, today, 5, ct);

        var lessonsThisWeek = await lessonRepo.CountByOrganizationAndDateRangeAsync(
            organizationId, weekStart, weekEnd, ct);

        // Enrollment count
        var totalEnrollments = await enrollmentRepo.CountActiveByOrganizationAsync(organizationId, ct);

        // Trainer count
        var trainers =
            await userLookup.GetOrganizationMembersAsync(organizationId, ct);
        var activeTrainerCount = trainers.Count;

        // Tennis club count
        var clubs =
            await tennisClubRepo.GetByOrganizationAsync(organizationId, ct);

        // Build upcoming lesson DTOs
        var trainerIds = upcomingLessons.Select(l => l.TrainerId).Distinct().ToList();
        var trainerNames =
            await userLookup.GetUserNamesByIdsAsync(trainerIds, ct);

        var upcomingDtos = upcomingLessons.Select(l => new UpcomingLessonDto
        {
            Id = l.Id,
            SeriesName = l.LessonSerie?.Name ?? string.Empty,
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
