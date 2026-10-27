using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>Anonieme inschrijving voor een kamp (mirror van Enrollment).</summary>
public class CampEnrollment : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampId { get; set; }

    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public string? ParticipantPhone { get; set; }

    public EnrollmentStatus Status { get; set; }
    public DateTime EnrolledAt { get; set; }
    public string? Notes { get; set; }

    public Guid? CampEnrollmentGroupId { get; set; }

    /// <summary>Door de beheerder gekozen tarief; null = hoofdprijs van het kamp.</summary>
    public Guid? CampPriceId { get; set; }

    /// <summary>Gekozen pakket; null = hele kamp tegen de hoofdprijs (telt voor alle dagdelen).</summary>
    public Guid? CampPackageId { get; set; }

    // Navigation
    public Camp Camp { get; set; } = null!;
    public CampPrice? Price { get; set; }
    public CampPackage? Package { get; set; }
    public CampEnrollmentGroup? Group { get; set; }
    public ICollection<CampFormResponse> FormResponses { get; set; } = new List<CampFormResponse>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
