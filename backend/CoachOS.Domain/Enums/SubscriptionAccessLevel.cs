namespace CoachOS.Domain.Enums;

/// <summary>Wat een organisatie op basis van haar abonnement mag.</summary>
public enum SubscriptionAccessLevel
{
    Full = 1,

    /// <summary>Inloggen, bekijken en exporteren — geen wijzigingen.</summary>
    ReadOnly = 2
}
