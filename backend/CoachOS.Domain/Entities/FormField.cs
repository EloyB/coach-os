using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

public class FormField : BaseEntity
{
    public Guid EnrollmentFormId { get; set; }
    public string Label { get; set; } = string.Empty;
    public FormFieldType Type { get; set; }
    public bool IsRequired { get; set; }

    /// <summary>Per persoon van de inschrijving, of één keer voor de hele inschrijving (bewaard bij de leider).</summary>
    public FormFieldScope Scope { get; set; } = FormFieldScope.PerParticipant;
    public int Order { get; set; }

    /// <summary>JSON array of option strings for MultipleChoice fields.</summary>
    public string? Options { get; set; }

    // Navigation properties
    public EnrollmentForm EnrollmentForm { get; set; } = null!;
    public ICollection<FormResponse> Responses { get; set; } = new List<FormResponse>();
}
