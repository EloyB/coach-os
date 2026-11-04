using System.Net;
using System.Text;
using System.Text.Json;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Invoicing;
using CoachOS.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CoachOS.Tests.Billing;

/// <summary>
/// De antwoorden volgen de echte Billit-sandbox (november 2026), ingekort tot de velden die
/// de client leest.
/// </summary>
[TestFixture]
public class BillitInvoicingServiceTests
{
    private const string BaseUrl = "https://api.billit.test";
    private const string OrdersPath = "/v1/orders";
    private const string SendPath = "/v1/orders/commands/send";
    private const string OrderJson = """{"OrderID":3522673,"OrderNumber":"17","IsSent":false,"Paid":false}""";
    private const string SentOrderJson = """{"OrderID":3522673,"OrderNumber":"17","IsSent":true,"Paid":true}""";
    private const string ConflictJson = """{"errors":[{"Code":"Idempotent token already exists"}]}""";
    private const string FoundJson = """{"Items":[{"OrderID":3522673,"OrderNumber":"17"}]}""";
    private const string IdentityErrorJson =
        """{"errors":[{"Code":"YouMustConfirmYourIdentityBeforeSendinAnInvoice","Description":"Confirm your identity"}]}""";

    private static readonly Guid PaymentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly string PaymentKey = PaymentId.ToString("N");

    private sealed class FixedTime(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    /// <summary>Instelbare Billit: standaard het gelukkige pad, per test één stap anders.</summary>
    private sealed class FakeBillit
    {
        public Func<HttpResponseMessage> CreateOrder { get; init; } = () => Respond(HttpStatusCode.OK, "3522673");
        public Func<HttpResponseMessage> FindOrder { get; init; } = () => Respond(HttpStatusCode.OK, FoundJson);
        public Func<HttpResponseMessage> GetOrder { get; init; } = () => Respond(HttpStatusCode.OK, OrderJson);
        public Func<HttpResponseMessage> Peppol { get; init; } =
            () => Respond(HttpStatusCode.OK, """{"Registered":true}""");
        public Func<HttpResponseMessage> Send { get; init; } = () => Respond(HttpStatusCode.OK);

        public List<HttpRequestMessage> Seen { get; } = [];
        public List<long?> BodyLengths { get; } = [];

        public RecordingHttpHandler Handler() => new(request =>
        {
            Seen.Add(request);
            if (request.Content is not null)
                BodyLengths.Add(request.Content.Headers.ContentLength);
            string path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Post && path == OrdersPath)
                return CreateOrder();
            if (request.Method == HttpMethod.Post && path == SendPath)
                return Send();
            if (path.StartsWith("/v1/peppol/", StringComparison.Ordinal))
                return Peppol();
            if (request.Method == HttpMethod.Get && path == OrdersPath)
                return FindOrder();
            if (request.Method == HttpMethod.Get)
                return GetOrder();
            return Respond(HttpStatusCode.OK);
        });
    }

