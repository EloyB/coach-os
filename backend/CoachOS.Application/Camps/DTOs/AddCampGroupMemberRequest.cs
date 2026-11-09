namespace CoachOS.Application.Camps.DTOs;

/// <summary>Beheerder voegt een deelnemer toe aan een bestaande kampgroep; het lid neemt de dagkeuze van de leider over.</summary>
public record AddCampGroupMemberRequest
{
    public string ParticipantName { get; init; } = string.Empty;
    public string ParticipantEmail { get; init; } = string.Empty;
    public string? ParticipantPhone { get; init; }

    public List<CampFormResponseValueDto> Responses { get; init; } = new();
}
