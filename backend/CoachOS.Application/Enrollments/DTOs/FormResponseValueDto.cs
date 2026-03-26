namespace CoachOS.Application.Enrollments.DTOs;

public class FormResponseValueDto
{
    public Guid FormFieldId { get; set; }
    public string Value { get; set; } = string.Empty;
}
