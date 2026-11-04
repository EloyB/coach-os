using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Common;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Invoicing;

/// <summary>
/// Maakt abonnementsfacturen aan in Billit en verstuurt ze: via Peppol als de klant daarop staat,
/// anders per e-mail. Billit bepaalt het factuurnummer.
///
/// Elke stap is veilig te herhalen, want de worker probeert een mislukte factuur opnieuw: het
/// betaling-id is zowel <c>Idempotency-Key</c> als <c>Reference</c>. Een herhaalde aanmaak geeft
/// 409, waarna de bestaande factuur op die referentie wordt opgezocht en afgewerkt.
/// </summary>
public class BillitInvoicingService(
    HttpClient httpClient,
    IOptions<BillitOptions> options,
    TimeProvider timeProvider,
    ILogger<BillitInvoicingService> logger) : IInvoicingService
{
    private const string OrdersPath = "/v1/orders";
    private const string SendPath = "/v1/orders/commands/send";
    private const string TransportPeppol = "Peppol";
    private const string TransportEmail = "SMTP";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly BillitOptions _options = options.Value;

    public bool IsConfigured => true;

    public async Task<Result<CreatedInvoice>> CreateInvoiceAsync(InvoiceRequest request, CancellationToken ct = default)
    {
        string reference = request.PaymentId.ToString("N");

        Result<long> created = await CreateOrFindOrderAsync(request, reference, ct);
        if (!created.IsSuccess)
            return Result<CreatedInvoice>.Fail(created.Errors);

        long orderId = created.Value;
        Result<BillitOrder> order = await GetOrderAsync(orderId, ct);
        if (!order.IsSuccess)
            return Result<CreatedInvoice>.Fail(order.Errors);

        Result<bool> delivered = await MarkPaidAndSendAsync(request, orderId, order.Value!, ct);
        if (!delivered.IsSuccess)
            return Result<CreatedInvoice>.Fail(delivered.Errors);

        // Geen pdf-link: een Billit-bestand is enkel met de API-key op te halen.
        return Result<CreatedInvoice>.Ok(new CreatedInvoice(
            orderId.ToString(CultureInfo.InvariantCulture), order.Value!.OrderNumber!, null));
    }

    private async Task<Result<long>> CreateOrFindOrderAsync(InvoiceRequest request, string reference, CancellationToken ct)
    {
        BillitResponse response = await SendAsync(HttpMethod.Post, OrdersPath, BuildOrder(request, reference), reference, ct);
        if (response.Status == HttpStatusCode.Conflict)
            return await FindOrderAsync(reference, ct);
        if (!response.IsSuccess)
            return Result<long>.Fail(Failure("Kon de factuur niet aanmaken bij Billit", response));

        return long.TryParse(response.Body.Trim().Trim('"'), NumberStyles.None, CultureInfo.InvariantCulture, out long orderId)
            ? Result<long>.Ok(orderId)
            : Result<long>.Fail(Failure("Onverwacht antwoord van Billit bij het aanmaken van de factuur", response));
    }

    private async Task<Result<long>> FindOrderAsync(string reference, CancellationToken ct)
    {
        string filter = Uri.EscapeDataString($"Reference eq '{reference}'");
        BillitResponse response = await SendAsync(HttpMethod.Get, $"{OrdersPath}?$filter={filter}", null, null, ct);
        if (!response.IsSuccess)
            return Result<long>.Fail(Failure("Kon de bestaande factuur niet ophalen bij Billit", response));

        BillitOrder? order = Deserialize<BillitOrderList>(response.Body)?.Items?.FirstOrDefault();
        return order is null
            ? Result<long>.Fail(Failure("Bestaande factuur niet gevonden bij Billit", response))
            : Result<long>.Ok(order.OrderId);
    }

    private async Task<Result<BillitOrder>> GetOrderAsync(long orderId, CancellationToken ct)
    {
        BillitResponse response = await SendAsync(HttpMethod.Get, OrderPath(orderId), null, null, ct);
        BillitOrder? order = response.IsSuccess ? Deserialize<BillitOrder>(response.Body) : null;
        return string.IsNullOrWhiteSpace(order?.OrderNumber)
            ? Result<BillitOrder>.Fail(Failure("Kon de factuur met nummer niet ophalen bij Billit", response))
            : Result<BillitOrder>.Ok(order!);
    }

    private async Task<Result<bool>> MarkPaidAndSendAsync(
        InvoiceRequest request, long orderId, BillitOrder order, CancellationToken ct)
    {
        if (request.IsPaid && !order.Paid)
        {
            BillitResponse paid = await SendAsync(HttpMethod.Patch, OrderPath(orderId), new { Paid = true }, null, ct);
            if (!paid.IsSuccess)
                return Result<bool>.Fail(Failure("Kon de factuur niet als betaald markeren bij Billit", paid));
        }

        if (order.IsSent)
            return Result<bool>.Ok(true);

        Result<string> transport = await ChooseTransportAsync(request.Customer.VatNumber, ct);
        if (!transport.IsSuccess)
            return Result<bool>.Fail(transport.Errors);

        BillitResponse sent = await SendAsync(
            HttpMethod.Post, SendPath, new { Transporttype = transport.Value, OrderIDs = new[] { orderId } }, null, ct);
        return sent.IsSuccess
            ? Result<bool>.Ok(true)
            : Result<bool>.Fail(Failure("Kon de factuur niet versturen via Billit", sent));
    }

    private async Task<Result<string>> ChooseTransportAsync(string? vatNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vatNumber))
            return Result<string>.Ok(TransportEmail);

        BillitResponse response = await SendAsync(
            HttpMethod.Get, $"/v1/peppol/participantInformation/{Uri.EscapeDataString(vatNumber)}", null, null, ct);
        if (response.Status == HttpStatusCode.NotFound)
            return Result<string>.Ok(TransportEmail);
        if (!response.IsSuccess)
            return Result<string>.Fail(Failure("Kon de Peppol-registratie niet controleren bij Billit", response));

        bool registered = Deserialize<BillitParticipant>(response.Body)?.Registered == true;
        return Result<string>.Ok(registered ? TransportPeppol : TransportEmail);
    }

    private object BuildOrder(InvoiceRequest request, string reference)
    {
        BillingProfileSnapshot customer = request.Customer;
        DateOnly today = timeProvider.GetBrusselsToday();
        return new
        {
            OrderType = "Invoice",
            OrderDirection = "Income",
            OrderDate = FormatDate(today),
            ExpiryDate = FormatDate(request.DueDate ?? today),
            Reference = reference,
            Customer = new
            {
                customer.Name,
                VATNumber = string.IsNullOrWhiteSpace(customer.VatNumber) ? null : customer.VatNumber,
                PartyType = "Customer",
                Email = customer.InvoiceEmail,
                Language = "NL",
                Addresses = new[]
                {
                    new
                    {
                        AddressType = "InvoiceAddress",
                        customer.Name,
                        customer.Street,
                        Zipcode = customer.PostalCode,
                        customer.City,
                        CountryCode = customer.Country,
                    },
                },
            },
            OrderLines = new[]
            {
                new
                {
                    Quantity = 1m,
                    UnitPriceExcl = request.AmountExclVat,
                    request.Description,
                    VATPercentage = request.VatRate * 100m,
                },
            },
        };
    }

    private async Task<BillitResponse> SendAsync(
        HttpMethod method, string path, object? body, string? idempotencyKey, CancellationToken ct)
    {
        using HttpRequestMessage request = new(method, $"{_options.ApiBaseUrl.TrimEnd('/')}{path}");
        request.Headers.Add("ApiKey", _options.ApiKey);
        request.Headers.Add("PartyID", _options.PartyId);
        if (idempotencyKey is not null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (body is not null)
        {
            // StringContent en geen JsonContent: Billit weigert een chunked body, en JsonContent
            // stuurt geen Content-Length mee.
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, body.GetType(), JsonOptions), Encoding.UTF8, "application/json");
        }

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request, ct);
            string content = await response.Content.ReadAsStringAsync(ct);
            BillitResponse result = new(response.StatusCode, content);
            if (!result.IsSuccess && response.StatusCode is not (HttpStatusCode.Conflict or HttpStatusCode.NotFound))
                logger.LogWarning("Billit {Method} {Path} gaf {Status}: {Error}",
                    method, path.Split('?')[0], (int)response.StatusCode, result.ErrorDetail);
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Fout bij Billit {Method} {Path}", method, path.Split('?')[0]);
            return new BillitResponse(null, string.Empty);
        }
    }

    private static Error Failure(string message, BillitResponse response)
    {
        string detail = response.ErrorDetail
                        ?? (response.Status is null ? "Billit niet bereikbaar" : $"HTTP {(int)response.Status}");
        return new Error(ErrorCodes.ExternalService, $"{message}: {detail}");
    }

    private static string OrderPath(long orderId)
        => string.Create(CultureInfo.InvariantCulture, $"{OrdersPath}/{orderId}");

    private static string FormatDate(DateOnly date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static T? Deserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record BillitResponse(HttpStatusCode? Status, string Body)
    {
        public bool IsSuccess => Status is >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices;

        public string? ErrorDetail
        {
            get
            {
                BillitError? error = IsSuccess ? null : Deserialize<BillitErrors>(Body)?.Errors?.FirstOrDefault();
                return error is null ? null : string.Join(" - ", new[] { error.Code, error.Description }.OfType<string>());
            }
        }
    }

    private sealed record BillitOrder(
        [property: JsonPropertyName("OrderID")] long OrderId,
        string? OrderNumber,
        bool IsSent,
        bool Paid);

    private sealed record BillitOrderList(IReadOnlyList<BillitOrder>? Items);

    private sealed record BillitParticipant(bool Registered);

    private sealed record BillitErrors([property: JsonPropertyName("errors")] IReadOnlyList<BillitError>? Errors);

    private sealed record BillitError(string? Code, string? Description);
}
