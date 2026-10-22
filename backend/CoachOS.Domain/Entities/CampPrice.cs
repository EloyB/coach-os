using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Extra tarief van een kamp naast de hoofdprijs (<see cref="Camp.Price"/>), bv. "Leden" of
/// "Tweede kind". Publiek enkel ter referentie getoond; de beheerder kiest per inschrijving
/// (<see cref="CampEnrollment.CampPriceId"/>) welk tarief geldt. Zonder keuze geldt de hoofdprijs.
/// </summary>
public class CampPrice : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CampId { get; set; }

    /// <summary>Naam die in admin en publieke weergave getoond wordt.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Korte publieke uitleg voor wie of wanneer dit tarief geldt.</summary>
    public string? Description { get; set; }

    /// <summary>Bedrag in EUR, per deelnemer.</summary>
    public decimal Amount { get; set; }

    public int SortOrder { get; set; }

    // Navigation
    public Organization Organization { get; set; } = null!;
    public Camp Camp { get; set; } = null!;
}
