using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Bewijs dat een gebruiker namens de organisatie een juridisch document heeft aanvaard:
/// welk document, welke versie en wanneer. Wordt nooit gewijzigd — een nieuwe versie
/// aanvaarden levert een nieuwe rij op.
/// </summary>
public class TermsAcceptance : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>De gebruiker die aanvaardde. Plain Guid: ApplicationUser leeft in Infrastructure.</summary>
    public Guid UserId { get; set; }

    public LegalDocumentType DocumentType { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}
