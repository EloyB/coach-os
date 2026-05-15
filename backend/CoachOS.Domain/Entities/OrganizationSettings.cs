using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Per-organisatie instellingen. 1-1 relatie met <see cref="Organization"/>.
/// Nieuwe org-brede toggles worden hier toegevoegd zodat <see cref="Organization"/> beperkt blijft tot identiteit.
/// </summary>
public class OrganizationSettings : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// True wanneer de admin van deze organisatie automatisch in de trainerlijst verschijnt
    /// en lessen toegewezen kan krijgen. Default true: een tennisschool runt typisch op één
    /// admin-coach combinatie. Zet op false wanneer de admin puur administratief is.
    /// </summary>
    public bool AdminsActAsTrainers { get; set; } = true;

    public Organization Organization { get; set; } = null!;
}
