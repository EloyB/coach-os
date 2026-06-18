using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

public class CampEnrollmentForm : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampId { get; set; }

    public Camp Camp { get; set; } = null!;
    public ICollection<CampFormField> Fields { get; set; } = new List<CampFormField>();
}
