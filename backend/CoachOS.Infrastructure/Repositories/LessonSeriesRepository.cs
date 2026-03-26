using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class LessonSeriesRepository(ApplicationDbContext context) : ILessonSeriesRepository
{
    public async Task<LessonSeries?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .Include(ls => ls.TennisClub)
            .Include(ls => ls.Lessons)
            .FirstOrDefaultAsync(ls => ls.Id == id && ls.OrganizationId == organizationId, ct);
    }

    public async Task<LessonSeries?> GetByIdWithEnrollmentsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .Include(ls => ls.Lessons)
            .Include(ls => ls.Enrollments)
            .FirstOrDefaultAsync(ls => ls.Id == id && ls.OrganizationId == organizationId, ct);
    }

    public async Task<IReadOnlyList<LessonSeries>> GetByOrganizationAsync(
        Guid organizationId, Guid? trainerId = null, CancellationToken ct = default)
    {
        IQueryable<LessonSeries> query = context.LessonSeries
            .AsNoTracking()
            .Include(ls => ls.TennisClub)
            .Where(ls => ls.OrganizationId == organizationId);

        if (trainerId.HasValue)
            query = query.Where(ls => ls.TrainerId == trainerId.Value);

        return await query.OrderBy(ls => ls.StartDate).ToListAsync(ct);
    }

    public async Task AddAsync(LessonSeries series, CancellationToken ct = default)
    {
        await context.LessonSeries.AddAsync(series, ct);
    }

    public Task UpdateAsync(LessonSeries series, CancellationToken ct = default)
    {
        context.LessonSeries.Update(series);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(LessonSeries series, CancellationToken ct = default)
    {
        context.LessonSeries.Remove(series);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .AsNoTracking()
            .AnyAsync(ls => ls.Id == id && ls.OrganizationId == organizationId, ct);
    }

    public async Task<bool> AnyByTennisClubAsync(Guid tennisClubId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .AsNoTracking()
            .AnyAsync(ls => ls.TennisClubId == tennisClubId, ct);
    }
}
