namespace CoachOS.Application.LessonSerie.DTOs;

public record LessonSeriePriceDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Mode { get; init; }
    public string ModeLabel { get; init; } = string.Empty;
    public int? Category { get; init; }
    public string? CategoryLabel { get; init; }
    public int? GroupSize { get; init; }
    public decimal TotalPrice { get; init; }
    public int SortOrder { get; init; }
    public string? ReusableKey { get; init; }
}

public record LessonSeriePriceRequest
{
    public string Label { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Mode { get; init; } = 1;
    public int? Category { get; init; }
    public int? GroupSize { get; init; }
    public decimal TotalPrice { get; init; }
    public int SortOrder { get; init; }
    public string? ReusableKey { get; init; }
}

/// <summary>
/// Vervangt alle prijsopties van een reeks. Een lege lijst wist de opties, waarna
/// de reeks terugvalt op het legacy prijsveld.
/// </summary>
public record SaveLessonSeriePricesRequest
{
    public List<LessonSeriePriceRequest> Prices { get; init; } = new();
}
