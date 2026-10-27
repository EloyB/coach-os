namespace CoachOS.Application.Camps.DTOs;

public record SubmitCampEnrollmentRequest
{
    public string ParticipantName { get; init; } = string.Empty;
    public string ParticipantEmail { get; init; } = string.Empty;
    public string? ParticipantPhone { get; init; }
    public List<CampFormResponseValueDto> Responses { get; init; } = new();
    /// <summary>Gekozen pakket van de leider; verplicht als het kamp pakketten heeft.</summary>
    public Guid? CampPackageId { get; init; }
    public string EnrollmentType { get; init; } = "solo"; // "solo" | "group"
    public List<CampGroupMemberDto>? GroupMembers { get; init; }
}
