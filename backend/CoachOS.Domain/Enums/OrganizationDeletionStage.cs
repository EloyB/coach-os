namespace CoachOS.Domain.Enums;

/// <summary>
/// Waar de automatische verwijdering van een organisatie staat (90 dagen na het einde van de
/// proef of het abonnement). Hoort bij <c>Subscription.ScheduledDeletionOn</c>.
/// </summary>
public enum OrganizationDeletionStage
{
    /// <summary>Eerste mail verstuurd (30 dagen vooraf) met de verwijderdatum en de export.</summary>
    FirstNotice = 1,

    /// <summary>Laatste mail verstuurd (7 dagen vooraf); op de verwijderdatum wordt verwijderd.</summary>
    FinalNotice = 2,

    /// <summary>
    /// Club heeft betaald (facturen met wettelijke bewaarplicht): niet automatisch verwijderd,
    /// de superadmin werd één keer verwittigd om het manueel af te handelen.
    /// </summary>
    HandedToSuperAdmin = 3
}
