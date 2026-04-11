namespace CoachOS.Application.LessonSerie.DTOs;

public record WeeklyTemplateEntryRequest
{
    public int DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public Guid? TrainerId { get; init; }
    public string? CourtName { get; init; }
    public int MaxStudents { get; init; }
}
