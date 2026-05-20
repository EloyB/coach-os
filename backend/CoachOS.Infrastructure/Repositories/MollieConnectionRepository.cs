using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class MollieConnectionRepository(ApplicationDbContext context) : IMollieConnectionRepository
{
    public async Task<MollieConnection?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await context.MollieConnections
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId, ct);
    }

    public async Task<MollieConnection?> GetByOrganizationReadOnlyAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await context.MollieConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId, ct);
    }

    public async Task AddAsync(MollieConnection connection, CancellationToken ct = default)
    {
        await context.MollieConnections.AddAsync(connection, ct);
    }

    public async Task DeleteByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        await context.MollieConnections
            .Where(c => c.OrganizationId == organizationId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
