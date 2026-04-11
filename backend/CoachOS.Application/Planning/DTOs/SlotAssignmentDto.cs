namespace CoachOS.Application.Planning.DTOs;

public record SlotAssignmentDto
{
    public Guid WeeklyTemplateEntryId { get; init; }
    public int DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string? CourtName { get; init; }
    public Guid? TrainerId { get; init; }
    public int MaxCapacity { get; init; }
    public int CurrentCount { get; init; }
    public List<AssignedItemDto> Assignments { get; init; } = new();
}
