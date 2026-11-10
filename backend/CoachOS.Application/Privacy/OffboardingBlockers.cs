using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Models;
using CoachOS.Domain.Subscriptions;

namespace CoachOS.Application.Privacy;

/// <summary>Regels die bepalen of een organisatie verwijderd mag worden.</summary>
public static class OffboardingBlockers
{
    public const string SubscriptionRunning = "subscription_running";
    public const string RetentionPeriod = "retention_period";
    public const string FinancialRecords = "financial_records";

    public static IReadOnlyList<OffboardingBlockerDto> Evaluate(
        Subscription? subscription, OrganizationDataCounts counts, DateOnly today)
    {
        List<OffboardingBlockerDto> blockers = [];
        DateOnly? endedOn = OrganizationRetentionClock.EndedOn(subscription);
        DateOnly? deletableFrom = OrganizationRetentionClock.DeletableFrom(subscription);

        if (endedOn is null || endedOn.Value >= today)
        {
            blockers.Add(new OffboardingBlockerDto(SubscriptionRunning,
                "Het abonnement of de proef loopt nog.", endedOn));
        }
        else if (deletableFrom is DateOnly from && today < from)
        {
            blockers.Add(new OffboardingBlockerDto(RetentionPeriod,
                $"De bewaartermijn van {OrganizationRetentionClock.RetentionDays} dagen loopt nog.", from));
        }

        if (counts.SubscriptionPayments > 0)
        {
            blockers.Add(new OffboardingBlockerDto(FinancialRecords,
                "Financiële gegevens: wettelijke bewaarplicht, handmatig afhandelen."));
        }

        return blockers;
    }
}
