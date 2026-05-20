namespace CoachOS.Application.Enrollments.DTOs;

/// <summary>
/// Resultaat van een succesvolle <c>SubmitEnrollment</c>. Wanneer de
/// onderliggende lesreeks <c>PaymentMode.Immediate</c> heeft én een Mollie
/// payment succesvol kon worden aangemaakt, bevat <see cref="CheckoutUrl"/>
/// de Mollie hosted-checkout URL — de FE redirect de browser daarheen. Bij
/// <c>null</c> blijft de student op de bevestigingspagina (Deferred mode,
/// gratis reeks, of fallback).
/// </summary>
public record SubmitEnrollmentResponse(
    Guid EnrollmentId,
    string? CheckoutUrl);
