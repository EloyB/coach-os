namespace CoachOS.Application.Reschedule.DTOs;

public class RescheduleRequestDto
{
    public Guid Id { get; set; }
    public Guid EnrollmentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public Guid ScheduleAssignmentId { get; set; }
    public string CurrentSlotDay { get; set; } = string.Empty;
    public string CurrentSlotTime { get; set; } = string.Empty;
    public Guid? AlternativeWeeklyTemplateEntryId { get; set; }
    public string? AlternativeSlotDay { get; set; }
    public string? AlternativeSlotTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
