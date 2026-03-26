namespace CoachOS.Application.LessonSeries.DTOs;

public record CreateLessonRequest
{
    public string Date { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string CourtName { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
