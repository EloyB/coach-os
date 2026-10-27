using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Vast pakket van een kamp: een prijs voor een vaste set dagdelen (bv. "Ochtenden" of "Maandag en dinsdag").
/// De deelnemer kiest bij het inschrijven een pakket (<see cref="CampEnrollment.CampPackageId"/>).
/// </summary>
public class CampPackage : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Prijs per deelnemer in EUR, vóór een eventuele korting (<see cref="CampPrice.Discount"/>).</summary>
    public decimal Price { get; set; }

    public int SortOrder { get; set; }

    // Navigation
    public Camp Camp { get; set; } = null!;
    public ICollection<CampPackageSlot> Slots { get; set; } = new List<CampPackageSlot>();
}
