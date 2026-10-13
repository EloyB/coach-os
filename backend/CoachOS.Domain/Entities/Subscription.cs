using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Abonnement van een organisatie op CoachOS (Game/Set/Match, per actieve leerling).
/// 1-op-1 met <see cref="Organization"/>. Billing verloopt via ons platform-Mollie-account
/// (niet de Connect-koppeling die clubs met hun leerlingen gebruiken).
/// </summary>
public class Subscription : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public BillingInterval Interval { get; set; } = BillingInterval.Monthly;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
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
}
