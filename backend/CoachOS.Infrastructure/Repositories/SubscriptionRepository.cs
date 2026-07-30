using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class SubscriptionRepository(ApplicationDbContext db) : ISubscriptionRepository
{
    public async Task<Subscription?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await db.Subscriptions
            .IgnoreQueryFilters() // middleware runs before the tenant filter is meaningful; scope explicitly
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
}
