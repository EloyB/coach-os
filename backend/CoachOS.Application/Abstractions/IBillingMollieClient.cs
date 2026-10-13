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

    /// <summary>Maakt een eenmalige betaling en geeft de checkout-URL terug (<c>POST /v2/payments</c>).</summary>
    Task<Result<BillingPaymentResult>> CreatePaymentAsync(
        BillingPaymentRequest request, CancellationToken ct = default);

    /// <summary>Haalt de huidige status van een billing-payment op (<c>GET /v2/payments/{id}</c>).</summary>
    Task<Result<BillingPaymentSnapshot>> GetPaymentAsync(string paymentId, CancellationToken ct = default);
}

/// <summary>Request voor een eenmalige abonnements-betaling op ons platform-account.</summary>
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

/// <summary>Snapshot van <c>GET /v2/payments/{id}</c> voor billing (open/paid/failed/...).</summary>
public sealed record BillingPaymentSnapshot(string Id, string Status, DateTime? PaidAt);
