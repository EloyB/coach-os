namespace CoachOS.Application.LessonSerie.DTOs;

public class WeeklyTemplateEntryDto
{
    public Guid Id { get; set; }
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public Guid? TrainerId { get; set; }
    public string? CourtName { get; set; }
    public int MaxStudents { get; set; }
}
