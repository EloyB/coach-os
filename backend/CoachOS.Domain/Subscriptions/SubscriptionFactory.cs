using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Subscriptions;

/// <summary>
/// Bouwt een Subscription-entity. Nu enkel de trial; betaalde subscriptions
/// (Mollie) volgen in fase 2.
/// </summary>
public static class SubscriptionFactory
{
    public static Subscription CreateTrial(Guid organizationId, int trialDays, DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Status = SubscriptionStatus.Trialing,
            TrialEndsAt = utcNow.AddDays(trialDays),
        };
}
