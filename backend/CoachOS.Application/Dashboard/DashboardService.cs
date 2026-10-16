using CoachOS.Application.Abstractions;
using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
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
    IAssignmentConfirmationTokenRepository confirmationTokenRepo,
    IScheduleAssignmentRepository assignmentRepo,
    ILessonInvitationRepository invitationRepo,
    TimeProvider timeProvider) : IDashboardService
{
    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var allSeries =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, null, Array.Empty<Guid>(), ct);

        var activeSeriesCount = allSeries.Count(s => s.IsActive);

        // Lessons this week
        var today = timeProvider.GetBrusselsToday();
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

    /// <summary>Maximale bereikgrootte: een maandkalender (6 weken) past er ruim in.</summary>
    private const int MaxMyLessonsRangeDays = 62;

    public async Task<Result<List<MyLessonDto>>> GetMyLessonsAsync(
        Guid organizationId, Guid trainerId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from)
            return Result<List<MyLessonDto>>.Fail(
                new Error(ErrorCodes.Validation, "De einddatum mag niet voor de begindatum liggen."));

        if (to.DayNumber - from.DayNumber > MaxMyLessonsRangeDays)
            return Result<List<MyLessonDto>>.Fail(
                new Error(ErrorCodes.Validation, $"Het datumbereik mag maximaal {MaxMyLessonsRangeDays} dagen zijn."));

        List<Lesson> lessons =
            await lessonRepo.GetByTrainerAndDateRangeAsync(trainerId, organizationId, from, to, ct);

        List<Guid> seriesIds = lessons
            .Where(l => l.LessonSerieId.HasValue)
            .Select(l => l.LessonSerieId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, int> enrollmentCounts = seriesIds.Count > 0
            ? await enrollmentRepo.CountActiveBySeriesIdsAsync(seriesIds, ct)
            : [];

        List<MyLessonDto> dtos = lessons.Select(l => new MyLessonDto
        {
            Id = l.Id,
            SeriesId = l.LessonSerieId,
            SeriesName = l.LessonSerie?.Name,
            Date = l.Date.ToString("yyyy-MM-dd"),
            StartTime = l.StartTime.ToString("HH:mm"),
            EndTime = l.EndTime.ToString("HH:mm"),
            CourtName = l.CourtName,
            // Reeks-lessen: club via de reeks (dubbele opslag vermijden); losse lessen: eigen club.
            TennisClubName = l.LessonSerie?.TennisClub?.Name ?? l.TennisClub?.Name,
            EnrolledCount = l.LessonSerieId.HasValue
                ? enrollmentCounts.GetValueOrDefault(l.LessonSerieId.Value, 0)
                : null,
        }).ToList();

        return Result<List<MyLessonDto>>.Ok(dtos);
    }

    public async Task<Result<MyLessonDetailDto>> GetMyLessonAsync(
        Guid organizationId, Guid trainerId, Guid lessonId, CancellationToken ct = default)
    {
        Lesson? lesson = await lessonRepo.GetForTrainerAsync(lessonId, trainerId, organizationId, ct);
        if (lesson is null)
            return Result<MyLessonDetailDto>.Fail(new Error(ErrorCodes.NotFound, "Les niet gevonden."));

        List<MyLessonParticipantDto> participants;

        if (lesson.LessonSerieId is null)
        {
            IReadOnlyList<LessonInvitation> invitations =
                await invitationRepo.GetByLessonAsync(lesson.Id, organizationId, ct);
            participants = invitations
                .OrderBy(i => i.InvitationSentAt)
                .Select(i => new MyLessonParticipantDto
                {
                    Name = string.IsNullOrWhiteSpace(i.FirstName) ? i.Email : i.FirstName!,
                    Status = i.Status switch
                    {
                        LessonInvitationStatus.Accepted => "accepted",
                        LessonInvitationStatus.Declined => "declined",
                        _ => "pending",
                    },
                })
                .ToList();
        }
        else
        {
            // De reeks is ingepland (anders zou GetForTrainerAsync de les niet teruggeven):
            // toon enkel wie in het weekslot van déze les is ingepland.
            List<ScheduleAssignment> assignments =
                await assignmentRepo.GetBySeriesAsync(lesson.LessonSerieId.Value, organizationId, ct);

            participants = assignments
                .Where(a => a.WeeklyTemplateEntryId == lesson.WeeklyTemplateEntryId
                    && a.Status != ScheduleAssignmentStatus.Declined)
                .SelectMany(a => AssignedPeople(a)
                    .Select(e => new MyLessonParticipantDto
                    {
                        Name = e.StudentName,
                        Status = a.Status switch
                        {
                            ScheduleAssignmentStatus.Confirmed => "confirmed",
                            ScheduleAssignmentStatus.AwaitingConfirmation => "awaiting",
                            _ => "proposed",
                        },
                    }))
                .DistinctBy(p => p.Name)
                .OrderBy(p => p.Name)
                .ToList();
        }

        TennisClub? club = lesson.LessonSerie?.TennisClub ?? lesson.TennisClub;

        MyLessonDetailDto dto = new()
        {
            Id = lesson.Id,
            SeriesId = lesson.LessonSerieId,
            SeriesName = lesson.LessonSerie?.Name,
            Date = lesson.Date.ToString("yyyy-MM-dd"),
            StartTime = lesson.StartTime.ToString("HH:mm"),
            EndTime = lesson.EndTime.ToString("HH:mm"),
            DurationMinutes = (int)(lesson.EndTime.ToTimeSpan() - lesson.StartTime.ToTimeSpan()).TotalMinutes,
            CourtName = lesson.CourtName,
            TennisClubName = club?.Name,
            TennisClubAddress = club?.Address,
            Level = (int?)(lesson.Level ?? lesson.LessonSerie?.Level),
            Notes = lesson.Notes,
            MaxParticipants = lesson.MaxStudents,
            Participants = participants,
        };

        return Result<MyLessonDetailDto>.Ok(dto);
    }

    /// <summary>De spelers achter een toewijzing: de inschrijving zelf, of alle leden van de groep.</summary>
    private static IEnumerable<Enrollment> AssignedPeople(ScheduleAssignment assignment)
    {
        if (assignment.Enrollment is not null)
            return IsActiveEnrollment(assignment.Enrollment) ? [assignment.Enrollment] : [];

        return assignment.EnrollmentGroup?.Members.Where(IsActiveEnrollment) ?? [];
    }

    private static bool IsActiveEnrollment(Enrollment e) =>
        e.Status is not (EnrollmentStatus.Cancelled or EnrollmentStatus.Waitlisted);

    public async Task<Result<InboxDto>> GetInboxAsync(
        Guid organizationId, int limit = 10, CancellationToken ct = default)
    {
        List<InboxItemDto> items = [];
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

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
                RefType = "Series",
                RefId = token.ScheduleAssignment?.LessonSerieId ?? token.EnrollmentId,
                Title = studentName,
                Body = "heeft nog niet bevestigd",
                Meta = meta,
                Severity = severity,
                CreatedAt = token.CreatedAt,
            });
        }

        // 2. Underbooked series
        IReadOnlyList<LessonSerieEntity> allSeries =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, null, Array.Empty<Guid>(), ct);

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

        // OccupancyPct: enrolled / maxRegistrations for series active in each week
        IReadOnlyList<LessonSerieEntity> allSeries =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, null, Array.Empty<Guid>(), ct);
        List<LessonSerieEntity> metricalSeries = allSeries
            .Where(s => s.IsActive && s.MaxRegistrations.HasValue && s.MaxRegistrations.Value > 0)
            .ToList();

        List<WeekMetricDto> occupancy;
        if (metricalSeries.Count > 0)
        {
            List<Guid> seriesIds = metricalSeries.Select(s => s.Id).ToList();
            Dictionary<Guid, int> enrollmentCounts =
                await enrollmentRepo.CountActiveBySeriesIdsAsync(seriesIds, ct);

            DateOnly today = timeProvider.GetBrusselsToday();
            int dayOfWeekOffset = ((int)today.DayOfWeek + 6) % 7;
            DateOnly currentWeekStart = today.AddDays(-dayOfWeekOffset);

            occupancy = [];
            for (int i = 0; i < weeks; i++)
            {
                DateOnly weekStart = currentWeekStart.AddDays(-7 * (weeks - 1 - i));
                DateOnly weekEnd = weekStart.AddDays(6);
                int isoYear = System.Globalization.ISOWeek.GetYear(weekStart.ToDateTime(TimeOnly.MinValue));
                int isoWeek = System.Globalization.ISOWeek.GetWeekOfYear(weekStart.ToDateTime(TimeOnly.MinValue));
                string weekLabel = $"{isoYear}-W{isoWeek:D2}";

                List<LessonSerieEntity> weekSeries = metricalSeries
                    .Where(s => s.StartDate <= weekEnd && s.EndDate >= weekStart)
                    .ToList();

                int totalEnrolled = weekSeries.Sum(s => enrollmentCounts.GetValueOrDefault(s.Id, 0));
                int totalCapacity = weekSeries.Sum(s => s.MaxRegistrations!.Value);
                int pct = totalCapacity > 0
                    ? (int)Math.Round((double)totalEnrolled / totalCapacity * 100)
                    : 0;
                occupancy.Add(new WeekMetricDto { Week = weekLabel, Value = pct });
            }
        }
        else
        {
            occupancy = lessonCounts.Keys
                .Select(week => new WeekMetricDto { Week = week, Value = 0 })
                .ToList();
        }

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
