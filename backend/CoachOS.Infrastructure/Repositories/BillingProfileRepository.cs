using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class BillingProfileRepository(ApplicationDbContext context) : IBillingProfileRepository
{
    public async Task<BillingProfile?> GetByOrganizationAsync(Guid organizationId, bool tracked = false, CancellationToken ct = default)
    {
        IQueryable<BillingProfile> query = context.BillingProfiles.IgnoreQueryFilters();
        if (!tracked)
            query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.OrganizationId == organizationId, ct);
    }

    public async Task AddAsync(BillingProfile profile, CancellationToken ct = default)
        => await context.BillingProfiles.AddAsync(profile, ct);
}
