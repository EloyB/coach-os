using CoachOS.Application.LessonSerie.DTOs;

namespace CoachOS.Application.Enrollments.DTOs;

public class PublicLessonSerieDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Level { get; set; }
    public decimal Price { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public DateTime RegistrationDeadline { get; set; }
    public string TennisClubName { get; set; } = string.Empty;
    public int EnrollmentCount { get; set; }
    public List<LessonDto> Lessons { get; set; } = [];
}
