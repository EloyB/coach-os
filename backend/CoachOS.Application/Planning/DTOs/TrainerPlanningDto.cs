namespace CoachOS.Application.Planning.DTOs;

public record TrainerPlanningDto
{
    public Guid LessonSerieId { get; init; }
    public string LessonSerieName { get; init; } = string.Empty;
    public string PlanningStatus { get; init; } = string.Empty;
    public DateTime? PlanningLastEditedAt { get; init; }
    public List<PlanningTimeSlotDto> TimeSlots { get; init; } = [];
    public List<TrainerPlanningEnrollmentDto> Enrollments { get; init; } = [];
    public List<PlanningGroupDto> Groups { get; init; } = [];
    public List<PlanningAssignmentDto> Assignments { get; init; } = [];
    public List<PlanningConflictDto> Conflicts { get; init; } = [];
}
