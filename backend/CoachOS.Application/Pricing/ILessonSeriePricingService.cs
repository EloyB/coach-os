using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Pricing;

/// <summary>
/// Beheer van de prijsmatrix van een lessenreeks (categorie × groepsgrootte).
/// Staat los van <see cref="IPricingService"/>, dat de matrix alleen leest om een
/// bedrag te berekenen.
/// </summary>
public interface ILessonSeriePricingService
{
    Task<Result<List<LessonSeriePriceDto>>> GetPricesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    /// <summary>Vervangt de volledige matrix. Lege lijst wist hem.</summary>
    Task<Result<List<LessonSeriePriceDto>>> SavePricesAsync(
        Guid lessonSerieId, Guid organizationId, SaveLessonSeriePricesRequest request,
        CancellationToken ct = default);
}
