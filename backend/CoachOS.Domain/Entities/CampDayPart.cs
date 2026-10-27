using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Vast dagdeel van een kampdag (bv. ochtend 09:00-12:00). Wordt samen met de dag vervangen bij een
/// kampwijziging; pakketten verwijzen daarom naar (datum, <see cref="Order"/>) i.p.v. naar dit id.
/// </summary>
public class CampDayPart : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampDayId { get; set; }

    /// <summary>Positie binnen de dag, vanaf 0.</summary>
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // Navigation
    public CampDay CampDay { get; set; } = null!;
}
