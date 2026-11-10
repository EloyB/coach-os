using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Subscriptions;

/// <summary>
/// De enige bron voor "wanneer eindigde (of eindigt) de dienst voor deze organisatie" — de start
/// van de bewaartermijn uit de voorwaarden (art. 14) en de verwerkersovereenkomst (art. 10).
/// </summary>
public static class OrganizationRetentionClock
{
    /// <summary>Na het einde blijven de gegevens zoveel dagen bewaard voor ze verwijderd mogen worden.</summary>
    public const int RetentionDays = 90;

    /// <summary>
    /// Laatste dag van de proef of het abonnement; null zolang er geen einde in zicht is (lopend
    /// betalend abonnement, legacy of geen abonnement). Kan in de toekomst liggen (opgezegd,
    /// periode loopt nog).
    /// </summary>
    public static DateOnly? EndedOn(Subscription? subscription) => subscription?.Status switch
    {
        // Proef zonder gekozen plan: eindigt op de laatste proefdag.
        SubscriptionStatus.Trialing when subscription.Plan is null => subscription.TrialEndsAt,
        // Proef stopgezet na opzegging: LockAfterTrialCancellation zet TrialEndsAt op de laatste dag.
        SubscriptionStatus.Locked => subscription.TrialEndsAt,
        // Opgezegd: einde van de periode, of vroeger bij een onmiddellijke stopzetting
        // (CancelAt = vandaag terwijl CurrentPeriodEnd later ligt).
        SubscriptionStatus.Cancelled => Earliest(subscription.CancelAt, subscription.CurrentPeriodEnd),
        _ => null
    };

    /// <summary>Eerste dag waarop de gegevens verwijderd mogen worden; null als er geen einde is.</summary>
    public static DateOnly? DeletableFrom(Subscription? subscription)
        => EndedOn(subscription)?.AddDays(RetentionDays);

    private static DateOnly? Earliest(DateOnly? first, DateOnly? second)
    {
        if (first is null) return second;
        if (second is null) return first;
        return first.Value < second.Value ? first : second;
    }
}
