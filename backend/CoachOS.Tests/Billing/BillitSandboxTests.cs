using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Invoicing;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace CoachOS.Tests.Billing;

/// <summary>
/// Draait de echte client tegen de Billit-sandbox; opt-in via BILLIT_API_KEY en BILLIT_PARTY_ID.
/// De URL staat vast op de sandbox, zodat deze test nooit echte facturen kan maken.
/// Het Billit-account moet zijn identiteit bevestigd hebben, anders weigert Billit het versturen.
/// </summary>
[TestFixture]
public class BillitSandboxTests
{
    private const string SandboxUrl = "https://api.sandbox.billit.be";

    private BillitInvoicingService _service = null!;
    private HttpClient _httpClient = null!;

    [SetUp]
    public void SetUp()
    {
        string? apiKey = Environment.GetEnvironmentVariable("BILLIT_API_KEY");
        string? partyId = Environment.GetEnvironmentVariable("BILLIT_PARTY_ID");
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(partyId))
            Assert.Ignore("BILLIT_API_KEY/BILLIT_PARTY_ID niet gezet: Billit-sandboxtest overgeslagen.");

        _httpClient = new HttpClient();
        _service = new BillitInvoicingService(
            _httpClient,
            Options.Create(new BillitOptions { ApiKey = apiKey!, PartyId = partyId!, ApiBaseUrl = SandboxUrl }),
            TimeProvider.System,
            NullLogger<BillitInvoicingService>.Instance);
    }

    [TearDown]
    public void TearDown() => _httpClient?.Dispose();

    [Test]
    public async Task CreateInvoice_TwiceForSamePayment_CreatesOneNumberedInvoice()
    {
        InvoiceRequest request = new(
            Guid.NewGuid(),
            new BillingProfileSnapshot("CoachOS Testclub", "Teststraat 1", "9000", "Gent", "BE", "test@example.invalid", null),
            "CoachOS Set-abonnement (maand) test",
            new DateOnly(2026, 11, 4),
            new DateOnly(2026, 12, 3),
            AmountExclVat: 49.00m,
            VatRate: 0.21m,
            IsPaid: true,
            DueDate: null);

        Result<CreatedInvoice> first = await _service.CreateInvoiceAsync(request);
        Result<CreatedInvoice> second = await _service.CreateInvoiceAsync(request);

        first.IsSuccess.Should().BeTrue(first.IsSuccess ? string.Empty : first.Errors[0].Message);
        first.Value!.Number.Should().NotBeNullOrWhiteSpace();
        second.IsSuccess.Should().BeTrue(second.IsSuccess ? string.Empty : second.Errors[0].Message);
        second.Value!.Id.Should().Be(first.Value.Id);
        second.Value.Number.Should().Be(first.Value.Number);
    }
}
