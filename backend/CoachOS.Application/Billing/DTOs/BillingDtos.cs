namespace CoachOS.Application.Billing.DTOs;

public sealed record SaveBillingProfileRequest(
    string Name,
    string Street,
    string PostalCode,
    string City,
    string Country,
    string InvoiceEmail,
    string? VatNumber);

/// <summary>Plan, interval en betaalwijze als enumwaarden (SubscriptionPlan, BillingInterval, BillingPaymentMethod).</summary>
public sealed record CheckoutRequest(int Plan, int Interval, int Method);

/// <summary>Checkout-URL van Mollie, of null bij overschrijving (abonnement is dan meteen actief).</summary>
public sealed record CheckoutResultDto(string? CheckoutUrl);

public sealed record PlanOptionDto(int Plan, string Name, decimal MonthlyPrice, decimal YearlyPrice, int StudentLimit);

public sealed record BillingProfileDto(
    string Name,
    string Street,
    string PostalCode,
    string City,
    string Country,
    string InvoiceEmail,
    string? VatNumber,
    bool? VatVerified);

public sealed record SubscriptionPaymentDto(
    Guid Id,
    string Kind,
    string Method,
    string Status,
    string Plan,
    string Interval,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal AmountExclVat,
    decimal AmountInclVat,
    DateTime? PaidAt,
    DateOnly? DueDate,
    string? CheckoutUrl,
    string InvoiceStatus,
    string? InvoiceNumber,
    string? InvoicePdfUrl,
    string? InvoiceLastError);

public sealed class BillingOverviewDto
{
    public string Status { get; init; } = string.Empty;
    public string? Plan { get; init; }
    public string? Interval { get; init; }
    public string? PaymentMethod { get; init; }
    public decimal PriceExclVat { get; init; }
    public decimal PriceInclVat { get; init; }
    public int? StudentLimit { get; init; }
    public DateOnly? CurrentPeriodStart { get; init; }
    public DateOnly? CurrentPeriodEnd { get; init; }
    public DateOnly? CancelAt { get; init; }
    public DateOnly? TrialEndsAt { get; init; }
    public bool IsDeal { get; init; }
    public int ActiveStudentCount { get; init; }
    public string SuggestedPlan { get; init; } = string.Empty;
    public bool CanCheckout { get; init; }
    public bool BillingConfigured { get; init; }
    public decimal OverageMarginPercent { get; init; }
    public decimal OveragePricePerStudent { get; init; }
    public IReadOnlyList<PlanOptionDto> Plans { get; init; } = [];
    public BillingProfileDto? Profile { get; init; }
    public IReadOnlyList<SubscriptionPaymentDto> Payments { get; init; } = [];
}
