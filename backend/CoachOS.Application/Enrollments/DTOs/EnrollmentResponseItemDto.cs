namespace CoachOS.Application.Enrollments.DTOs;

public class EnrollmentResponseItemDto
{
    public string FieldLabel { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    /// <summary>0 = antwoord van deze persoon, 1 = algemeen antwoord van de inschrijving (bij de leider).</summary>
    public int Scope { get; set; }
}
