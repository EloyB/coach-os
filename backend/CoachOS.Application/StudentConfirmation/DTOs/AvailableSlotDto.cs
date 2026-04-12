namespace CoachOS.Application.StudentConfirmation.DTOs;

public record AvailableSlotDto
{
    public Guid WeeklyTemplateEntryId { get; init; }
    public int DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string? CourtName { get; init; }
    public int RemainingCapacity { get; init; }
}
