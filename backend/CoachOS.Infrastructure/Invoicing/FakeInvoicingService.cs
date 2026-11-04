using System.Collections.Concurrent;
using CoachOS.Application.Abstractions;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Invoicing;

/// <summary>
/// Placeholder voor Billit zolang er geen account/API-toegang is. Geeft oplopende
/// nummers <c>FAKE-{jaar}-{0001}</c> terug, idempotent per betaling, zonder pdf.
/// </summary>
public class FakeInvoicingService(TimeProvider timeProvider, ILogger<FakeInvoicingService> logger) : IInvoicingService
{
    private readonly ConcurrentDictionary<Guid, CreatedInvoice> _invoices = new();
    private int _counter;

    public bool IsConfigured => false;

    public Task<Result<CreatedInvoice>> CreateInvoiceAsync(InvoiceRequest request, CancellationToken ct = default)
    {
        CreatedInvoice invoice = _invoices.GetOrAdd(request.PaymentId, _ =>
        {
            int number = Interlocked.Increment(ref _counter);
            int year = timeProvider.GetUtcNow().Year;
            logger.LogInformation("FakeInvoicingService: factuur {Number} voor {Customer} ({Amount} excl. btw)",
                number, request.Customer.Name, request.AmountExclVat);
            return new CreatedInvoice($"fake_{request.PaymentId:N}", $"FAKE-{year}-{number:D4}", null);
        });
        return Task.FromResult(Result<CreatedInvoice>.Ok(invoice));
    }
}
