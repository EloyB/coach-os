using CoachOS.Domain.Entities;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Pricing;

public interface IPricingService
{
    /// <summary>
    /// Berekent het totaalbedrag voor een groep deelnemers van een lessenreeks.
    ///
    /// Volgorde: bestaat er een prijsmatrix voor de reeks, dan bepaalt die het
    /// totaal (per categorie pro rata bij een gemengde groep). Ontbreekt de matrix,
    /// dan valt de berekening terug op het legacy veld <c>LessonSerie.Price</c>,
    /// dat per persoon geldt.
    /// </summary>
    /// <param name="participants">
    /// Alle deelnemers van de groep, inclusief de groepsleider.
    /// </param>
    Task<Result<PriceBreakdown>> CalculateForGroupAsync(
        Guid lessonSerieId, IReadOnlyList<Enrollment> participants, CancellationToken ct = default);
}
