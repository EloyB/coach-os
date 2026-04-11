namespace CoachOS.Application.Planning.DTOs;

public record AssignedItemDto
{
    public Guid AssignmentId { get; init; }
    public Guid? EnrollmentId { get; init; }
    public Guid? GroupId { get; init; }
    public List<string> StudentNames { get; init; } = new();
    public string Status { get; init; } = string.Empty;
}
