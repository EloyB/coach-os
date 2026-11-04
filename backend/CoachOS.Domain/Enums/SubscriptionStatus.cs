namespace CoachOS.Domain.Enums;

public enum SubscriptionStatus
{
    /// <summary>Organisatie van vóór het abonnementsmodel: volledige toegang, geen proef, geen herinneringen.</summary>
    Legacy = 1,

    /// <summary>Gratis proefperiode; loopt af op <c>Subscription.TrialEndsAt</c>.</summary>
    Trialing = 2,

    /// <summary>Betalend abonnement.</summary>
    Active = 3,

    /// <summary>Opgezegd; loopt nog tot <c>Subscription.CancelAt</c> en verlengt niet meer.</summary>
    Cancelled = 4,

    /// <summary>
    /// Vergrendeld: proef stopgezet na opzegging voor de betaalde periode begon. Enkel inloggen,
    /// raadplegen en exporteren tot de club een plan kiest.
    /// </summary>
    Locked = 5
}
