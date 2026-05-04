using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Eenmalige uitnodiging naar één deelnemer voor een standalone les.
/// De ruwe token gaat enkel per e-mail; in de DB staat enkel de SHA256-hash.
/// </summary>
public class LessonInvitation : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid LessonId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }

    /// <summary>SHA256 hash van de token (hex, 64 chars). De ruwe token gaat enkel per e-mail.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public LessonInvitationStatus Status { get; set; } = LessonInvitationStatus.Pending;
    public DateTime InvitationSentAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    // Navigation
    public Organization Organization { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
}
