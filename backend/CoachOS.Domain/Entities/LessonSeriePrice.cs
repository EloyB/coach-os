using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Eén cel uit de prijsmatrix van een lessenreeks: het TOTAALBEDRAG voor een groep
/// van <see cref="GroupSize"/> deelnemers in categorie <see cref="Category"/>.
///
/// Let op — dit is een totaalprijs, geen prijs per persoon. Een rij
/// (Adult, 4, 480) betekent: een groep van 4 volwassenen betaalt samen €480.
/// Dit wijkt af van het legacy veld <see cref="LessonSerie.Price"/>, dat per
/// persoon geldt en als fallback blijft bestaan voor reeksen zonder matrix.
///
/// Bij een gemengde groep (bv. 2 volwassenen + 2 jeugd) bestaat er geen enkele
/// juiste rij. Dan wordt per categorie het pro-rata aandeel genomen
/// (totaal / groepsgrootte) en gesommeerd — zie <c>PricingService</c>.
/// </summary>
public class LessonSeriePrice : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid LessonSerieId { get; set; }

    public ParticipantCategory Category { get; set; }

    /// <summary>Aantal deelnemers waarvoor dit totaalbedrag geldt (1 t/m 8).</summary>
    public int GroupSize { get; set; }

    /// <summary>Totaalbedrag in EUR voor de volledige groep van deze grootte.</summary>
    public decimal TotalPrice { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public LessonSerie LessonSerie { get; set; } = null!;
}
