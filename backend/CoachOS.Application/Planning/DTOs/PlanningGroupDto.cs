namespace CoachOS.Application.Planning.DTOs;

public record PlanningGroupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid LeaderEnrollmentId { get; init; }
    public List<Guid> MemberEnrollmentIds { get; init; } = new();
}
