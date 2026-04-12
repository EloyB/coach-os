using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Eenmalige token die per ScheduleAssignment (of groepsleider) naar de student
/// gestuurd wordt om de planning te bevestigen of af te wijzen.
/// </summary>
public class AssignmentConfirmationToken : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ScheduleAssignmentId { get; set; }
    public Guid EnrollmentId { get; set; }

    /// <summary>SHA256 hash van de token (hex, 64 chars). De ruwe token gaat enkel per e-mail.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public ConfirmationResponse Response { get; set; } = ConfirmationResponse.Pending;

    // Navigation
    public Organization Organization { get; set; } = null!;
    public ScheduleAssignment ScheduleAssignment { get; set; } = null!;
    public Enrollment Enrollment { get; set; } = null!;
}
