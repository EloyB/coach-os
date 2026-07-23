namespace CoachOS.Application.LessonSerie.DTOs;

/// <summary>
/// Eén cel uit de prijsmatrix. <see cref="TotalPrice"/> is het TOTAAL voor de
/// hele groep van <see cref="GroupSize"/> deelnemers, niet per persoon.
/// </summary>
public record LessonSeriePriceDto
{
    public Guid Id { get; init; }
    public int Category { get; init; }
    public string CategoryLabel { get; init; } = string.Empty;
    public int GroupSize { get; init; }
    public decimal TotalPrice { get; init; }
}

/// <summary>Eén rij bij het opslaan van de matrix.</summary>
public record LessonSeriePriceRequest
{
    public int Category { get; init; }
    public int GroupSize { get; init; }
    public decimal TotalPrice { get; init; }
}

/// <summary>
/// Vervangt de volledige prijsmatrix van een reeks. Een lege lijst wist de matrix,
/// waarna de reeks terugvalt op het legacy prijsveld.
/// </summary>
public record SaveLessonSeriePricesRequest
{
    public List<LessonSeriePriceRequest> Prices { get; init; } = new();
}
