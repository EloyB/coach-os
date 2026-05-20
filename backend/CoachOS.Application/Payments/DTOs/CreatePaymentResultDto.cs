namespace CoachOS.Application.Payments.DTOs;

/// <summary>
/// Antwoord van <c>CreatePaymentForEnrollmentAsync</c>. <see cref="CheckoutUrl"/>
/// is de Mollie hosted-checkout URL waar de FE de student naartoe redirect.
/// </summary>
public record CreatePaymentResultDto(
    Guid PaymentId,
    string CheckoutUrl);
