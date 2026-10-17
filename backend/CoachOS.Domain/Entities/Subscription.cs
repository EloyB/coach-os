using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Abonnement van een organisatie op CoachOS. Elke organisatie heeft er precies één:
/// bestaande organisaties staan op <see cref="SubscriptionStatus.Legacy"/>, nieuwe starten
/// met een gratis proef (<see cref="StartTrial"/>).
/// </summary>
public class Subscription : BaseEntity
{
    public const int TrialDays = 30;

    public Guid OrganizationId { get; set; }

    /// <summary>Gekozen plan; null zolang er nog niet betaald wordt (proef of legacy).</summary>
    public SubscriptionPlan? Plan { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Legacy;

    /// <summary>Facturatieperiode; null zolang er geen plan is.</summary>
    public BillingInterval? Interval { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>Laatste dag van de proef (inclusief). Enkel gezet bij <see cref="SubscriptionStatus.Trialing"/>.</summary>
    public DateOnly? TrialEndsAt { get; set; }

    /// <summary>
    /// Laatst verstuurde proef-herinnering (20, 27 of 30 = dag in de proef). Voorkomt dat
    /// dezelfde herinnering twee keer vertrekt.
    /// </summary>
    public int? LastTrialReminderDay { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Snapshot van het aangerekende bedrag (EUR, excl. btw) voor het gekozen interval.</summary>
    public decimal Price { get; set; }

    // --- Platform-billing (ons Mollie-account) ---

    /// <summary>Mollie customer op ons platform-account (bv. <c>cst_xxx</c>).</summary>
    public string? MollieCustomerId { get; set; }

    /// <summary>Mollie recurring subscription-id. Ongebruikt in de MVP (handmatige betaallinks).</summary>
    public string? MollieSubscriptionId { get; set; }

    /// <summary>Laatste aangemaakte billing-payment (bv. <c>tr_xxx</c>).</summary>
    public string? LastPaymentId { get; set; }

    /// <summary>Checkout-URL van de laatste betaallink; stuur je door naar de club.</summary>
    public string? LastCheckoutUrl { get; set; }

    /// <summary>Laatst gekende Mollie-status van de billing-payment (open, paid, failed, ...).</summary>
    public string? LastPaymentStatus { get; set; }

    /// <summary>UTC moment waarop de laatste betaling als betaald is bevestigd.</summary>
    public DateTime? LastPaidAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;

    /// <summary>Nieuwe proef van <see cref="TrialDays"/> dagen, zonder plan of betaalgegevens.</summary>
    public static Subscription StartTrial(Guid organizationId, DateOnly today) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        Status = SubscriptionStatus.Trialing,
        StartDate = today,
        TrialEndsAt = today.AddDays(TrialDays),
        IsActive = true
    };
}
