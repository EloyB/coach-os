using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class EnrollmentGroupRepository(ApplicationDbContext context) : IEnrollmentGroupRepository
{
    public async Task<List<EnrollmentGroup>> GetBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.EnrollmentGroups
            .AsNoTracking()
            .Include(g => g.Members)
            .Where(g => g.LessonSerieId == lessonSerieId && g.OrganizationId == organizationId)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);
    }

    public async Task<EnrollmentGroup?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.EnrollmentGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == organizationId, ct);
    }

    public async Task<int> CountBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.EnrollmentGroups
            .AsNoTracking()
            .CountAsync(g => g.LessonSerieId == lessonSerieId && g.OrganizationId == organizationId, ct);
    }

    public async Task AddAsync(EnrollmentGroup group, CancellationToken ct = default)
    {
        await context.EnrollmentGroups.AddAsync(group, ct);
    }

    public void Delete(EnrollmentGroup group)
    {
        context.EnrollmentGroups.Remove(group);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
