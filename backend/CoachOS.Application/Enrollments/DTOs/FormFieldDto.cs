namespace CoachOS.Application.Enrollments.DTOs;

public class FormFieldDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Type { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
    public List<string>? Options { get; set; }
}
