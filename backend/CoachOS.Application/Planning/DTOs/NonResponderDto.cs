namespace CoachOS.Application.Planning.DTOs;

public record NonResponderDto
{
    public Guid AssignmentId { get; init; }
    public Guid EnrollmentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string? StudentPhone { get; init; }
    public bool IsGroup { get; init; }
    public int GroupSize { get; init; }
    public int DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string? CourtName { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsExpired { get; init; }
}
