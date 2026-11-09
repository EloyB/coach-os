namespace CoachOS.Application.Camps.DTOs;

public record SubmitCampEnrollmentRequest
{
    public string ParticipantName { get; init; } = string.Empty;
    public string ParticipantEmail { get; init; } = string.Empty;
    public string? ParticipantPhone { get; init; }
    public List<CampFormResponseValueDto> Responses { get; init; } = new();
    /// <summary>Gekozen dagdelen per kampdag; geldt voor de leider en alle groepsleden.</summary>
    public List<CampDaySelectionDto> Days { get; init; } = new();
    public string EnrollmentType { get; init; } = "solo"; // "solo" | "group"
    public List<CampGroupMemberDto>? GroupMembers { get; init; }
}
