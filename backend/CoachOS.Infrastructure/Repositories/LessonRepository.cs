using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class LessonRepository(ApplicationDbContext context) : ILessonRepository
{
    public async Task<Lesson?> GetByIdAsync(
        Guid lessonId, Guid seriesId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.Lessons
            .FirstOrDefaultAsync(l =>
                l.Id == lessonId &&
                l.LessonSerieId == seriesId &&
                l.OrganizationId == organizationId, ct);
    }

    public async Task<Lesson?> GetByIdWithEnrollmentsAsync(
        Guid lessonId, Guid seriesId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.Lessons
            .Include(l => l.Enrollments)
            .FirstOrDefaultAsync(l =>
                l.Id == lessonId &&
                l.LessonSerieId == seriesId &&
                l.OrganizationId == organizationId, ct);
    }

    public async Task<int> CountBySeriesIdAsync(Guid seriesId, CancellationToken ct = default)
    {
        return await context.Lessons
            .AsNoTracking()
            .CountAsync(l => l.LessonSerieId == seriesId, ct);
    }

    public async Task<Dictionary<Guid, int>> GetLessonCountsBySeriesIdsAsync(
        IEnumerable<Guid> seriesIds, CancellationToken ct = default)
    {
        var ids = seriesIds as List<Guid> ?? seriesIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.Lessons
            .AsNoTracking()
            .Where(l => l.LessonSerieId.HasValue && ids.Contains(l.LessonSerieId.Value))
            .GroupBy(l => l.LessonSerieId!.Value)
            .Select(g => new { SeriesId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SeriesId, x => x.Count, ct);
    }

    public async Task<List<Lesson>> GetUpcomingByOrganizationAsync(
        Guid organizationId, DateOnly fromDate, int limit, CancellationToken ct = default)
    {
        return await context.Lessons
            .AsNoTracking()
            .Include(l => l.LessonSerie)
            .Where(l => l.OrganizationId == organizationId && l.Date >= fromDate && !l.IsCancelled)
            .OrderBy(l => l.Date)
            .ThenBy(l => l.StartTime)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<Lesson?> FindTrainerConflictAsync(
        Guid trainerId, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        Guid? excludeLessonId = null, CancellationToken ct = default)
    {
        return await context.Lessons
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(l => l.LessonSerie)
            .Where(l =>
                l.TrainerId == trainerId
                && l.Date == date
                && !l.IsCancelled
                && l.StartTime < endTime
                && l.EndTime > startTime
                && (excludeLessonId == null || l.Id != excludeLessonId))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CountByOrganizationAndDateRangeAsync(
        Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        return await context.Lessons
            .AsNoTracking()
            .CountAsync(l => l.OrganizationId == organizationId
                && l.Date >= from && l.Date <= to
                && !l.IsCancelled, ct);
    }

    public async Task<Dictionary<string, int>> CountByOrganizationWeeksAsync(
        Guid organizationId, int weeks, CancellationToken ct = default)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        int dayOfWeek = ((int)today.DayOfWeek + 6) % 7; // Monday=0
        DateOnly currentWeekStart = today.AddDays(-dayOfWeek);
        DateOnly rangeStart = currentWeekStart.AddDays(-7 * (weeks - 1));

        List<Lesson> lessons = await context.Lessons
            .AsNoTracking()
            .Where(l => l.OrganizationId == organizationId
                && l.Date >= rangeStart
                && l.Date <= currentWeekStart.AddDays(6)
                && !l.IsCancelled)
            .ToListAsync(ct);

        Dictionary<string, int> result = new();
        for (int i = 0; i < weeks; i++)
        {
            DateOnly weekStart = currentWeekStart.AddDays(-7 * (weeks - 1 - i));
            DateOnly weekEnd = weekStart.AddDays(6);

            int isoYear = System.Globalization.ISOWeek.GetYear(weekStart.ToDateTime(TimeOnly.MinValue));
            int isoWeek = System.Globalization.ISOWeek.GetWeekOfYear(weekStart.ToDateTime(TimeOnly.MinValue));
            string weekLabel = $"{isoYear}-W{isoWeek:D2}";

            int count = lessons.Count(l => l.Date >= weekStart && l.Date <= weekEnd);
            result[weekLabel] = count;
        }

        return result;
    }

    public async Task AddAsync(Lesson lesson, CancellationToken ct = default)
    {
        await context.Lessons.AddAsync(lesson, ct);
    }

    public Task DeleteAsync(Lesson lesson, CancellationToken ct = default)
    {
        context.Lessons.Remove(lesson);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<Lesson> lessons, CancellationToken ct = default)
    {
        context.Lessons.RemoveRange(lessons);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task<Lesson?> GetByIdInOrganizationAsync(
        Guid lessonId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.Lessons
            .FirstOrDefaultAsync(l =>
                l.Id == lessonId &&
                l.OrganizationId == organizationId, ct);
    }

    public async Task<IReadOnlyList<Lesson>> GetStandaloneByOrganizationAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        return await context.Lessons
            .AsNoTracking()
            .Where(l => l.OrganizationId == organizationId && l.LessonSerieId == null)
            .OrderBy(l => l.Date)
            .ThenBy(l => l.StartTime)
            .ToListAsync(ct);
    }
}
