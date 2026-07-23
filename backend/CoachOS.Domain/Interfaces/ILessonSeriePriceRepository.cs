using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ILessonSeriePriceRepository
{
    /// <summary>Alle prijscellen van een reeks, org-gefilterd.</summary>
    Task<IReadOnlyList<LessonSeriePrice>> GetBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Prijscellen zonder org-filter, voor gebruik in publieke/token-flows waar geen
    /// ingelogde organisatie beschikbaar is (bv. de student-bevestigingspagina).
    /// </summary>
    Task<IReadOnlyList<LessonSeriePrice>> GetBySeriesPublicAsync(
        Guid lessonSerieId, CancellationToken ct = default);

    /// <summary>Vervangt de volledige matrix van een reeks in één transactie.</summary>
    Task ReplaceForSeriesAsync(
        Guid lessonSerieId, Guid organizationId, IEnumerable<LessonSeriePrice> prices,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
