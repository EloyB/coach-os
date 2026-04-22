namespace CoachOS.Application.LessonSerie.DTOs;

public record UpdateLessonRequest
{
    public Guid? TrainerId { get; init; }
    public string? Date { get; init; }
    public string? StartTime { get; init; }
    public string? EndTime { get; init; }
    public string? CourtName { get; init; }
    public int? MaxStudents { get; init; }
    public string? Notes { get; init; }
    public bool? IsCancelled { get; init; }
}
