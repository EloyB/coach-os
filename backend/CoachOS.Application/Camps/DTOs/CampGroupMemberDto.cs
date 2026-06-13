namespace CoachOS.Application.Camps.DTOs;

public record CampGroupMemberDto
{
    public string ParticipantName { get; init; } = string.Empty;
    public string ParticipantEmail { get; init; } = string.Empty;
    public string? ParticipantPhone { get; init; }
    public List<CampFormResponseValueDto>? Responses { get; init; }
}
