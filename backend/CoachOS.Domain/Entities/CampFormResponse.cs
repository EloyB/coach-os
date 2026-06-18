using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

public class CampFormResponse : BaseEntity
{
    public Guid CampEnrollmentId { get; set; }
    public Guid CampFormFieldId { get; set; }
    public string Value { get; set; } = string.Empty;

    public CampEnrollment CampEnrollment { get; set; } = null!;
    public CampFormField CampFormField { get; set; } = null!;
}
