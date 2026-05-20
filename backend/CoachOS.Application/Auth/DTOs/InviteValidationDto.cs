namespace CoachOS.Application.Auth.DTOs;

/// <summary>
/// Resultaat van een invite-token validatie. Bevat enkel velden die de FE nodig
/// heeft om de welkom-pagina te personaliseren — geen e-mail (anti-enumeration)
/// en geen membership-details.
/// </summary>
public record InviteValidationDto(string FirstName);
