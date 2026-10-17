using System.Globalization;
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
        Result<MollieIdResponse> response = await SendAsync<MollieIdResponse>(
            HttpMethod.Post, "/v2/customers", new { name, email }, "Kon de Mollie customer niet aanmaken.", ct);
        return response.IsSuccess
            ? Result<string>.Ok(response.Value!.Id)
            : Result<string>.Fail(response.Errors);
    }

    public Task<Result<BillingPaymentResult>> CreatePaymentAsync(
        BillingPaymentRequest request, CancellationToken ct = default)
        => CreatePaymentCoreAsync(request, sequenceType: null, ct);

    public Task<Result<BillingPaymentResult>> CreateFirstPaymentAsync(
        BillingPaymentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.CustomerId))
            return Task.FromResult(Result<BillingPaymentResult>.Fail(
                new Error(ErrorCodes.Validation, "Een eerste betaling vereist een Mollie customer.")));

        return CreatePaymentCoreAsync(request, sequenceType: "first", ct);
    }

    public async Task<Result<BillingPaymentDetails>> GetPaymentDetailsAsync(string paymentId, CancellationToken ct = default)
    {
        Result<MolliePaymentResponse> response = await SendAsync<MolliePaymentResponse>(
            HttpMethod.Get, $"/v2/payments/{Uri.EscapeDataString(paymentId)}", null,
            "Kon de Mollie-betaling niet ophalen.", ct);
        if (!response.IsSuccess)
            return Result<BillingPaymentDetails>.Fail(response.Errors);

        MolliePaymentResponse p = response.Value!;
        DateTime? paidAtUtc = p.PaidAt?.UtcDateTime;
        return Result<BillingPaymentDetails>.Ok(new BillingPaymentDetails(
            p.Id,
            p.Status ?? "open",
            ParseAmount(p.Amount?.Value),
            paidAtUtc,
            p.SequenceType ?? "oneoff",
            p.CustomerId,
            p.SubscriptionId,
            p.Metadata ?? new Dictionary<string, string>()));
    }

    public async Task<Result<string>> CreateSubscriptionAsync(
        BillingSubscriptionRequest request, CancellationToken ct = default)
    {
        Dictionary<string, object?> payload = new()
        {
            ["amount"] = new { value = FormatAmount(request.Amount), currency = "EUR" },
            ["interval"] = request.Interval,
            ["startDate"] = request.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["description"] = request.Description,
            ["metadata"] = request.Metadata,
        };
        if (!string.IsNullOrEmpty(request.WebhookUrl)) payload["webhookUrl"] = request.WebhookUrl;

        Result<MollieIdResponse> response = await SendAsync<MollieIdResponse>(
            HttpMethod.Post, $"/v2/customers/{Uri.EscapeDataString(request.CustomerId)}/subscriptions", payload,
            "Kon de automatische verlenging bij Mollie niet aanmaken.", ct, request.IdempotencyKey);
        return response.IsSuccess
            ? Result<string>.Ok(response.Value!.Id)
            : Result<string>.Fail(response.Errors);
    }

    /// <summary>
    /// Elk 2xx-antwoord is succes, ook zonder body (204 No Content): een DELETE moet geen object
    /// teruggeven om geslaagd te zijn.
    /// </summary>
    public async Task<Result> CancelSubscriptionAsync(string customerId, string subscriptionId, CancellationToken ct = default)
    {
        string path = $"/v2/customers/{Uri.EscapeDataString(customerId)}/subscriptions/{Uri.EscapeDataString(subscriptionId)}";
        using HttpRequestMessage request = new(HttpMethod.Delete, $"{_options.ApiBaseUrl.TrimEnd('/')}{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return Result.Ok();

            string error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Mollie billing DELETE {Path} gaf {Status}: {Body}", path, (int)response.StatusCode, error);
            return Result.Fail(new Error(ErrorCodes.ExternalService, "Kon de automatische verlenging bij Mollie niet stopzetten."));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Fout bij Mollie billing DELETE {Path}", path);
            return Result.Fail(new Error(ErrorCodes.ExternalService, "Kon Mollie niet bereiken."));
        }
    }

    public async Task<Result> RefundPaymentAsync(string paymentId, decimal amount, string description, CancellationToken ct = default)
    {
        object payload = new { amount = new { value = FormatAmount(amount), currency = "EUR" }, description };
        Result<MollieIdResponse> response = await SendAsync<MollieIdResponse>(
            HttpMethod.Post, $"/v2/payments/{Uri.EscapeDataString(paymentId)}/refunds", payload,
            "Kon de terugbetaling bij Mollie niet aanmaken.", ct);
        return response.IsSuccess ? Result.Ok() : Result.Fail(response.Errors);
    }

    private async Task<Result<BillingPaymentResult>> CreatePaymentCoreAsync(
        BillingPaymentRequest req, string? sequenceType, CancellationToken ct)
    {
        Dictionary<string, object?> payload = new()
        {
            ["amount"] = new { value = FormatAmount(req.Amount), currency = req.Currency },
            ["description"] = req.Description,
            ["redirectUrl"] = req.RedirectUrl,
        };
        if (sequenceType is not null) payload["sequenceType"] = sequenceType;
        if (!string.IsNullOrEmpty(req.WebhookUrl)) payload["webhookUrl"] = req.WebhookUrl;
        if (!string.IsNullOrEmpty(req.CustomerId)) payload["customerId"] = req.CustomerId;
        if (req.Metadata is { Count: > 0 }) payload["metadata"] = req.Metadata;

        Result<MolliePaymentResponse> response = await SendAsync<MolliePaymentResponse>(
            HttpMethod.Post, "/v2/payments", payload, "Kon de Mollie-betaling niet aanmaken.", ct);
        if (!response.IsSuccess)
            return Result<BillingPaymentResult>.Fail(response.Errors);

        string? checkoutUrl = response.Value!.Links?.Checkout?.Href;
        if (string.IsNullOrEmpty(checkoutUrl))
            return Result<BillingPaymentResult>.Fail(new Error(ErrorCodes.ExternalService, "Mollie gaf geen checkout-URL terug."));

        return Result<BillingPaymentResult>.Ok(
            new BillingPaymentResult(response.Value.Id, response.Value.Status ?? "open", checkoutUrl));
    }

    private async Task<Result<T>> SendAsync<T>(
        HttpMethod method, string path, object? body, string userMessage, CancellationToken ct,
        string? idempotencyKey = null)
        where T : MollieIdResponse
    {
        using HttpRequestMessage request = new(method, $"{_options.ApiBaseUrl.TrimEnd('/')}{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        if (!string.IsNullOrEmpty(idempotencyKey))
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Mollie billing {Method} {Path} gaf {Status}: {Body}",
                    method, path, (int)response.StatusCode, error);
                // 404 = bestaat niet bij Mollie (geen storing): niet opnieuw proberen.
                string code = response.StatusCode == System.Net.HttpStatusCode.NotFound
                    ? ErrorCodes.NotFound
                    : ErrorCodes.ExternalService;
                return Result<T>.Fail(new Error(code, userMessage));
            }

            T? payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            if (payload is null || string.IsNullOrEmpty(payload.Id))
                return Result<T>.Fail(new Error(ErrorCodes.ExternalService, userMessage));

            return Result<T>.Ok(payload);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Fout bij Mollie billing {Method} {Path}", method, path);
            return Result<T>.Fail(new Error(ErrorCodes.ExternalService, "Kon Mollie niet bereiken."));
        }
    }

    private static string FormatAmount(decimal amount)
        => amount.ToString("F2", CultureInfo.InvariantCulture);

    private static decimal ParseAmount(string? value)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) ? amount : 0m;

    private record MollieIdResponse([property: JsonPropertyName("id")] string Id);

    private sealed record MolliePaymentResponse(
        string Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("amount")] MollieAmount? Amount,
        [property: JsonPropertyName("paidAt")] DateTimeOffset? PaidAt,
        [property: JsonPropertyName("sequenceType")] string? SequenceType,
        [property: JsonPropertyName("customerId")] string? CustomerId,
        [property: JsonPropertyName("subscriptionId")] string? SubscriptionId,
        [property: JsonPropertyName("metadata")] Dictionary<string, string>? Metadata,
        [property: JsonPropertyName("_links")] MolliePaymentLinks? Links) : MollieIdResponse(Id);

    private sealed record MollieAmount([property: JsonPropertyName("value")] string? Value);

    private sealed record MolliePaymentLinks(
        [property: JsonPropertyName("checkout")] MollieLinkHref? Checkout);

    private sealed record MollieLinkHref(
        [property: JsonPropertyName("href")] string? Href);
}
