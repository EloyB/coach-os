namespace CoachOS.Application.Camps.DTOs;

/// <summary>
/// Betaalopties voor een kamp, getoond op de publieke betaalpagina. <see cref="Price"/>
/// is de prijs per deelnemer; <see cref="OnlineAvailable"/> is true wanneer de
/// organisatie van het kamp aan Mollie gekoppeld is.
/// </summary>
public record CampPaymentOptionsDto(decimal Price, bool OnlineAvailable);
