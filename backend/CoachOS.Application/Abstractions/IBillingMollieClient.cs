using CoachOS.Domain.Models;

namespace CoachOS.Application.Abstractions;

/// <summary>
/// Mollie-client voor <b>platform-billing</b>: clubs betalen hún CoachOS-abonnement
/// aan ons, via ons eigen Mollie-account met een statische API-key (géén OAuth/Connect).
/// Volledig los van <see cref="IMollieClient"/> (dat is de Connect-koppeling waarmee
/// clubs van hún leerlingen betaald worden).
/// </summary>
public interface IBillingMollieClient
{
    /// <summary>
    /// <c>true</c> wanneer er een echte platform-API-key is geconfigureerd.
    /// <c>false</c> = lokale fake die geen echte Mollie-calls doet.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>Maakt een Mollie customer op ons platform-account (<c>POST /v2/customers</c>).</summary>
    Task<Result<string>> CreateCustomerAsync(string name, string email, CancellationToken ct = default);

    /// <summary>Eenmalige betaling zonder mandaat (<c>POST /v2/payments</c>), bv. na een mislukte verlenging.</summary>
    Task<Result<BillingPaymentResult>> CreatePaymentAsync(
        BillingPaymentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Eerste betaling (<c>sequenceType=first</c>) die bij succes een mandaat aanmaakt voor
    /// automatische verlenging. <see cref="BillingPaymentRequest.CustomerId"/> is verplicht.
    /// </summary>
    Task<Result<BillingPaymentResult>> CreateFirstPaymentAsync(
        BillingPaymentRequest request, CancellationToken ct = default);

    /// <summary>Volledige status van een betaling (<c>GET /v2/payments/{id}</c>).</summary>
    Task<Result<BillingPaymentDetails>> GetPaymentDetailsAsync(string paymentId, CancellationToken ct = default);

    /// <summary>Mollie Subscription voor automatische verlenging; geeft het subscription-id terug.</summary>
    Task<Result<string>> CreateSubscriptionAsync(BillingSubscriptionRequest request, CancellationToken ct = default);

    /// <summary>Annuleert een Mollie Subscription (<c>DELETE /v2/customers/{c}/subscriptions/{s}</c>).</summary>
    Task<Result> CancelSubscriptionAsync(string customerId, string subscriptionId, CancellationToken ct = default);

    /// <summary>Terugbetaling van (een deel van) een betaling (<c>POST /v2/payments/{id}/refunds</c>).</summary>
    Task<Result> RefundPaymentAsync(string paymentId, decimal amount, string description, CancellationToken ct = default);
}

/// <summary>Request voor een abonnements-betaling op ons platform-account. Bedrag incl. btw.</summary>
public sealed record BillingPaymentRequest(
    decimal Amount,
    string Currency,
    string Description,
    string RedirectUrl,
    string? WebhookUrl,
    string? CustomerId,
    IReadOnlyDictionary<string, string>? Metadata);

/// <summary>Resultaat van een succesvol aangemaakte billing-payment.</summary>
public sealed record BillingPaymentResult(string Id, string Status, string CheckoutUrl);

/// <summary>Volledige toestand van een billing-payment zoals Mollie die rapporteert.</summary>
public sealed record BillingPaymentDetails(
    string Id,
    string Status,
    decimal Amount,
    DateTime? PaidAt,
    string SequenceType,
    string? CustomerId,
    string? SubscriptionId,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>Request voor een Mollie Subscription. Bedrag incl. btw; interval in Mollie-syntax.</summary>
public sealed record BillingSubscriptionRequest(
    string CustomerId,
    decimal Amount,
    string Interval,
    DateOnly StartDate,
    string Description,
    string? WebhookUrl,
    IReadOnlyDictionary<string, string> Metadata);
