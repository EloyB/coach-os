using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Abonnement van een organisatie op CoachOS. Fase 1: enkel trial + status.
/// Plan/prijs/Mollie worden pas bindend bij upgrade (fase 2).
/// </summary>
public class Subscription : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Statusmachine — bron van waarheid voor toegang.</summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

    /// <summary>Einde van de gratis proefperiode (UTC). Null zodra betaald.</summary>
    public DateTime? TrialEndsAt { get; set; }

    /// <summary>Tot wanneer betaalde toegang loopt (UTC). Null tijdens trial.</summary>
    public DateTime? CurrentPeriodEnd { get; set; }

    /// <summary>Plan gekozen op de website bij aanmelden — niet-bindend tot upgrade.</summary>
    public SubscriptionPlan? IntendedPlan { get; set; }

    /// <summary>Bindend plan zodra betaald (fase 2).</summary>
    public SubscriptionPlan? Plan { get; set; }

    /// <summary>Netto maandbedrag in EUR zodra betaald (fase 2).</summary>
    public decimal? MonthlyPrice { get; set; }

    public string? MollieSubscriptionId { get; set; }
    public string? MollieCustomerId { get; set; }

    public Organization Organization { get; set; } = null!;
}
