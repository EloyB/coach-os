namespace CoachOS.Application.Planning.DTOs;

public record PlanningOverviewDto
{
    public string PlanningStatus { get; init; } = string.Empty;
    public List<SlotAssignmentDto> Slots { get; init; } = new();
    public List<UnassignedEnrollmentDto> Unassigned { get; init; } = new();
}
