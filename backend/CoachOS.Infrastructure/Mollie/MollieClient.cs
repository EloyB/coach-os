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

    public async Task<Result<MolliePaymentCreatedResponse>> CreatePaymentAsync(
        string accessToken,
        MolliePaymentRequest paymentRequest,
        CancellationToken ct = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"{_options.ApiBaseUrl}/v2/payments");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        object payload = BuildPaymentPayload(paymentRequest);
        request.Content = JsonContent.Create(payload);

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Mollie POST /v2/payments gaf {Status}: {Body}", (int)response.StatusCode, body);
                return Result<MolliePaymentCreatedResponse>.Fail(new Error(
                    ErrorCodes.ExternalService,
                    "Kon de Mollie betaling niet aanmaken."));
            }

            MolliePaymentApiResponse? payload2 = await response.Content.ReadFromJsonAsync<MolliePaymentApiResponse>(JsonOptions, ct);
            if (payload2 is null || string.IsNullOrEmpty(payload2.Id))
            {
                return Result<MolliePaymentCreatedResponse>.Fail(new Error(
                    ErrorCodes.ExternalService,
                    "Mollie gaf een leeg payment-antwoord."));
            }

            string? checkoutUrl = payload2.Links?.Checkout?.Href;
            if (string.IsNullOrEmpty(checkoutUrl))
            {
                return Result<MolliePaymentCreatedResponse>.Fail(new Error(
                    ErrorCodes.ExternalService,
                    "Mollie payment-antwoord mist checkout URL."));
            }

            return Result<MolliePaymentCreatedResponse>.Ok(new MolliePaymentCreatedResponse(
                payload2.Id,
                payload2.Status ?? "open",
                checkoutUrl));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Netwerkfout bij Mollie POST /v2/payments");
            return Result<MolliePaymentCreatedResponse>.Fail(new Error(
                ErrorCodes.ExternalService,
                "Kon Mollie niet bereiken."));
        }
    }

    public async Task<Result<MolliePaymentSnapshot>> GetPaymentAsync(
        string accessToken,
        string paymentId,
        CancellationToken ct = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"{_options.ApiBaseUrl}/v2/payments/{paymentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Mollie GET /v2/payments/{Id} gaf {Status}: {Body}", paymentId, (int)response.StatusCode, body);
                return Result<MolliePaymentSnapshot>.Fail(new Error(
                    ErrorCodes.ExternalService,
                    "Kon Mollie payment status niet ophalen."));
            }

            MolliePaymentApiResponse? payload = await response.Content.ReadFromJsonAsync<MolliePaymentApiResponse>(JsonOptions, ct);
            if (payload is null || string.IsNullOrEmpty(payload.Id))
            {
                return Result<MolliePaymentSnapshot>.Fail(new Error(
                    ErrorCodes.ExternalService,
                    "Mollie gaf een leeg payment-antwoord."));
            }

            decimal amount = decimal.TryParse(payload.Amount?.Value, System.Globalization.CultureInfo.InvariantCulture, out decimal a) ? a : 0m;
            string currency = payload.Amount?.Currency ?? "EUR";

            return Result<MolliePaymentSnapshot>.Ok(new MolliePaymentSnapshot(
                payload.Id,
                payload.Status ?? "open",
                amount,
                currency,
                payload.PaidAt,
                payload.Method,
                payload.Details?.FailureReason));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Netwerkfout bij Mollie GET /v2/payments/{Id}", paymentId);
            return Result<MolliePaymentSnapshot>.Fail(new Error(
                ErrorCodes.ExternalService,
                "Kon Mollie niet bereiken."));
        }
    }

    private static object BuildPaymentPayload(MolliePaymentRequest req)
    {
        Dictionary<string, object?> payload = new()
        {
            ["amount"] = new { value = FormatAmount(req.Amount), currency = req.Currency },
            ["description"] = req.Description,
            ["redirectUrl"] = req.RedirectUrl,
        };
        if (!string.IsNullOrEmpty(req.WebhookUrl))
        {
            payload["webhookUrl"] = req.WebhookUrl;
        }
        if (req.ApplicationFee is { } fee && fee > 0m)
        {
            payload["applicationFee"] = new
            {
                amount = new { value = FormatAmount(fee), currency = req.Currency },
                description = req.ApplicationFeeDescription ?? "CoachOS platform fee",
            };
        }
        if (req.Metadata is { Count: > 0 })
        {
            payload["metadata"] = req.Metadata;
        }
        return payload;
    }

    private static string FormatAmount(decimal amount)
    {
        // Mollie vereist string-formaat met 2 decimalen en . als separator, ongeacht culture.
        return amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
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

    private sealed record MolliePaymentApiResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("amount")] MollieAmount? Amount,
        [property: JsonPropertyName("method")] string? Method,
        [property: JsonPropertyName("paidAt")] DateTime? PaidAt,
        [property: JsonPropertyName("details")] MolliePaymentDetails? Details,
        [property: JsonPropertyName("_links")] MolliePaymentLinks? Links);

    private sealed record MollieAmount(
        [property: JsonPropertyName("value")] string? Value,
        [property: JsonPropertyName("currency")] string? Currency);

    private sealed record MolliePaymentDetails(
        [property: JsonPropertyName("failureReason")] string? FailureReason);

    private sealed record MolliePaymentLinks(
        [property: JsonPropertyName("checkout")] MollieLinkHref? Checkout);

    private sealed record MollieLinkHref(
        [property: JsonPropertyName("href")] string? Href);
}
