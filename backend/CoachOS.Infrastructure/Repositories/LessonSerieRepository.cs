using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class LessonSerieRepository(ApplicationDbContext context) : ILessonSerieRepository
{
    public async Task<LessonSerie?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .Include(ls => ls.TennisClub)
            .Include(ls => ls.Lessons)
            .Include(ls => ls.WeeklyTemplate)
            .FirstOrDefaultAsync(ls => ls.Id == id && ls.OrganizationId == organizationId, ct);
    }

    public async Task<LessonSerie?> GetByIdWithEnrollmentsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .Include(ls => ls.Lessons)
            .Include(ls => ls.Enrollments)
            .Include(ls => ls.WeeklyTemplate)
            .FirstOrDefaultAsync(ls => ls.Id == id && ls.OrganizationId == organizationId, ct);
    }

    public async Task<IReadOnlyList<LessonSerie>> GetByOrganizationAsync(
        Guid organizationId, Guid? trainerId = null, CancellationToken ct = default)
    {
        var query = context.LessonSeries
            .AsNoTracking()
            .Include(ls => ls.TennisClub)
            .Where(ls => ls.OrganizationId == organizationId);

        if (trainerId.HasValue)
            query = query.Where(ls => ls.Lessons.Any(l => l.TrainerId == trainerId.Value));

        return await query.OrderBy(ls => ls.StartDate).ToListAsync(ct);
    }

    public async Task AddAsync(LessonSerie series, CancellationToken ct = default)
    {
        await context.LessonSeries.AddAsync(series, ct);
    }

    public Task UpdateAsync(LessonSerie series, CancellationToken ct = default)
    {
        context.LessonSeries.Update(series);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(LessonSerie series, CancellationToken ct = default)
    {
        context.LessonSeries.Remove(series);
        return Task.CompletedTask;
    }

    public Task DeleteWeeklyTemplateRangeAsync(IEnumerable<WeeklyTemplateEntry> entries, CancellationToken ct = default)
    {
        context.WeeklyTemplateEntries.RemoveRange(entries);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        await context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (context.Database.CurrentTransaction is not null)
            await context.Database.CommitTransactionAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (context.Database.CurrentTransaction is not null)
            await context.Database.RollbackTransactionAsync(ct);
    }

    public async Task<LessonSerie?> GetByIdPublicAsync(Guid id, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .AsNoTracking()
            .Include(ls => ls.Lessons)
            .Include(ls => ls.TennisClub)
            .Include(ls => ls.WeeklyTemplate)
            .FirstOrDefaultAsync(ls => ls.Id == id && ls.IsActive, ct);
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

    public async Task<bool> AnyByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await context.LessonSeries
            .AsNoTracking()
            .AnyAsync(ls => ls.OrganizationId == organizationId, ct);
    }
}
