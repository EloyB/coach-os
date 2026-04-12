namespace CoachOS.Application.Planning.DTOs;

public record PlanningAssignmentDto
{
    public Guid Id { get; init; }
    public Guid TimeSlotId { get; init; }
    public Guid? EnrollmentId { get; init; }
    public Guid? GroupId { get; init; }
    public string Status { get; init; } = string.Empty;
}
