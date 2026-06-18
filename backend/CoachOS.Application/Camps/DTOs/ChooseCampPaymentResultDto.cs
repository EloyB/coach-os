namespace CoachOS.Application.Camps.DTOs;

/// <summary>
/// Resultaat van een betaalkeuze. <see cref="CheckoutUrl"/> is de Mollie-checkout-URL
/// bij een online betaling, of null bij een cash-betaling (de coach bevestigt die later).
/// </summary>
public record ChooseCampPaymentResultDto(string? CheckoutUrl);
