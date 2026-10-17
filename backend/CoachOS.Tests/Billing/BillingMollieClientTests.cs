using System.Net;
using System.Text.Json;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Mollie;
using CoachOS.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CoachOS.Tests.Billing;

[TestFixture]
public class BillingMollieClientTests
{
    private const string PaymentJson = """
        {"id":"tr_1","status":"open","amount":{"value":"59.29","currency":"EUR"},
         "sequenceType":"first","customerId":"cst_1",
         "metadata":{"subscriptionPaymentId":"abc"},
         "_links":{"checkout":{"href":"https://www.mollie.com/checkout/tr_1"}}}
        """;

    private static BillingMollieClient Create(RecordingHttpHandler handler)
        => new(new HttpClient(handler),
            Options.Create(new MollieBillingOptions { ApiKey = "test_key", ApiBaseUrl = "https://api.mollie.test" }),
            NullLogger<BillingMollieClient>.Instance);

    private static BillingPaymentRequest Request(string? customerId = "cst_1") => new(
        Amount: 59.29m, Currency: "EUR", Description: "CoachOS Set (maand)",
        RedirectUrl: "https://app.test/return", WebhookUrl: "https://app.test/api/webhooks/mollie-billing",
        CustomerId: customerId, Metadata: new Dictionary<string, string> { ["subscriptionPaymentId"] = "abc" });

    [Test]
    public async Task CreateFirstPayment_SendsSequenceTypeFirstAndCustomerId()
    {
        RecordingHttpHandler handler = RecordingHttpHandler.Json(HttpStatusCode.Created, PaymentJson);

        Result<BillingPaymentResult> result = await Create(handler).CreateFirstPaymentAsync(Request());

        result.IsSuccess.Should().BeTrue();
        result.Value!.CheckoutUrl.Should().Be("https://www.mollie.com/checkout/tr_1");
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Url.Should().Be("https://api.mollie.test/v2/payments");
        using JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        body.RootElement.GetProperty("sequenceType").GetString().Should().Be("first");
        body.RootElement.GetProperty("customerId").GetString().Should().Be("cst_1");
        body.RootElement.GetProperty("amount").GetProperty("value").GetString().Should().Be("59.29");
    }

    [Test]
    public async Task CreateFirstPayment_WithoutCustomer_FailsWithoutCallingMollie()
    {
        RecordingHttpHandler handler = RecordingHttpHandler.Json(HttpStatusCode.Created, PaymentJson);

        Result<BillingPaymentResult> result = await Create(handler).CreateFirstPaymentAsync(Request(customerId: null));

        result.IsSuccess.Should().BeFalse();
        handler.Requests.Should().BeEmpty();
    }

    [Test]
    public async Task CreateSubscription_SendsIntervalStartDateAndAmountAsString()
    {
        RecordingHttpHandler handler = RecordingHttpHandler.Json(HttpStatusCode.Created, """{"id":"sub_1","status":"active"}""");

        Result<string> result = await Create(handler).CreateSubscriptionAsync(new BillingSubscriptionRequest(
            CustomerId: "cst_1", Amount: 30.25m, Interval: "1 month", StartDate: new DateOnly(2026, 12, 5),
            Description: "CoachOS Game (maand)", WebhookUrl: "https://app.test/api/webhooks/mollie-billing",
            Metadata: new Dictionary<string, string> { ["organizationId"] = "org" }));

        result.Value.Should().Be("sub_1");
        handler.Requests[0].Url.Should().Be("https://api.mollie.test/v2/customers/cst_1/subscriptions");
        using JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        body.RootElement.GetProperty("amount").GetProperty("value").GetString().Should().Be("30.25");
        body.RootElement.GetProperty("amount").GetProperty("currency").GetString().Should().Be("EUR");
        body.RootElement.GetProperty("interval").GetString().Should().Be("1 month");
        body.RootElement.GetProperty("startDate").GetString().Should().Be("2026-12-05");
        body.RootElement.GetProperty("webhookUrl").GetString().Should().Be("https://app.test/api/webhooks/mollie-billing");
    }

    [Test]
    public async Task CancelSubscription_SendsDeleteToCustomerSubscription()
    {
        RecordingHttpHandler handler = RecordingHttpHandler.Json(HttpStatusCode.OK, """{"id":"sub_1","status":"canceled"}""");

        Result result = await Create(handler).CancelSubscriptionAsync("cst_1", "sub_1");

        result.IsSuccess.Should().BeTrue();
        handler.Requests[0].Method.Should().Be(HttpMethod.Delete);
        handler.Requests[0].Url.Should().Be("https://api.mollie.test/v2/customers/cst_1/subscriptions/sub_1");
    }

    [Test]
    public async Task Refund_PostsAmountToPaymentRefunds()
    {
        RecordingHttpHandler handler = RecordingHttpHandler.Json(HttpStatusCode.Created, """{"id":"re_1","status":"queued"}""");

        Result result = await Create(handler).RefundPaymentAsync("tr_1", 59.29m, "Opzegging tijdens proef");

        result.IsSuccess.Should().BeTrue();
        handler.Requests[0].Url.Should().Be("https://api.mollie.test/v2/payments/tr_1/refunds");
        using JsonDocument body = JsonDocument.Parse(handler.Requests[0].Body!);
        body.RootElement.GetProperty("amount").GetProperty("value").GetString().Should().Be("59.29");
    }

    [Test]
    public async Task GetPaymentDetails_ParsesSequenceTypeSubscriptionAndMetadata()
    {
        RecordingHttpHandler handler = RecordingHttpHandler.Json(HttpStatusCode.OK, """
            {"id":"tr_2","status":"paid","amount":{"value":"30.25","currency":"EUR"},
             "paidAt":"2026-12-05T08:00:00+00:00","sequenceType":"recurring",
             "customerId":"cst_1","subscriptionId":"sub_1","metadata":{"organizationId":"org"}}
            """);

        Result<BillingPaymentDetails> result = await Create(handler).GetPaymentDetailsAsync("tr_2");

        BillingPaymentDetails details = result.Value!;
        details.Status.Should().Be("paid");
        details.Amount.Should().Be(30.25m);
        details.SequenceType.Should().Be("recurring");
        details.SubscriptionId.Should().Be("sub_1");
        details.CustomerId.Should().Be("cst_1");
        details.Metadata.Should().ContainKey("organizationId").WhoseValue.Should().Be("org");
        details.PaidAt.Should().Be(new DateTime(2026, 12, 5, 8, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task GetPaymentDetails_UnknownPayment_ReturnsNotFound()
    {
        RecordingHttpHandler handler = RecordingHttpHandler.Json(HttpStatusCode.NotFound, """{"status":404}""");

        Result<BillingPaymentDetails> result = await Create(handler).GetPaymentDetailsAsync("tr_x");

        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public async Task GetPaymentDetails_MollieError_ReturnsExternalService()
    {
        RecordingHttpHandler handler = RecordingHttpHandler.Json(HttpStatusCode.InternalServerError, """{"status":500}""");

        Result<BillingPaymentDetails> result = await Create(handler).GetPaymentDetailsAsync("tr_x");

        result.Errors[0].Code.Should().Be(ErrorCodes.ExternalService);
    }
}
