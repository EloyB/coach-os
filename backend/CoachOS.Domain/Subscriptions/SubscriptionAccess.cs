using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Subscriptions;

/// <summary>
/// Single source of truth for whether an organisation may use the app,
/// based purely on its subscription state. Used by the access middleware
/// and covered directly by unit tests.
/// </summary>
public static class SubscriptionAccess
{
    public static bool HasAppAccess(
        SubscriptionStatus status,
        DateTime? trialEndsAt,
        DateTime? currentPeriodEnd,
        DateTime utcNow) =>
        status switch
        {
            SubscriptionStatus.Trialing => trialEndsAt is { } end && end > utcNow,
            SubscriptionStatus.Active => currentPeriodEnd is { } end && end > utcNow,
            // PastDue keeps access until the grace period (encoded as CurrentPeriodEnd) elapses.
            SubscriptionStatus.PastDue => currentPeriodEnd is { } end && end > utcNow,
            _ => false,
        };
}
