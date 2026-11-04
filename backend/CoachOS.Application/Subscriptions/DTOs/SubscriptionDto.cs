using CoachOS.Application.Billing.DTOs;

namespace CoachOS.Application.Subscriptions.DTOs;

/// <summary>Abonnement van een organisatie, zoals getoond in het superadmin-paneel.</summary>
public class SubscriptionDto
{
    public Guid OrganizationId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Plan { get; init; }
    public string? Interval { get; init; }
    public string? PaymentMethod { get; init; }
    public int? StudentLimit { get; init; }
    public decimal Price { get; init; }
    public decimal PriceInclVat { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? TrialEndsAt { get; init; }
    public DateOnly? CurrentPeriodStart { get; init; }
    public DateOnly? CurrentPeriodEnd { get; init; }
    public DateOnly? CancelAt { get; init; }
    public bool IsDeal { get; init; }
    public string? MollieCustomerId { get; init; }
    public string? MollieSubscriptionId { get; init; }

    /// <summary><c>false</c> zolang er geen echte platform-Mollie-key is (fake-betalingen).</summary>
    public bool BillingConfigured { get; init; }

    /// <summary><c>false</c> zolang Billit niet gekoppeld is (fake-facturen).</summary>
    public bool InvoicingConfigured { get; init; }

    public BillingProfileDto? Profile { get; init; }
    public IReadOnlyList<SubscriptionPaymentDto> Payments { get; init; } = [];
}
