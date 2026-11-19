namespace CoachOS.Application.Camps.DTOs;

/// <summary>Beheerder voegt een deelnemer toe aan een bestaande kampgroep.</summary>
public record AddCampGroupMemberRequest
{
    public string ParticipantName { get; init; } = string.Empty;
    public string ParticipantEmail { get; init; } = string.Empty;
    public string? ParticipantPhone { get; init; }

    /// <summary>Geboortedatum, formaat yyyy-MM-dd (verplicht).</summary>
    public string DateOfBirth { get; init; } = string.Empty;

    /// <summary>Gekozen tarief; null = hoofdprijs van het kamp.</summary>
    public Guid? CampPriceId { get; init; }
    public List<CampFormResponseValueDto> Responses { get; init; } = new();
}
