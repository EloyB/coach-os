using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Mollie;

/// <summary>
/// Mollie REST API client. Endpoint paths volgen Mollie's officiële documentatie:
/// <c>POST /oauth2/tokens</c>, <c>DELETE /oauth2/tokens</c>, <c>GET /v2/organizations/me</c>.
/// Auth voor de OAuth token endpoint is HTTP Basic (client_id:client_secret); voor
/// resource endpoints is het een Bearer access token.
/// </summary>
public class MollieClient(
    HttpClient httpClient,
    IOptions<MollieOptions> options,
    ILogger<MollieClient> logger) : IMollieClient
{
    private readonly MollieOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<Result<MollieTokenResponse>> ExchangeCodeForTokenAsync(
        string code,
        string redirectUri,
        CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, string> form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
        };
        return await PostTokenAsync(form, ct);
    }

    public async Task<Result<MollieTokenResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, string> form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        };
        return await PostTokenAsync(form, ct);
    }

    public async Task<Result<MollieOrganizationInfo>> GetOrganizationAsync(
        string accessToken,
        CancellationToken ct = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"{_options.ApiBaseUrl}/v2/organizations/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Mollie /organizations/me gaf {Status}: {Body}", (int)response.StatusCode, body);
                return Result<MollieOrganizationInfo>.Fail(new Error(
                    ErrorCodes.ExternalService,
                    "Kon Mollie organisatie-info niet ophalen."));
            }

            MollieOrganizationPayload? payload = await response.Content.ReadFromJsonAsync<MollieOrganizationPayload>(JsonOptions, ct);
            if (payload is null || string.IsNullOrEmpty(payload.Id))
            {
                return Result<MollieOrganizationInfo>.Fail(new Error(
                    ErrorCodes.ExternalService,
                    "Mollie gaf een leeg organisatie-antwoord."));
            }

            return Result<MollieOrganizationInfo>.Ok(new MollieOrganizationInfo(payload.Id, payload.Name ?? string.Empty));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Netwerkfout bij Mollie /organizations/me");
            return Result<MollieOrganizationInfo>.Fail(new Error(
                ErrorCodes.ExternalService,
                "Kon Mollie niet bereiken."));
        }
    }

    public async Task<Result> RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default)
    {
        // Mollie revoke: DELETE /oauth2/tokens met body { "token": "...", "token_type_hint": "refresh_token" }
        // Basic auth met client credentials (zelfde als token exchange).
        using HttpRequestMessage request = new(HttpMethod.Delete, $"{_options.ApiBaseUrl}/oauth2/tokens");
        request.Headers.Authorization = BuildBasicAuthHeader();
        request.Content = JsonContent.Create(new
        {
            token = refreshToken,
            token_type_hint = "refresh_token",
        });

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request, ct);
            // Mollie returns 204 No Content op succes; 4xx als token al ingetrokken is.
            // 4xx als al ingetrokken is acceptabel — disconnect blijft idempotent.
            if (!response.IsSuccessStatusCode && (int)response.StatusCode < 400)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Mollie token revoke gaf {Status}: {Body}", (int)response.StatusCode, body);
                return Result.Fail(new Error(ErrorCodes.ExternalService, "Kon Mollie token niet intrekken."));
            }
            return Result.Ok();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Netwerkfout bij Mollie token revoke");
            return Result.Fail(new Error(ErrorCodes.ExternalService, "Kon Mollie niet bereiken."));
        }
    }

    private async Task<Result<MollieTokenResponse>> PostTokenAsync(
        IReadOnlyDictionary<string, string> form,
        CancellationToken ct)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"{_options.ApiBaseUrl}/oauth2/tokens");
        request.Headers.Authorization = BuildBasicAuthHeader();
        request.Content = new FormUrlEncodedContent(form);

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request, ct);
            string body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Mollie /oauth2/tokens gaf {Status}: {Body}", (int)response.StatusCode, body);
                return Result<MollieTokenResponse>.Fail(new Error(
                    ErrorCodes.ExternalService,
                    "Mollie OAuth-token uitwisseling is mislukt."));
            }

            MollieTokenPayload? payload = JsonSerializer.Deserialize<MollieTokenPayload>(body, JsonOptions);
            if (payload is null || string.IsNullOrEmpty(payload.AccessToken) || string.IsNullOrEmpty(payload.RefreshToken))
            {
                return Result<MollieTokenResponse>.Fail(new Error(
                    ErrorCodes.ExternalService,
                    "Mollie gaf een onvolledig OAuth-token-antwoord."));
            }

            return Result<MollieTokenResponse>.Ok(new MollieTokenResponse(
                payload.AccessToken,
                payload.RefreshToken,
                payload.ExpiresIn,
                payload.TokenType ?? "bearer",
                payload.Scope ?? string.Empty));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Netwerkfout bij Mollie /oauth2/tokens");
            return Result<MollieTokenResponse>.Fail(new Error(
                ErrorCodes.ExternalService,
                "Kon Mollie niet bereiken."));
        }
    }

    private AuthenticationHeaderValue BuildBasicAuthHeader()
    {
        string raw = $"{_options.ClientId}:{_options.ClientSecret}";
        string encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
        return new AuthenticationHeaderValue("Basic", encoded);
    }

    private sealed record MollieTokenPayload(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string? TokenType,
        [property: JsonPropertyName("scope")] string? Scope);

    private sealed record MollieOrganizationPayload(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? Name);
}
