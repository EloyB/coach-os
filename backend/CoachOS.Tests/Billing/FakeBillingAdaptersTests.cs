using System.Net;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Invoicing;
using CoachOS.Infrastructure.Mollie;
using CoachOS.Infrastructure.Vat;
using CoachOS.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CoachOS.Tests.Billing;

[TestFixture]
public class FakeBillingAdaptersTests
{
    private static FakeBillingMollieClient CreateFakeMollie() => new(
        Options.Create(new AppOptions { FrontendBaseUrl = "https://app.test" }),
        TimeProvider.System,
        NullLogger<FakeBillingMollieClient>.Instance);

    private static BillingPaymentRequest Request() => new(
        30.25m, "EUR", "test", "https://app.test/return", null, "cst_fake",
        new Dictionary<string, string> { ["k"] = "v" });

    [Test]
    public async Task FakeMollie_NewPaymentIsOpenWithLocalCheckoutUrl()
    {
        FakeBillingMollieClient fake = CreateFakeMollie();

        BillingPaymentResult created = (await fake.CreateFirstPaymentAsync(Request())).Value!;
        BillingPaymentDetails details = (await fake.GetPaymentDetailsAsync(created.Id)).Value!;

        created.CheckoutUrl.Should().Be($"https://app.test/dashboard/settings/subscription?fakePayment={created.Id}");
        details.Status.Should().Be("open");
        details.SequenceType.Should().Be("first");
        details.Amount.Should().Be(30.25m);
        details.Metadata.Should().ContainKey("k");
    }

    [Test]
    public async Task FakeMollie_SetFakeStatusPaid_ReportsPaidWithPaidAt()
    {
        FakeBillingMollieClient fake = CreateFakeMollie();
        BillingPaymentResult created = (await fake.CreateFirstPaymentAsync(Request())).Value!;

        fake.SetFakeStatus(created.Id, "paid").Should().BeTrue();
        BillingPaymentDetails details = (await fake.GetPaymentDetailsAsync(created.Id)).Value!;

        details.Status.Should().Be("paid");
        details.PaidAt.Should().NotBeNull();
    }

    [Test]
    public async Task FakeMollie_UnknownPayment_Fails()
    {
        (await CreateFakeMollie().GetPaymentDetailsAsync("tr_onbekend")).Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public async Task FakeInvoicing_IsIdempotentPerPaymentAndNumbersIncrement()
    {
        FakeInvoicingService invoicing = new(TimeProvider.System, NullLogger<FakeInvoicingService>.Instance);
        InvoiceRequest first = InvoiceFor(Guid.NewGuid());
        InvoiceRequest second = InvoiceFor(Guid.NewGuid());

        CreatedInvoice a = (await invoicing.CreateInvoiceAsync(first)).Value!;
        CreatedInvoice aAgain = (await invoicing.CreateInvoiceAsync(first)).Value!;
        CreatedInvoice b = (await invoicing.CreateInvoiceAsync(second)).Value!;

        aAgain.Should().Be(a);
        a.Number.Should().MatchRegex(@"^FAKE-\d{4}-0001$");
        b.Number.Should().MatchRegex(@"^FAKE-\d{4}-0002$");
    }

    private static InvoiceRequest InvoiceFor(Guid paymentId) => new(
        paymentId,
        new BillingProfileSnapshot("TC Test", "Straat 1", "2000", "Antwerpen", "BE", "f@test.be", null),
        "CoachOS Game", new DateOnly(2026, 11, 5), new DateOnly(2026, 12, 4), 25m, 0.21m, true, null);

    [Test]
    public async Task Vies_ValidTrue_ReturnsValid()
    {
        RecordingHttpHandler handler = RecordingHttpHandler.Json(HttpStatusCode.OK, """{"valid":true}""");

        VatCheckResult result = await new ViesVatNumberValidator(new HttpClient(handler), NullLogger<ViesVatNumberValidator>.Instance)
            .CheckAsync("BE0123456789");

        result.Should().Be(VatCheckResult.Valid);
        handler.Requests[0].Body.Should().Contain("\"countryCode\":\"BE\"").And.Contain("\"vatNumber\":\"0123456789\"");
    }

    [Test]
    public async Task Vies_ValidFalse_ReturnsInvalid()
    {
        RecordingHttpHandler handler = RecordingHttpHandler.Json(HttpStatusCode.OK, """{"valid":false}""");

        (await new ViesVatNumberValidator(new HttpClient(handler), NullLogger<ViesVatNumberValidator>.Instance)
            .CheckAsync("BE0123456789")).Should().Be(VatCheckResult.Invalid);
    }

    [Test]
    public async Task Vies_ServerError_ReturnsUnavailable()
    {
        RecordingHttpHandler handler = RecordingHttpHandler.Json(HttpStatusCode.InternalServerError, "{}");

        (await new ViesVatNumberValidator(new HttpClient(handler), NullLogger<ViesVatNumberValidator>.Instance)
            .CheckAsync("BE0123456789")).Should().Be(VatCheckResult.Unavailable);
    }

    [Test]
    public async Task Vies_NetworkFailure_ReturnsUnavailable()
    {
        RecordingHttpHandler handler = new(_ => throw new HttpRequestException("down"));

        (await new ViesVatNumberValidator(new HttpClient(handler), NullLogger<ViesVatNumberValidator>.Instance)
            .CheckAsync("BE0123456789")).Should().Be(VatCheckResult.Unavailable);
    }
}
