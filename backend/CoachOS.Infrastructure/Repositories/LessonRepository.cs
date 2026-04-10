using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class LessonRepository(ApplicationDbContext context) : ILessonRepository
{
    public async Task<Lesson?> GetByIdWithEnrollmentsAsync(
        Guid lessonId, Guid seriesId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.Lessons
            .Include(l => l.Enrollments)
            .FirstOrDefaultAsync(l =>
                l.Id == lessonId &&
                l.LessonSeriesId == seriesId &&
                l.OrganizationId == organizationId, ct);
    }

    public async Task<int> CountBySeriesIdAsync(Guid seriesId, CancellationToken ct = default)
    {
        return await context.Lessons
            .AsNoTracking()
            .CountAsync(l => l.LessonSeriesId == seriesId, ct);
    }

    public async Task<Dictionary<Guid, int>> GetLessonCountsBySeriesIdsAsync(
        IEnumerable<Guid> seriesIds, CancellationToken ct = default)
    {
        List<Guid> ids = seriesIds as List<Guid> ?? seriesIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.Lessons
            .AsNoTracking()
            .Where(l => l.LessonSeriesId.HasValue && ids.Contains(l.LessonSeriesId.Value))
            .GroupBy(l => l.LessonSeriesId!.Value)
            .Select(g => new { SeriesId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SeriesId, x => x.Count, ct);
    }

    public async Task<List<Lesson>> GetUpcomingByOrganizationAsync(
        Guid organizationId, DateOnly fromDate, int limit, CancellationToken ct = default)
    {
        return await context.Lessons
            .AsNoTracking()
            .Include(l => l.LessonSeries)
            .Where(l => l.OrganizationId == organizationId && l.Date >= fromDate && !l.IsCancelled)
            .OrderBy(l => l.Date)
            .ThenBy(l => l.StartTime)
            .Take(limit)
            .ToListAsync(ct);
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
}
