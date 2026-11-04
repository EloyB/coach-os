using CoachOS.Domain.Models;

namespace CoachOS.Application.Abstractions;

/// <summary>
/// Facturatie van CoachOS-abonnementen (Billit). Idempotent per <see cref="InvoiceRequest.PaymentId"/>:
/// dezelfde betaling levert nooit twee facturen op.
/// </summary>
public interface IInvoicingService
{
    /// <summary><c>false</c> = lokale fake zonder echte facturen.</summary>
    bool IsConfigured { get; }

    Task<Result<CreatedInvoice>> CreateInvoiceAsync(InvoiceRequest request, CancellationToken ct = default);
}

/// <summary>Eén factuurlijn voor één abonnementsperiode. Bedrag excl. btw.</summary>
public sealed record InvoiceRequest(
    Guid PaymentId,
    BillingProfileSnapshot Customer,
    string Description,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal AmountExclVat,
    decimal VatRate,
    bool IsPaid,
    DateOnly? DueDate);

public sealed record BillingProfileSnapshot(
    string Name,
    string Street,
    string PostalCode,
    string City,
    string Country,
    string InvoiceEmail,
    string? VatNumber);

public sealed record CreatedInvoice(string Id, string Number, string? PdfUrl);
