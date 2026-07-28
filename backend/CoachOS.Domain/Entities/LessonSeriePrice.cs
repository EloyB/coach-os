using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Eén prijsoptie (tarief) van een lessenreeks: een benoemd bedrag met optionele
/// beschrijving. De speler kiest bij het inschrijven één optie; het bedrag geldt
/// per deelnemer. Zonder opties valt de reeks terug op <see cref="LessonSerie.Price"/>.
/// </summary>
public class LessonSeriePrice : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid LessonSerieId { get; set; }

    /// <summary>Naam die in admin en publieke flow getoond wordt.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Korte publieke uitleg voor wie of wanneer dit tarief geldt.</summary>
    public string? Description { get; set; }

    /// <summary>Bedrag in EUR, per deelnemer.</summary>
    public decimal TotalPrice { get; set; }

    public int SortOrder { get; set; }

    /// <summary>
    /// Voorbereiding op herbruikbare organisatie-opties: dezelfde key kan later over
    /// meerdere reeksen heen als template gebruikt worden. Null = enkel voor deze reeks.
    /// </summary>
    public string? ReusableKey { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public LessonSerie LessonSerie { get; set; } = null!;
}
