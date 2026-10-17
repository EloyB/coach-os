using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Subscriptions;

/// <summary>Berekening van abonnementsperiodes (inclusief begin- en einddag).</summary>
public static class BillingPeriods
{
    /// <summary>
    /// Eerste betaalde dag: de dag na het einde van een lopende proef (geen verloren gratis
    /// dagen), anders vandaag.
    /// </summary>
    public static DateOnly FirstPeriodStart(Subscription subscription, DateOnly today)
    {
        if (subscription.Status == SubscriptionStatus.Trialing
            && subscription.TrialEndsAt is DateOnly trialEnd
            && trialEnd >= today)
        {
            return trialEnd.AddDays(1);
        }

        return today;
    }

    /// <summary>Laatste dag van een periode die op <paramref name="start"/> begint.</summary>
    public static DateOnly PeriodEnd(DateOnly start, BillingInterval interval)
        => (interval == BillingInterval.Yearly ? start.AddYears(1) : start.AddMonths(1)).AddDays(-1);

    /// <summary>Interval in de syntax van de Mollie Subscriptions API.</summary>
    public static string MollieInterval(BillingInterval interval)
        => interval == BillingInterval.Yearly ? "12 months" : "1 month";
}
