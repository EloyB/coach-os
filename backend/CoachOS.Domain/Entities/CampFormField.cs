using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

public class CampFormField : BaseEntity
{
    public Guid CampEnrollmentFormId { get; set; }
    public string Label { get; set; } = string.Empty;
    public FormFieldType Type { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }

    /// <summary>JSON array of option strings for MultipleChoice fields.</summary>
    public string? Options { get; set; }

    public CampEnrollmentForm CampEnrollmentForm { get; set; } = null!;
    public ICollection<CampFormResponse> Responses { get; set; } = new List<CampFormResponse>();
}
