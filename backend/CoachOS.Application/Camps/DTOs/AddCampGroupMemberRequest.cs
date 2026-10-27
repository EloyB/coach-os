namespace CoachOS.Application.Camps.DTOs;

/// <summary>Beheerder voegt een deelnemer toe aan een bestaande kampgroep.</summary>
public record AddCampGroupMemberRequest
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
