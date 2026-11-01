using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>De gekozen dagdelen van één kampdag voor één inschrijving (minstens één dagdeel).</summary>
public class CampEnrollmentDay : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampEnrollmentId { get; set; }
    public Guid CampDayId { get; set; }
    public bool Morning { get; set; }
    public bool Afternoon { get; set; }

    // Navigation
    public CampEnrollment CampEnrollment { get; set; } = null!;
    public CampDay CampDay { get; set; } = null!;
}
