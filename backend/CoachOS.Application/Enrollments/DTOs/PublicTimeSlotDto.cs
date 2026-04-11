namespace CoachOS.Application.Enrollments.DTOs;

public record PublicTimeSlotDto
{
    public Guid Id { get; init; }
    public int DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string? CourtName { get; init; }
    public int MaxStudents { get; init; }
}
