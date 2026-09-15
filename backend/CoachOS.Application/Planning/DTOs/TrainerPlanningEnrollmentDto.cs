namespace CoachOS.Application.Planning.DTOs;

public record TrainerPlanningEnrollmentDto
{
    public Guid Id { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public bool IsOpenToGrouping { get; init; }
    public Guid? GroupId { get; init; }
}
