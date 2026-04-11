namespace CoachOS.Application.Planning.DTOs;

public record UnassignedEnrollmentDto
{
    public Guid EnrollmentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
