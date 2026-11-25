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

    /// <summary>
    /// Maximum aantal personen (1 tot 4) waarmee de deelnemer of groep in één tijdslot wil zitten. Voor een groep
    /// geldt het voor iedereen. Null = de eigen groepsgrootte (niet samenvoegen met anderen).
    /// </summary>
    public int? MaxGroupSize { get; init; }
    public Guid? SelectedPriceOptionId { get; init; }
    public List<GroupMemberDto>? GroupMembers { get; init; }
}
