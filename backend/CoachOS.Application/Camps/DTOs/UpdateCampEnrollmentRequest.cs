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

    /// <summary>Gekozen tarief; null = hoofdprijs van het kamp.</summary>
    public Guid? CampPriceId { get; init; }

    /// <summary>Gekozen pakket; null = hele kamp tegen de hoofdprijs (telt voor alle dagdelen).</summary>
    public Guid? CampPackageId { get; init; }
    public List<CampFormResponseValueDto> Responses { get; init; } = new();
}
