using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Configureerbare prijsoptie voor een lessenreeks.
///
/// Legacy: oudere rijen uit de vroegere matrix hadden alleen <see cref="Category"/>,
/// <see cref="GroupSize"/> en <see cref="TotalPrice"/>. Zonder expliciete mode worden
/// die rijen gemigreerd naar <see cref="PricingMode.GroupSize"/> zodat bestaande
/// reeksen blijven werken terwijl de admin voortaan met benoemde opties werkt.
/// </summary>
public class LessonSeriePrice : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid LessonSerieId { get; set; }

    /// <summary>Naam die in admin en publieke flow getoond wordt.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Korte publieke uitleg waarom deze prijs bestaat/toegepast wordt.</summary>
    public string? Description { get; set; }

    public PricingMode Mode { get; set; } = PricingMode.GroupSize;

    /// <summary>
    /// Tariefcategorie voor mode TariffCategory. Null voor opties die niet per
    /// leeftijds-/tariefcategorie werken.
    /// </summary>
    public ParticipantCategory? Category { get; set; }

    /// <summary>
    /// Groepsgrootte voor mode GroupSize. Null voor andere modes.
    /// </summary>
    public int? GroupSize { get; set; }

    /// <summary>
    /// Bedrag in EUR. Betekenis hangt af van <see cref="Mode"/>:
    /// per deelnemer, per tariefcategorie, handmatige keuze, of totaal per groep.
    /// </summary>
    public decimal TotalPrice { get; set; }

    public int SortOrder { get; set; }

    /// <summary>
    /// Voorbereiding op herbruikbare organisatie-opties: dezelfde key kan later over
    /// meerdere reeksen heen als template gebruikt worden zonder bestaande rijen te
    /// breken. Null = enkel voor deze reeks.
    /// </summary>
    public string? ReusableKey { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public LessonSerie LessonSerie { get; set; } = null!;
}
