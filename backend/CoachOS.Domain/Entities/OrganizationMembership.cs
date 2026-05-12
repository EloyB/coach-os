using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Koppelt een <c>ApplicationUser</c> aan een <c>Organization</c> met een rol.
/// Dezelfde user kan in meerdere organisaties zitten (multi-tenant membership).
/// </summary>
public class OrganizationMembership : BaseEntity
{
    /// <summary>Verwijst naar AspNetUsers.Id. Geen navigation — ApplicationUser leeft in Infrastructure.</summary>
    public Guid UserId { get; set; }

    public Guid OrganizationId { get; set; }

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// True wanneer deze user lessen mag krijgen toegewezen in deze org.
    /// Voor Role=Trainer altijd true. Voor Role=Admin opt-in via "Voeg mij toe als trainer".
    /// </summary>
    public bool IsTrainer { get; set; }

    public DateTime JoinedAt { get; set; }

    public Organization Organization { get; set; } = null!;
}
