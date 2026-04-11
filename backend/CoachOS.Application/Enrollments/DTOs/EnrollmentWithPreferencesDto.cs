namespace CoachOS.Application.Enrollments.DTOs;

public record EnrollmentWithPreferencesDto
{
    public Guid Id { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsOpenToGrouping { get; init; }
    public Guid? EnrollmentGroupId { get; init; }
    public string? GroupName { get; init; }
    public bool IsGroupLeader { get; init; }
    public List<TimeSlotPreferenceDto> Preferences { get; init; } = new();
}
