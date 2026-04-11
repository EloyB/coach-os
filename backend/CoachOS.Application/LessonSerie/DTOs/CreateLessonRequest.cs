namespace CoachOS.Application.LessonSerie.DTOs;

public record CreateLessonRequest
{
    public Guid TrainerId { get; init; }
    public string Date { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string CourtName { get; init; } = string.Empty;
    public int? Level { get; init; }
    public string? Notes { get; init; }
}
