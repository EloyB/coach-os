namespace CoachOS.Domain.Enums;

/// <summary>Wat een organisatie op basis van haar abonnement mag.</summary>
public enum SubscriptionAccessLevel
{
    Full = 1,

    /// <summary>Inloggen, bekijken en exporteren — geen wijzigingen.</summary>
    ReadOnly = 2,

    /// <summary>Normal club data access closed after the export window; reactivation remains separate.</summary>
    Closed = 3
}
