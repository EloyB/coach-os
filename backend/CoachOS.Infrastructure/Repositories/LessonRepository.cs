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

    public async Task AddAsync(Lesson lesson, CancellationToken ct = default)
    {
        await context.Lessons.AddAsync(lesson, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Lesson lesson, CancellationToken ct = default)
    {
        context.Lessons.Remove(lesson);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteRangeAsync(IEnumerable<Lesson> lessons, CancellationToken ct = default)
    {
        context.Lessons.RemoveRange(lessons);
        await context.SaveChangesAsync(ct);
    }
}
