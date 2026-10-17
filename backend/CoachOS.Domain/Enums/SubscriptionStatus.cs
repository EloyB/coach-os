namespace CoachOS.Domain.Enums;

public enum SubscriptionStatus
{
    /// <summary>Organisatie van vóór het abonnementsmodel: volledige toegang, geen proef, geen herinneringen.</summary>
    Legacy = 1,

    /// <summary>Gratis proefperiode; loopt af op <c>Subscription.TrialEndsAt</c>.</summary>
    Trialing = 2,

    /// <summary>Betalend abonnement.</summary>
    Active = 3
}
