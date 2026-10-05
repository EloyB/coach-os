using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class SubscriptionRepository(ApplicationDbContext context) : ISubscriptionRepository
{
    public async Task<Subscription?> GetByOrganizationReadOnlyAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await context.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
    }

    public async Task<IReadOnlyList<Subscription>> GetAllTrialingReadOnlyAsync(CancellationToken ct = default)
    {
        // Bewust cross-tenant: de herinneringsjob draait buiten een request en bedient alle organisaties.
        return await context.Subscriptions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(s => s.Organization)
            .Where(s => s.Status == SubscriptionStatus.Trialing
                        && s.TrialEndsAt != null
                        && s.IsActive
                        && s.Organization.IsActive)
            .ToListAsync(ct);
    }

    public async Task<bool> TrySetLastTrialReminderDayAsync(
        Guid subscriptionId, int? expectedCurrent, int? newValue, CancellationToken ct = default)
    {
        int affected = await context.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Id == subscriptionId && s.LastTrialReminderDay == expectedCurrent)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.LastTrialReminderDay, newValue), ct);

        return affected == 1;
    }
}
