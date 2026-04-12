namespace CoachOS.Application.Planning.DTOs;

public record PlanningOverviewDto
{
    public string PlanningStatus { get; init; } = string.Empty;
    public DateTime? PlanningLastEditedAt { get; init; }
    public List<PlanningTimeSlotDto> TimeSlots { get; init; } = new();
    public List<PlanningEnrollmentDto> Enrollments { get; init; } = new();
    public List<PlanningGroupDto> Groups { get; init; } = new();
    public List<PlanningAssignmentDto> Assignments { get; init; } = new();
    public List<PlanningConflictDto> Conflicts { get; init; } = new();
}
