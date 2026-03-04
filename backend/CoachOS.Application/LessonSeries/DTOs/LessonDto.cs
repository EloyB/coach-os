namespace CoachOS.Application.LessonSeries.DTOs;

public class LessonDto
{
    public Guid Id { get; set; }
    public Guid LessonSeriesId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string CourtName { get; set; } = string.Empty;
    public int MaxStudents { get; set; }
    public string? Notes { get; set; }
    public bool IsCancelled { get; set; }
}
