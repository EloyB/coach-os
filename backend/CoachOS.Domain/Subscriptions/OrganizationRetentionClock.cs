using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Subscriptions;

/// <summary>Only unambiguous actual service end dates. Active renewal is never historical trial expiry.</summary>
public static class OrganizationRetentionClock
{
    public static DateOnly? EndedOn(Subscription? subscription) => subscription?.Status switch
    {
        SubscriptionStatus.Trialing or SubscriptionStatus.Locked when subscription.Plan is null &&
            subscription.CurrentPeriodEnd is null && subscription.MollieSubscriptionId is null => subscription.TrialEndsAt,
        SubscriptionStatus.Cancelled when subscription.CancelAt is not null &&
            subscription.CancelAt == subscription.CurrentPeriodEnd => subscription.CurrentPeriodEnd,
        _ => null
    };
}
