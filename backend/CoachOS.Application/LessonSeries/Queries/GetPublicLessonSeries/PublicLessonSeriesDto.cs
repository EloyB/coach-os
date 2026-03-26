using CoachOS.Application.LessonSeries.DTOs;

namespace CoachOS.Application.LessonSeries.Queries.GetPublicLessonSeries;

public class PublicLessonSeriesDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public int Level { get; set; }
    public decimal Price { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string TennisClubName { get; set; } = string.Empty;
    public int EnrollmentCount { get; set; }
    public List<LessonDto> Lessons { get; set; } = [];
}
