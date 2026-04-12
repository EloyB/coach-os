namespace CoachOS.Application.Planning.DTOs;

public record PlanningTimeSlotDto
{
    public Guid Id { get; init; }
    public int DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string? CourtName { get; init; }
    public Guid? TrainerId { get; init; }
    public int MaxCapacity { get; init; }
}
