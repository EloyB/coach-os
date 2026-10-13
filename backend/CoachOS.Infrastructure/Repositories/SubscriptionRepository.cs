using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class SubscriptionRepository(ApplicationDbContext context) : ISubscriptionRepository
{
    public async Task<Subscription?> GetByOrganizationAsync(
        Guid organizationId, bool tracked = false, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: superadmin beheert cross-org en heeft geen tenant-context
        // die matcht met de doel-organisatie. Scoping gebeurt expliciet op organizationId.
        IQueryable<Subscription> query = context.Subscriptions.IgnoreQueryFilters();
        if (!tracked)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
    }

    public async Task AddAsync(Subscription subscription, CancellationToken ct = default)
        => await context.Subscriptions.AddAsync(subscription, ct);
}
