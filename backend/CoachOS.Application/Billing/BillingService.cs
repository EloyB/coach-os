using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Subscriptions;

namespace CoachOS.Application.Billing;

public class BillingService(ISubscriptionRepository subscriptions) : IBillingService
{
    public async Task<Result<SubscriptionStatusDto>> GetStatusAsync(Guid organizationId, CancellationToken ct = default)
    {
        Subscription? sub = await subscriptions.GetByOrganizationAsync(organizationId, ct);
        if (sub is null)
            return Result<SubscriptionStatusDto>.Ok(new SubscriptionStatusDto("None", null, null, false));

        DateTime now = DateTime.UtcNow;
        bool hasAccess = SubscriptionAccess.HasAppAccess(sub.Status, sub.TrialEndsAt, sub.CurrentPeriodEnd, now);
        int? daysLeft = sub.TrialEndsAt is { } end
            ? Math.Max(0, (int)Math.Ceiling((end - now).TotalDays))
            : null;

        return Result<SubscriptionStatusDto>.Ok(new SubscriptionStatusDto(
            sub.Status.ToString(), sub.TrialEndsAt, daysLeft, hasAccess));
    }
}
