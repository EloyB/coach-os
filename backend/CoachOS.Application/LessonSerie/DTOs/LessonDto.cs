namespace CoachOS.Application.LessonSerie.DTOs;

public class LessonDto
{
    public Guid Id { get; set; }
    public Guid LessonSerieId { get; set; }
    public Guid TrainerId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string CourtName { get; set; } = string.Empty;
    public int? Level { get; set; }
    public int MaxStudents { get; set; }
    public string? Notes { get; set; }
    public bool IsCancelled { get; set; }
}
