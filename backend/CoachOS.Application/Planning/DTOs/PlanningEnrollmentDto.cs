namespace CoachOS.Application.Planning.DTOs;

public record PlanningEnrollmentDto
{
    public Guid Id { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string? StudentPhone { get; init; }
    public bool IsOpenToGrouping { get; init; }
    public Guid? GroupId { get; init; }
    /// <summary>Map of WeeklyTemplateEntryId → "Preferred" | "Available" | "Unavailable". Slots not in the map have no stated preference.</summary>
    public Dictionary<Guid, string> Preferences { get; init; } = new();
}
