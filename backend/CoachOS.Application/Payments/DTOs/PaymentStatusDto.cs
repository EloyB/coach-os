namespace CoachOS.Application.Payments.DTOs;

/// <summary>
/// Status van een payment voor de publieke thank-you-page polling. <see cref="Status"/>
/// is de string-representatie van <c>PaymentStatus</c> enum
/// (<c>Pending</c>, <c>Paid</c>, <c>Failed</c>, <c>Refunded</c>) zodat de FE
/// niet hoeft te speculeren over numerieke waarden.
/// </summary>
public record PaymentStatusDto(
    Guid PaymentId,
    string Status,
    decimal Amount,
    string Currency,
    string? CheckoutUrl,
    DateTime? PaidAt,
    string? FailureReason);
