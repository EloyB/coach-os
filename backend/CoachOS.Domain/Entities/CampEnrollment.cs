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

    // Navigation
    public Camp Camp { get; set; } = null!;
    public CampEnrollmentGroup? Group { get; set; }
    public ICollection<CampFormResponse> FormResponses { get; set; } = new List<CampFormResponse>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
