using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class LessonSeriePriceRepository(ApplicationDbContext context) : ILessonSeriePriceRepository
{
    public async Task<IReadOnlyList<LessonSeriePrice>> GetBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.LessonSeriePrices
            .AsNoTracking()
            .Where(p => p.LessonSerieId == lessonSerieId && p.OrganizationId == organizationId)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Label)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LessonSeriePrice>> GetBySeriesPublicAsync(
        Guid lessonSerieId, CancellationToken ct = default)
    {
        return await context.LessonSeriePrices
            .AsNoTracking()
            .Where(p => p.LessonSerieId == lessonSerieId)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Label)
            .ToListAsync(ct);
    }

    public async Task ReplaceForSeriesAsync(
        Guid lessonSerieId, Guid organizationId, IEnumerable<LessonSeriePrice> prices,
        CancellationToken ct = default)
    {
        List<LessonSeriePrice> existing = await context.LessonSeriePrices
            .Where(p => p.LessonSerieId == lessonSerieId && p.OrganizationId == organizationId)
            .ToListAsync(ct);

        context.LessonSeriePrices.RemoveRange(existing);
        await context.LessonSeriePrices.AddRangeAsync(prices, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
