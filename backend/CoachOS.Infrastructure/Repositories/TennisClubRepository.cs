using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class TennisClubRepository(ApplicationDbContext context) : ITennisClubRepository
{
    public async Task<TennisClub?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.TennisClubs
            .FirstOrDefaultAsync(tc => tc.Id == id && tc.OrganizationId == organizationId, ct);
    }

    public async Task<IReadOnlyList<TennisClub>> GetByOrganizationAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        return await context.TennisClubs
            .AsNoTracking()
            .Where(tc => tc.OrganizationId == organizationId)
            .OrderBy(tc => tc.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(TennisClub club, CancellationToken ct = default)
    {
        await context.TennisClubs.AddAsync(club, ct);
    }

    public Task DeleteAsync(TennisClub club, CancellationToken ct = default)
    {
        context.TennisClubs.Remove(club);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.TennisClubs
            .AsNoTracking()
            .AnyAsync(tc => tc.Id == id && tc.OrganizationId == organizationId, ct);
    }
}
