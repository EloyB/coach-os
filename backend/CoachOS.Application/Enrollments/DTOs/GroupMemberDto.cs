namespace CoachOS.Application.Enrollments.DTOs;

public record GroupMemberDto
{
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string? StudentPhone { get; init; }
    public List<FormResponseValueDto>? Responses { get; init; }
}
