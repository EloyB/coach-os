using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Mollie;

/// <summary>
/// Platform-billing client met een statische Mollie API-key (Bearer). Test- vs live-mode
/// volgt uit het key-prefix (<c>test_</c>/<c>live_</c>); geen profileId of OAuth nodig.
/// Wordt enkel geregistreerd wanneer <see cref="MollieBillingOptions.ApiKey"/> is ingevuld.
/// </summary>
public class BillingMollieClient(
    HttpClient httpClient,
    IOptions<MollieBillingOptions> options,
    ILogger<BillingMollieClient> logger) : IBillingMollieClient
{
    private readonly MollieBillingOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public bool IsConfigured => true;

    public async Task<Result<string>> CreateCustomerAsync(string name, string email, CancellationToken ct = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"{_options.ApiBaseUrl}/v2/customers");
        request.Headers.Authorization = BearerHeader();
        request.Content = JsonContent.Create(new { name, email });

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Mollie billing POST /v2/customers gaf {Status}: {Body}", (int)response.StatusCode, body);
                return Result<string>.Fail(new Error(ErrorCodes.ExternalService, "Kon de Mollie customer niet aanmaken."));
            }

            MollieCustomerResponse? payload = await response.Content.ReadFromJsonAsync<MollieCustomerResponse>(JsonOptions, ct);
            if (payload is null || string.IsNullOrEmpty(payload.Id))
                return Result<string>.Fail(new Error(ErrorCodes.ExternalService, "Mollie gaf een leeg customer-antwoord."));

            return Result<string>.Ok(payload.Id);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Netwerkfout bij Mollie billing POST /v2/customers");
            return Result<string>.Fail(new Error(ErrorCodes.ExternalService, "Kon Mollie niet bereiken."));
        }
    }

    public async Task<Result<BillingPaymentResult>> CreatePaymentAsync(
        BillingPaymentRequest req, CancellationToken ct = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"{_options.ApiBaseUrl}/v2/payments");
        request.Headers.Authorization = BearerHeader();

        Dictionary<string, object?> payload = new()
        {
            ["amount"] = new { value = FormatAmount(req.Amount), currency = req.Currency },
            ["description"] = req.Description,
            ["redirectUrl"] = req.RedirectUrl,
        };
        if (!string.IsNullOrEmpty(req.WebhookUrl)) payload["webhookUrl"] = req.WebhookUrl;
        if (!string.IsNullOrEmpty(req.CustomerId)) payload["customerId"] = req.CustomerId;
        if (req.Metadata is { Count: > 0 }) payload["metadata"] = req.Metadata;
        request.Content = JsonContent.Create(payload);

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Mollie billing POST /v2/payments gaf {Status}: {Body}", (int)response.StatusCode, body);
                return Result<BillingPaymentResult>.Fail(new Error(ErrorCodes.ExternalService, "Kon de Mollie betaling niet aanmaken."));
            }

            MolliePaymentResponse? payload2 = await response.Content.ReadFromJsonAsync<MolliePaymentResponse>(JsonOptions, ct);
            string? checkoutUrl = payload2?.Links?.Checkout?.Href;
            if (payload2 is null || string.IsNullOrEmpty(payload2.Id) || string.IsNullOrEmpty(checkoutUrl))
                return Result<BillingPaymentResult>.Fail(new Error(ErrorCodes.ExternalService, "Mollie payment-antwoord mist id of checkout URL."));

            return Result<BillingPaymentResult>.Ok(new BillingPaymentResult(payload2.Id, payload2.Status ?? "open", checkoutUrl));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Netwerkfout bij Mollie billing POST /v2/payments");
            return Result<BillingPaymentResult>.Fail(new Error(ErrorCodes.ExternalService, "Kon Mollie niet bereiken."));
        }
    }

    public async Task<Result<BillingPaymentSnapshot>> GetPaymentAsync(string paymentId, CancellationToken ct = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"{_options.ApiBaseUrl}/v2/payments/{paymentId}");
        request.Headers.Authorization = BearerHeader();

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Mollie billing GET /v2/payments/{Id} gaf {Status}: {Body}", paymentId, (int)response.StatusCode, body);
                return Result<BillingPaymentSnapshot>.Fail(new Error(ErrorCodes.ExternalService, "Kon Mollie payment status niet ophalen."));
            }

            MolliePaymentResponse? payload = await response.Content.ReadFromJsonAsync<MolliePaymentResponse>(JsonOptions, ct);
            if (payload is null || string.IsNullOrEmpty(payload.Id))
                return Result<BillingPaymentSnapshot>.Fail(new Error(ErrorCodes.ExternalService, "Mollie gaf een leeg payment-antwoord."));

            DateTime? paidAtUtc = payload.PaidAt.HasValue
                ? DateTime.SpecifyKind(payload.PaidAt.Value.ToUniversalTime(), DateTimeKind.Utc)
                : null;

            return Result<BillingPaymentSnapshot>.Ok(new BillingPaymentSnapshot(payload.Id, payload.Status ?? "open", paidAtUtc));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Netwerkfout bij Mollie billing GET /v2/payments/{Id}", paymentId);
            return Result<BillingPaymentSnapshot>.Fail(new Error(ErrorCodes.ExternalService, "Kon Mollie niet bereiken."));
        }
    }

    private AuthenticationHeaderValue BearerHeader() => new("Bearer", _options.ApiKey);

    private static string FormatAmount(decimal amount)
        => amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    private sealed record MollieCustomerResponse(
        [property: JsonPropertyName("id")] string Id);

    private sealed record MolliePaymentResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("paidAt")] DateTime? PaidAt,
        [property: JsonPropertyName("_links")] MolliePaymentLinks? Links);

    private sealed record MolliePaymentLinks(
        [property: JsonPropertyName("checkout")] MollieLinkHref? Checkout);

    private sealed record MollieLinkHref(
        [property: JsonPropertyName("href")] string? Href);
}
