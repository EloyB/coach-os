using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class CampRepository(ApplicationDbContext db) : ICampRepository
{
    public async Task<IReadOnlyList<Camp>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await db.Camps
            .AsNoTracking()
            .Include(c => c.Days)
            .Include(c => c.TennisClub)
            .Where(c => c.OrganizationId == organizationId && c.IsActive)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(ct);

    public async Task<Camp?> GetByIdWithDetailsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
        => await db.Camps
            .Include(c => c.Days).ThenInclude(d => d.TrainerAssignments)
            .Include(c => c.EnrollmentForm!).ThenInclude(f => f.Fields)
            .Include(c => c.TennisClub)
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.IsActive, ct);

    public async Task<Camp?> GetByIdPublicAsync(Guid id, CancellationToken ct = default)
        => await db.Camps
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(c => c.Days).ThenInclude(d => d.TrainerAssignments)
            .Include(c => c.TennisClub)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);

    public async Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
        => await db.Camps.AnyAsync(c => c.Id == id && c.OrganizationId == organizationId && c.IsActive, ct);

    public async Task AddAsync(Camp camp, CancellationToken ct = default)
        => await db.Camps.AddAsync(camp, ct);

    public void Remove(Camp camp) => db.Camps.Remove(camp);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
