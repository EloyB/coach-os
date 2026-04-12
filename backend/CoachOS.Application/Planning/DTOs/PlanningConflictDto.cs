namespace CoachOS.Application.Planning.DTOs;

public record PlanningConflictDto
{
    public Guid? EnrollmentId { get; init; }
    public Guid? TimeSlotId { get; init; }
    /// <summary>"no_viable_slot" | "oversubscribed"</summary>
    public string Type { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