    private static HttpResponseMessage Respond(HttpStatusCode status, string body = "")
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static BillitInvoicingService Create(RecordingHttpHandler handler)
        => new(new HttpClient(handler),
            Options.Create(new BillitOptions { ApiKey = "secret-key", PartyId = "123", ApiBaseUrl = BaseUrl }),
            new FixedTime(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<BillitInvoicingService>.Instance);

    private static InvoiceRequest Request(bool isPaid = true, string? vatNumber = "BE0563846944", DateOnly? dueDate = null)
        => new(
            PaymentId,
            new BillingProfileSnapshot("TC Test", "Teststraat 1", "9000", "Gent", "BE", "factuur@club.test", vatNumber),
            "CoachOS Set-abonnement (maand) 04/11/2026 - 03/12/2026",
            new DateOnly(2026, 11, 4),
            new DateOnly(2026, 12, 3),
            AmountExclVat: 49.00m,
            VatRate: 0.21m,
            IsPaid: isPaid,
            DueDate: dueDate);

    private static string BodyOf(RecordingHttpHandler handler, HttpMethod method, string path)
        => handler.Requests.Single(r => r.Method == method && r.Url == $"{BaseUrl}{path}").Body!;

    private static bool IsSend((HttpMethod Method, string Url, string? Body) request)
        => request.Url == $"{BaseUrl}{SendPath}";

    [Test]
    public void IsConfigured_IsTrue()
        => Create(new FakeBillit().Handler()).IsConfigured.Should().BeTrue();

    [Test]
    public async Task CreateInvoice_PostsOrderWithCustomerLineAndVatAsPercentage()
    {
        RecordingHttpHandler handler = new FakeBillit().Handler();

        await Create(handler).CreateInvoiceAsync(Request(isPaid: false, dueDate: new DateOnly(2026, 12, 4)));

        using JsonDocument body = JsonDocument.Parse(BodyOf(handler, HttpMethod.Post, OrdersPath));
        JsonElement root = body.RootElement;
        root.GetProperty("OrderType").GetString().Should().Be("Invoice");
        root.GetProperty("OrderDirection").GetString().Should().Be("Income");
        root.GetProperty("OrderDate").GetString().Should().Be("2026-11-04");
        root.GetProperty("ExpiryDate").GetString().Should().Be("2026-12-04");
        root.GetProperty("Reference").GetString().Should().Be(PaymentKey);
        root.TryGetProperty("OrderNumber", out _).Should().BeFalse("Billit nummert zelf door");

        JsonElement customer = root.GetProperty("Customer");
        customer.GetProperty("Name").GetString().Should().Be("TC Test");
        customer.GetProperty("VATNumber").GetString().Should().Be("BE0563846944");
        customer.GetProperty("PartyType").GetString().Should().Be("Customer");
        customer.GetProperty("Email").GetString().Should().Be("factuur@club.test");
        customer.GetProperty("Language").GetString().Should().Be("NL");

        JsonElement address = customer.GetProperty("Addresses")[0];
        address.GetProperty("AddressType").GetString().Should().Be("InvoiceAddress");
        address.GetProperty("Name").GetString().Should().Be("TC Test");
        address.GetProperty("Street").GetString().Should().Be("Teststraat 1");
        address.GetProperty("Zipcode").GetString().Should().Be("9000");
        address.GetProperty("City").GetString().Should().Be("Gent");
        address.GetProperty("CountryCode").GetString().Should().Be("BE");

        JsonElement line = root.GetProperty("OrderLines")[0];
        line.GetProperty("Quantity").GetDecimal().Should().Be(1m);
        line.GetProperty("UnitPriceExcl").GetDecimal().Should().Be(49.00m);
        line.GetProperty("VATPercentage").GetDecimal().Should().Be(21m);
        line.GetProperty("Description").GetString().Should().StartWith("CoachOS Set-abonnement");
    }

    [Test]
    public async Task CreateInvoice_SendsAuthOnEveryCallAndIdempotencyKeyOnCreate()
    {
        FakeBillit billit = new();

        await Create(billit.Handler()).CreateInvoiceAsync(Request());

        billit.Seen.Should().NotBeEmpty();
        billit.Seen.Should().OnlyContain(r => r.Headers.GetValues("ApiKey").Single() == "secret-key"
                                              && r.Headers.GetValues("PartyID").Single() == "123");
        billit.Seen[0].Headers.GetValues("Idempotency-Key").Single().Should().Be(PaymentKey);
    }

    [Test]
    public async Task CreateInvoice_SendsBodiesWithContentLength()
    {
        // Billit weigert een chunked body met "Invalid request body" (gezien op de sandbox).
        FakeBillit billit = new();

        await Create(billit.Handler()).CreateInvoiceAsync(Request());

        billit.BodyLengths.Should().NotBeEmpty();
        billit.BodyLengths.Should().OnlyContain(length => length > 0);
    }

    [Test]
    public async Task CreateInvoice_NoDueDate_UsesInvoiceDateAsExpiry()
    {
        RecordingHttpHandler handler = new FakeBillit().Handler();

        await Create(handler).CreateInvoiceAsync(Request(dueDate: null));

        using JsonDocument body = JsonDocument.Parse(BodyOf(handler, HttpMethod.Post, OrdersPath));
        body.RootElement.GetProperty("ExpiryDate").GetString().Should().Be("2026-11-04");
    }

    [Test]
    public async Task CreateInvoice_NoVatNumber_OmitsVatAndSendsByEmailWithoutPeppolLookup()
    {
        RecordingHttpHandler handler = new FakeBillit().Handler();

        Result<CreatedInvoice> result = await Create(handler).CreateInvoiceAsync(Request(vatNumber: null));

        result.IsSuccess.Should().BeTrue();
        using JsonDocument order = JsonDocument.Parse(BodyOf(handler, HttpMethod.Post, OrdersPath));
        order.RootElement.GetProperty("Customer").TryGetProperty("VATNumber", out _).Should().BeFalse();
        handler.Requests.Should().NotContain(r => r.Url.Contains("/peppol/", StringComparison.Ordinal));
        using JsonDocument send = JsonDocument.Parse(BodyOf(handler, HttpMethod.Post, SendPath));
        send.RootElement.GetProperty("Transporttype").GetString().Should().Be("SMTP");
    }

    [Test]
    public async Task CreateInvoice_RegisteredOnPeppol_SendsViaPeppol()
    {
        RecordingHttpHandler handler = new FakeBillit().Handler();

        await Create(handler).CreateInvoiceAsync(Request());

        handler.Requests.Should().Contain(r => r.Url == $"{BaseUrl}/v1/peppol/participantInformation/BE0563846944");
        using JsonDocument send = JsonDocument.Parse(BodyOf(handler, HttpMethod.Post, SendPath));
        send.RootElement.GetProperty("Transporttype").GetString().Should().Be("Peppol");
        send.RootElement.GetProperty("OrderIDs")[0].GetInt64().Should().Be(3522673);
    }

    [TestCase(HttpStatusCode.OK, """{"Registered":false}""")]
    [TestCase(HttpStatusCode.NotFound, "")]
    public async Task CreateInvoice_NotOnPeppol_FallsBackToEmail(HttpStatusCode status, string body)
    {
        RecordingHttpHandler handler = new FakeBillit { Peppol = () => Respond(status, body) }.Handler();

        Result<CreatedInvoice> result = await Create(handler).CreateInvoiceAsync(Request());

        result.IsSuccess.Should().BeTrue();
        using JsonDocument send = JsonDocument.Parse(BodyOf(handler, HttpMethod.Post, SendPath));
        send.RootElement.GetProperty("Transporttype").GetString().Should().Be("SMTP");
    }

    [Test]
    public async Task CreateInvoice_PeppolLookupServerError_FailsSoTheWorkerRetries()
    {
        RecordingHttpHandler handler = new FakeBillit
        {
            Peppol = () => Respond(HttpStatusCode.ServiceUnavailable),
        }.Handler();

        Result<CreatedInvoice> result = await Create(handler).CreateInvoiceAsync(Request());

        result.IsSuccess.Should().BeFalse();
        handler.Requests.Should().NotContain(r => IsSend(r));
    }

    [Test]
    public async Task CreateInvoice_Paid_MarksOrderPaidBeforeSending()
    {
        RecordingHttpHandler handler = new FakeBillit().Handler();

        await Create(handler).CreateInvoiceAsync(Request(isPaid: true));

        using JsonDocument patch = JsonDocument.Parse(BodyOf(handler, HttpMethod.Patch, $"{OrdersPath}/3522673"));
        patch.RootElement.GetProperty("Paid").GetBoolean().Should().BeTrue();
        int patchIndex = handler.Requests.FindIndex(r => r.Method == HttpMethod.Patch);
        int sendIndex = handler.Requests.FindIndex(r => IsSend(r));
        patchIndex.Should().BeLessThan(sendIndex);
    }

    [Test]
    public async Task CreateInvoice_Unpaid_DoesNotMarkOrderPaid()
    {
        RecordingHttpHandler handler = new FakeBillit().Handler();

        await Create(handler).CreateInvoiceAsync(Request(isPaid: false));

        handler.Requests.Should().NotContain(r => r.Method == HttpMethod.Patch);
    }

    [Test]
    public async Task CreateInvoice_ReturnsBillitIdAndNumberWithoutPdfLink()
    {
        Result<CreatedInvoice> result = await Create(new FakeBillit().Handler()).CreateInvoiceAsync(Request());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("3522673");
        result.Value.Number.Should().Be("17");
        result.Value.PdfUrl.Should().BeNull("een Billit-bestand is enkel met de API-key op te halen");
    }

    [Test]
    public async Task CreateInvoice_RetryAfterConflict_FindsExistingOrderByReference()
    {
        RecordingHttpHandler handler = new FakeBillit
        {
            CreateOrder = () => Respond(HttpStatusCode.Conflict, ConflictJson),
        }.Handler();

        Result<CreatedInvoice> result = await Create(handler).CreateInvoiceAsync(Request());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("3522673");
        string lookup = handler.Requests.Single(r => r.Url.Contains($"{OrdersPath}?", StringComparison.Ordinal)).Url;
        Uri.UnescapeDataString(lookup).Should().Contain($"$filter=Reference eq '{PaymentKey}'");
    }

    [Test]
    public async Task CreateInvoice_ConflictButOrderNotFound_Fails()
    {
        RecordingHttpHandler handler = new FakeBillit
        {
            CreateOrder = () => Respond(HttpStatusCode.Conflict, ConflictJson),
            FindOrder = () => Respond(HttpStatusCode.OK, """{"Items":[]}"""),
        }.Handler();

        Result<CreatedInvoice> result = await Create(handler).CreateInvoiceAsync(Request());

        result.IsSuccess.Should().BeFalse();
        handler.Requests.Should().NotContain(r => IsSend(r));
    }

    [Test]
    public async Task CreateInvoice_OrderWithoutNumber_FailsWithoutSending()
    {
        RecordingHttpHandler handler = new FakeBillit
        {
            GetOrder = () => Respond(HttpStatusCode.OK, """{"OrderID":3522673,"OrderNumber":"","IsSent":false,"Paid":false}"""),
        }.Handler();

        Result<CreatedInvoice> result = await Create(handler).CreateInvoiceAsync(Request());

        result.IsSuccess.Should().BeFalse();
        handler.Requests.Should().NotContain(r => IsSend(r));
    }

    [Test]
    public async Task CreateInvoice_AlreadySent_DoesNotSendAgain()
    {
        RecordingHttpHandler handler = new FakeBillit
        {
            GetOrder = () => Respond(HttpStatusCode.OK, SentOrderJson),
        }.Handler();

        Result<CreatedInvoice> result = await Create(handler).CreateInvoiceAsync(Request());

        result.IsSuccess.Should().BeTrue();
        handler.Requests.Should().NotContain(r => IsSend(r));
    }

    [Test]
    public async Task CreateInvoice_SendRejected_FailsAndKeepsBillitCodeInTheError()
    {
        RecordingHttpHandler handler = new FakeBillit
        {
            Send = () => Respond(HttpStatusCode.BadRequest, IdentityErrorJson),
        }.Handler();

        Result<CreatedInvoice> result = await Create(handler).CreateInvoiceAsync(Request());

        result.IsSuccess.Should().BeFalse();
        result.Errors.Single().Code.Should().Be(ErrorCodes.ExternalService);
        result.Errors.Single().Message.Should().Contain("YouMustConfirmYourIdentity");
        result.Errors.Single().Message.Should().Contain("Confirm your identity");
    }

    [Test]
    public async Task CreateInvoice_OrderRejected_FailsWithoutFurtherCalls()
    {
        RecordingHttpHandler handler = new FakeBillit
        {
            CreateOrder = () => Respond(HttpStatusCode.BadRequest, """{"errors":[{"Code":"InvalidVat"}]}"""),
        }.Handler();

        Result<CreatedInvoice> result = await Create(handler).CreateInvoiceAsync(Request());

        result.IsSuccess.Should().BeFalse();
        handler.Requests.Should().ContainSingle();
    }

    [Test]
    public async Task CreateInvoice_NetworkFailure_ReturnsFailureInsteadOfThrowing()
    {
        RecordingHttpHandler handler = new(_ => throw new HttpRequestException("geen netwerk"));

        Result<CreatedInvoice> result = await Create(handler).CreateInvoiceAsync(Request());

        result.IsSuccess.Should().BeFalse();
        result.Errors.Single().Code.Should().Be(ErrorCodes.ExternalService);
    }
}
