using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>Eén dagdeel in een pakket, geïdentificeerd door datum en <see cref="CampDayPart.Order"/>.</summary>
public class CampPackageSlot : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampPackageId { get; set; }
    public DateOnly Date { get; set; }
    public int PartOrder { get; set; }

    // Navigation
    public CampPackage CampPackage { get; set; } = null!;
}
