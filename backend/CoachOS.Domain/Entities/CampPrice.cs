using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Extra tarief van een kamp, bv. "Leden" of "Tweede kind": een vast kortingsbedrag op de pakketprijs
/// (of de hoofdprijs <see cref="Camp.Price"/> zonder pakket). De beheerder kiest per inschrijving
/// (<see cref="CampEnrollment.CampPriceId"/>) welk tarief geldt. Zonder keuze geldt geen korting.
/// </summary>
public class CampPrice : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampId { get; set; }

    /// <summary>Naam die in admin en publieke weergave getoond wordt.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Korte publieke uitleg voor wie of wanneer dit tarief geldt.</summary>
    public string? Description { get; set; }

    /// <summary>Korting in EUR, per deelnemer; de prijs wordt nooit negatief.</summary>
    public decimal Discount { get; set; }

    public int SortOrder { get; set; }

    // Navigation
    public Organization Organization { get; set; } = null!;
    public Camp Camp { get; set; } = null!;
}
