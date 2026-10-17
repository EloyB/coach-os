using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Een betaalde (of te betalen) periode van een CoachOS-abonnement. Dient tegelijk als
/// betaalhistoriek en als factuur-outbox (<see cref="InvoiceStatus"/>). Los van
/// <see cref="Payment"/>, dat over betalingen van leerlingen aan de club gaat.
/// </summary>
public class SubscriptionPayment : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid SubscriptionId { get; set; }
    public SubscriptionPaymentKind Kind { get; set; }
    public BillingPaymentMethod Method { get; set; }
    public SubscriptionPaymentStatus Status { get; set; } = SubscriptionPaymentStatus.Open;
    public SubscriptionPlan Plan { get; set; }
    public BillingInterval Interval { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal AmountExclVat { get; set; }
    public decimal VatRate { get; set; }
    public decimal AmountInclVat { get; set; }

    /// <summary>Mollie payment-id (<c>tr_xxx</c>); null bij overschrijving of deal.</summary>
    public string? MolliePaymentId { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }

    /// <summary>Vervaldag van de factuur bij overschrijving.</summary>
    public DateOnly? DueDate { get; set; }

    public InvoiceStatus InvoiceStatus { get; set; } = InvoiceStatus.NotNeeded;
    public int InvoiceAttempts { get; set; }
    public DateTime? InvoiceNextAttemptAt { get; set; }
    public string? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? InvoicePdfUrl { get; set; }
    public string? InvoiceLastError { get; set; }

    public Subscription Subscription { get; set; } = null!;
}
