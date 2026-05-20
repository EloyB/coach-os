using CoachOS.Domain.Models;

namespace CoachOS.Domain.Interfaces;

/// <summary>
/// Mollie REST API wrapper. Hier zijn enkel de calls nodig voor OAuth onboarding
/// (PR #2). Payment-gerelateerde calls (<c>CreatePaymentAsync</c>,
/// <c>GetPaymentAsync</c>) komen in PR #4.
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
