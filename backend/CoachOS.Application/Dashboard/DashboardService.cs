using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using LessonSerieEntity = CoachOS.Domain.Entities.LessonSerie;

namespace CoachOS.Application.Dashboard;

public class DashboardService(
    ILessonSerieRepository lessonSeriesRepo,
    ILessonRepository lessonRepo,
    IEnrollmentRepository enrollmentRepo,
    ITennisClubRepository tennisClubRepo,
    IUserLookupService userLookup,
    IAssignmentConfirmationTokenRepository confirmationTokenRepo) : IDashboardService
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

        // Trainer count — enkel rol Trainer, admins uitgesloten.
        var activeTrainerCount = await userLookup.CountActiveTrainersAsync(organizationId, ct);

        // Tennis club count
        var clubs =
            await tennisClubRepo.GetByOrganizationAsync(organizationId, ct);

        // Build upcoming lesson DTOs
        var trainerIds = upcomingLessons
            .Where(l => l.TrainerId.HasValue)
            .Select(l => l.TrainerId!.Value)
            .Distinct()
            .ToList();
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
            TrainerName = l.TrainerId.HasValue
                ? trainerNames.GetValueOrDefault(l.TrainerId.Value, string.Empty)
                : string.Empty,
        }).ToList();

        int lessonsToday = await lessonRepo.CountByOrganizationAndDateRangeAsync(
            organizationId, today, today, ct);

        DashboardSummaryDto summary = new()
        {
            ActiveSeriesCount = activeSeriesCount,
            LessonsTodayCount = lessonsToday,
            LessonsThisWeekCount = lessonsThisWeek,
            TotalEnrollmentCount = totalEnrollments,
            ActiveTrainerCount = activeTrainerCount,
            TennisClubCount = clubs.Count,
            UpcomingLessons = upcomingDtos,
        };

        return Result<DashboardSummaryDto>.Ok(summary);
    }

    public async Task<Result<InboxDto>> GetInboxAsync(
        Guid organizationId, int limit = 10, CancellationToken ct = default)
    {
        List<InboxItemDto> items = [];
        DateTime now = DateTime.UtcNow;

        // 1. Pending confirmations
        List<AssignmentConfirmationToken> pendingTokens =
            await confirmationTokenRepo.GetPendingByOrganizationAsync(organizationId, ct);

        foreach (AssignmentConfirmationToken token in pendingTokens)
        {
            TimeSpan timeRemaining = token.ExpiresAt - now;
            string severity = timeRemaining.TotalHours < 12 ? "urgent" : "warn";
            string studentName = token.Enrollment?.StudentName ?? "Onbekend";
            string meta = timeRemaining.TotalHours > 0
                ? timeRemaining.TotalHours >= 24
                    ? $"{(int)timeRemaining.TotalDays}d {timeRemaining.Hours}u resterend"
                    : $"{(int)timeRemaining.TotalHours}u {timeRemaining.Minutes}m resterend"
                : "verlopen";

            items.Add(new InboxItemDto
            {
                Type = "confirmation_pending",
                RefType = "Student",
                RefId = token.EnrollmentId,
                Title = studentName,
                Body = "heeft nog niet bevestigd",
                Meta = meta,
                Severity = severity,
                CreatedAt = token.CreatedAt,
            });
        }

        // 2. Underbooked series
        IReadOnlyList<LessonSerieEntity> allSeries =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, ct: ct);

        List<LessonSerieEntity> activeSeries = allSeries
            .Where(s => s.IsActive && s.MaxRegistrations.HasValue && s.MaxRegistrations.Value > 0)
            .ToList();

        if (activeSeries.Count > 0)
        {
            List<Guid> seriesIds = activeSeries.Select(s => s.Id).ToList();
            Dictionary<Guid, int> enrollmentCounts =
                await enrollmentRepo.CountActiveBySeriesIdsAsync(seriesIds, ct);

            foreach (LessonSerieEntity series in activeSeries)
            {
                int enrolled = enrollmentCounts.GetValueOrDefault(series.Id, 0);
                int max = series.MaxRegistrations!.Value;
                double ratio = (double)enrolled / max;

                if (ratio < 0.6)
                {
                    int emptySlots = max - enrolled;
                    decimal estimatedRevenueLoss = emptySlots * series.Price;

                    items.Add(new InboxItemDto
                    {
                        Type = "series_underbooked",
                        RefType = "Series",
                        RefId = series.Id,
                        Title = series.Name,
                        Body = $"{emptySlots} plaatsen leeg",
                        Meta = $"~\u20AC{estimatedRevenueLoss:F0} omzet gemist",
                        Severity = "warn",
                        CreatedAt = series.CreatedAt,
                    });
                }
            }
        }

        // Sort: urgent first, then by CreatedAt desc
        List<InboxItemDto> sorted = items
            .OrderByDescending(i => i.Severity == "urgent" ? 1 : 0)
            .ThenByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToList();

        InboxDto inbox = new()
        {
            Items = sorted,
            UpdatedAt = now,
        };

        return Result<InboxDto>.Ok(inbox);
    }

    public async Task<Result<DashboardMetricsDto>> GetMetricsAsync(
        Guid organizationId, int weeks = 7, CancellationToken ct = default)
    {
        Dictionary<string, int> lessonCounts =
            await lessonRepo.CountByOrganizationWeeksAsync(organizationId, weeks, ct);

        List<WeekMetricDto> lessons = lessonCounts
            .Select(kv => new WeekMetricDto { Week = kv.Key, Value = kv.Value })
            .ToList();

        // OccupancyPct: stub as 0 for now
        List<WeekMetricDto> occupancy = lessonCounts.Keys
            .Select(week => new WeekMetricDto { Week = week, Value = 0 })
            .ToList();

        // OutstandingCents: stub as 0 for now
        List<WeekMetricDto> outstanding = lessonCounts.Keys
            .Select(week => new WeekMetricDto { Week = week, Value = 0 })
            .ToList();

        DashboardMetricsDto metrics = new()
        {
            Lessons = lessons,
            OccupancyPct = occupancy,
            OutstandingCents = outstanding,
        };

        return Result<DashboardMetricsDto>.Ok(metrics);
    }
}
