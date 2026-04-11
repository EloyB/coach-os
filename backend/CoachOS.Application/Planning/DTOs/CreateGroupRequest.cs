namespace CoachOS.Application.Planning.DTOs;

public record CreateGroupRequest
{
    public List<Guid> EnrollmentIds { get; init; } = new();
}
