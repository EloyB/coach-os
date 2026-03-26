namespace CoachOS.Application.Enrollments.DTOs;

public class LessonSeriesEnrollmentDto
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public string? Notes { get; set; }
    public List<EnrollmentResponseItemDto> FormResponses { get; set; } = new();
}
