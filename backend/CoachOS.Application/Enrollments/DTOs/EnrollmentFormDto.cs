namespace CoachOS.Application.Enrollments.DTOs;

public class EnrollmentFormDto
{
    public Guid Id { get; set; }
    public Guid LessonSeriesId { get; set; }
    public List<FormFieldDto> Fields { get; set; } = new();
}
