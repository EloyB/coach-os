using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class OrganizationSettingsRepository(ApplicationDbContext context) : IOrganizationSettingsRepository
{
    public async Task<OrganizationSettings?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await context.OrganizationSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
    }

    public async Task<OrganizationSettings?> GetByOrganizationReadOnlyAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await context.OrganizationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
    }

    public async Task AddAsync(OrganizationSettings settings, CancellationToken ct = default)
    {
        await context.OrganizationSettings.AddAsync(settings, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
