namespace CoachOS.Application.Configuration;

/// <summary>
/// Configuratie voor de trial-/subscription-flow bij registratie.
/// </summary>
public class SubscriptionOptions
{
    public const string SectionName = "Subscription";

    /// <summary>Gratis proefperiode in dagen.</summary>
    public int TrialDays { get; set; } = 60;
}
