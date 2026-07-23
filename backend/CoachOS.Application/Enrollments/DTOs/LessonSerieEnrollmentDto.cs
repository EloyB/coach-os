namespace CoachOS.Application.Enrollments.DTOs;

public class LessonSerieEnrollmentDto
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public string? Notes { get; set; }

    /// <summary>Geboortedatum als yyyy-MM-dd, null voor inschrijvingen van vóór deze feature.</summary>
    public string? DateOfBirth { get; set; }

    /// <summary>1 = volwassenen, 2 = jeugd, null = onbekend.</summary>
    public int? Category { get; set; }

    public string? CategoryLabel { get; set; }
    public List<EnrollmentResponseItemDto> FormResponses { get; set; } = new();
}
