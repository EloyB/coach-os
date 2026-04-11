namespace CoachOS.Application.Planning.DTOs;

public record UpdateAssignmentRequest
{
    public Guid WeeklyTemplateEntryId { get; init; }
}
