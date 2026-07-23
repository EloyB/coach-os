namespace CoachOS.Application.Enrollments.DTOs;

public record SubmitEnrollmentRequest
{
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string? StudentPhone { get; init; }

    /// <summary>
    /// Geboortedatum in formaat yyyy-MM-dd. Verplicht: bepaalt de tariefcategorie
    /// (volwassene/jeugd) en daarmee het bedrag dat de deelnemer betaalt.
    /// </summary>
    public string DateOfBirth { get; init; } = string.Empty;

    public List<FormResponseValueDto> Responses { get; init; } = new();
    public List<TimeSlotPreferenceDto>? TimeSlotPreferences { get; init; }
    public string EnrollmentType { get; init; } = "solo";
    public bool IsOpenToGrouping { get; init; }
    public List<GroupMemberDto>? GroupMembers { get; init; }
}
