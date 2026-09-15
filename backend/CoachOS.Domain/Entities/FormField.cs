using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

public class FormField : BaseEntity
{
    public Guid EnrollmentFormId { get; set; }
    public string Label { get; set; } = string.Empty;
    public FormFieldType Type { get; set; }
    public bool IsRequired { get; set; }
    public bool IsForEachGroupMember { get; set; }
    public int Order { get; set; }

    /// <summary>JSON array of option strings for MultipleChoice fields.</summary>
    public string? Options { get; set; }

    // Navigation properties
    public EnrollmentForm EnrollmentForm { get; set; } = null!;
    public ICollection<FormResponse> Responses { get; set; } = new List<FormResponse>();
}
