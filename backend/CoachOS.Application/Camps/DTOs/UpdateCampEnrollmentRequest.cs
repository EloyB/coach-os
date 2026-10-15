namespace CoachOS.Application.Camps.DTOs;

/// <summary>
/// Beheerder past de basisgegevens en formulierantwoorden van een kamp-inschrijving aan.
/// <see cref="Responses"/> is de volledige set antwoorden: een veld dat ontbreekt of leeg is
/// wordt gewist (enkel toegestaan voor niet-verplichte velden). Betaling en status blijven ongewijzigd.
/// </summary>
public record UpdateCampEnrollmentRequest
{
    public string ParticipantName { get; init; } = string.Empty;
    public string ParticipantEmail { get; init; } = string.Empty;
    public string? ParticipantPhone { get; init; }
    public List<CampFormResponseValueDto> Responses { get; init; } = new();
}
