using CoachOS.Domain.Models;

namespace CoachOS.Domain.Interfaces;

/// <summary>
/// Mollie REST API wrapper. Bevat de OAuth onboarding calls (PR #2) en de
/// payment lifecycle calls (PR #4).
/// </summary>
public interface IMollieClient
{
    /// <summary>Wisselt een authorization <paramref name="code"/> in voor access + refresh tokens.</summary>
    Task<Result<MollieTokenResponse>> ExchangeCodeForTokenAsync(
        string code,
        string redirectUri,
        CancellationToken ct = default);

    /// <summary>Vernieuwt een access token met een refresh token.</summary>
    Task<Result<MollieTokenResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default);

    /// <summary>Haalt de Mollie-organisatie op voor het gegeven access token (<c>GET /v2/organizations/me</c>).</summary>
    Task<Result<MollieOrganizationInfo>> GetOrganizationAsync(
        string accessToken,
        CancellationToken ct = default);

    /// <summary>Trekt een refresh token (en de bijbehorende access tokens) in.</summary>
    Task<Result> RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default);

    /// <summary>
    /// Creëert een Mollie payment op het account van de met OAuth gekoppelde organisatie.
    /// <paramref name="accessToken"/> is de Bearer token van die organisatie.
    /// </summary>
    Task<Result<MolliePaymentCreatedResponse>> CreatePaymentAsync(
        string accessToken,
        MolliePaymentRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Haalt de authoritative status van een payment op (<c>GET /v2/payments/{id}</c>).
    /// Webhook-handler én polling-endpoints gebruiken deze om de Payment-rij bij te werken.
    /// </summary>
    Task<Result<MolliePaymentSnapshot>> GetPaymentAsync(
        string accessToken,
        string paymentId,
        bool testmode,
        CancellationToken ct = default);

    /// <summary>
    /// Haalt het eerste actieve website-profile op voor de gekoppelde organisatie
    /// (<c>GET /v2/profiles?limit=1</c>). Mollie eist een <c>profileId</c> bij
    /// elke payment in Connect-context; deze call laat ons de eerste vinden zonder
    /// dat de club iets manueel in het Mollie dashboard hoeft te doen.
    /// </summary>
    Task<Result<string>> GetFirstProfileIdAsync(
        string accessToken,
        CancellationToken ct = default);
}

/// <summary>Antwoord van Mollie's <c>POST /oauth2/tokens</c>.</summary>
public sealed record MollieTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    string TokenType,
    string Scope);

/// <summary>Subset van <c>GET /v2/organizations/me</c> die we cachen.</summary>
public sealed record MollieOrganizationInfo(
    string Id,
    string Name);

/// <summary>
/// Request voor <c>POST /v2/payments</c>. Bedragen worden naar strings in het door
/// Mollie verwachte formaat (<c>"50.00"</c>) geserialiseerd door <see cref="IMollieClient"/>.
/// </summary>
public sealed record MolliePaymentRequest(
    decimal Amount,
    string Currency,
    string Description,
    string RedirectUrl,
    string? WebhookUrl,
    decimal? ApplicationFee,
    string? ApplicationFeeDescription,
    IReadOnlyDictionary<string, string>? Metadata,
    /// <summary>Mollie website-profile-id; verplicht in Connect-context.</summary>
    string? ProfileId,
    /// <summary>Forceer test mode (lokaal dev); <c>null</c> = volg account default.</summary>
    bool? Testmode);

/// <summary>Resultaat van een succesvolle payment creation.</summary>
public sealed record MolliePaymentCreatedResponse(
    string Id,
    string Status,
    string CheckoutUrl);

/// <summary>
/// Snapshot van <c>GET /v2/payments/{id}</c>. <see cref="Status"/> matcht Mollie's
/// statussen (<c>open</c>, <c>pending</c>, <c>authorized</c>, <c>paid</c>,
/// <c>canceled</c>, <c>expired</c>, <c>failed</c>).
/// </summary>
public sealed record MolliePaymentSnapshot(
    string Id,
    string Status,
    decimal Amount,
    string Currency,
    DateTime? PaidAt,
    string? Method,
    string? FailureReason);
