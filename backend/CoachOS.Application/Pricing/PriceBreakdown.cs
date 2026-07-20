using CoachOS.Domain.Enums;

namespace CoachOS.Application.Pricing;

/// <summary>
/// Uitkomst van een prijsberekening: het totaalbedrag plus een specificatie per
/// categorie, zodat facturen en bevestigingsmails kunnen tonen hoe het bedrag
/// is opgebouwd.
/// </summary>
public record PriceBreakdown
{
    public decimal Total { get; init; }
    public int GroupSize { get; init; }
    public IReadOnlyList<PriceLine> Lines { get; init; } = [];

    /// <summary>
    /// True wanneer er geen prijsmatrix bestond en teruggevallen is op het legacy
    /// veld <c>LessonSerie.Price</c> (per persoon × groepsgrootte).
    /// </summary>
    public bool UsedLegacyPrice { get; init; }
}

/// <summary>Eén regel in de specificatie: N deelnemers van een categorie.</summary>
public record PriceLine
{
    public ParticipantCategory Category { get; init; }
    public int Count { get; init; }
    public decimal Amount { get; init; }
}
