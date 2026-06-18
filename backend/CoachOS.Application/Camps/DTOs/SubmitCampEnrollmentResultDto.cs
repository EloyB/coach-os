namespace CoachOS.Application.Camps.DTOs;

/// <summary>
/// Resultaat van een kampinschrijving. <see cref="RequiresPayment"/> is true wanneer
/// het kamp betaald is (status PendingPayment) en de deelnemer nog een betaalkeuze
/// (cash of online) moet maken op de betaalpagina. Gratis kampen worden direct
/// bevestigd en geven false terug.
/// </summary>
public record SubmitCampEnrollmentResultDto(Guid CampEnrollmentId, bool RequiresPayment);
