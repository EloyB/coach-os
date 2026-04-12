namespace CoachOS.Application.Planning.DTOs;

public record CreateAssignmentRequest
{
    public Guid EnrollmentId { get; init; }
    public Guid WeeklyTemplateEntryId { get; init; }
}
