namespace CoachOS.Application.Subscriptions.DTOs;

/// <summary>Kent een plan + facturatieperiode toe aan een organisatie (upsert).</summary>
public record AssignSubscriptionRequest(
    /// <summary>SubscriptionPlan enum-waarde: 1=Game, 2=Set, 3=Match.</summary>
    int Plan,
    /// <summary>BillingInterval enum-waarde: 1=Monthly, 2=Yearly.</summary>
    int Interval,
    /// <summary>Startdatum (yyyy-MM-dd). Leeg = vandaag (Brusselse datum).</summary>
    string? StartDate);
