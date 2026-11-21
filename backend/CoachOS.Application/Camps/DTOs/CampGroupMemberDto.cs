namespace CoachOS.Application.Camps.DTOs;

public record CampGroupMemberDto
{
    public string ParticipantName { get; init; } = string.Empty;
    public string ParticipantEmail { get; init; } = string.Empty;
    public string? ParticipantPhone { get; init; }

    /// <summary>Geboortedatum, formaat yyyy-MM-dd (verplicht bij inschrijven; null in antwoorden voor oude inschrijvingen).</summary>
    public string? DateOfBirth { get; init; }
    public List<CampFormResponseValueDto>? Responses { get; init; }
}
