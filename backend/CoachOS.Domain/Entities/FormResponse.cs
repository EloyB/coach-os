using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

public class FormResponse : BaseEntity
{
    public Guid EnrollmentId { get; set; }
    public Guid FormFieldId { get; set; }
    public string Value { get; set; } = string.Empty;

    // Navigation properties
    public Enrollment Enrollment { get; set; } = null!;
    public FormField FormField { get; set; } = null!;
}
