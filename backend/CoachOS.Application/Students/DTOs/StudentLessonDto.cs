namespace CoachOS.Application.Students.DTOs;

public class StudentLessonDto
{
    public Guid AssignmentId { get; set; }
    public Guid SeriesId { get; set; }
    public string SeriesName { get; set; } = string.Empty;
    public string SeriesStartDate { get; set; } = string.Empty;
    public string SeriesEndDate { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? CourtName { get; set; }
    public string? TrainerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsGroup { get; set; }
    public int GroupSize { get; set; }
    public decimal Price { get; set; }
    public string? PaymentStatus { get; set; }
}
