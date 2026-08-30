using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

using LessonSerieEntity = CoachOS.Domain.Entities.LessonSerie;

namespace CoachOS.Application.Pricing;

/// <summary>
/// Prijsberekening voor een lessenreeks. Simpel model: elke reeks heeft een lijst
/// benoemde prijsopties waaruit de speler er één kiest (per deelnemer). Bestaan er
/// geen opties, of koos niemand er een, dan valt de berekening terug op het legacy
/// veld <see cref="LessonSerie.Price"/> (per persoon × groepsgrootte).
/// </summary>
public class PricingService(
    ILessonSerieRepository lessonSeries,
    ILessonSeriePriceRepository prices) : IPricingService
{
    public async Task<Result<PriceBreakdown>> CalculateForGroupAsync(
        Guid lessonSerieId, IReadOnlyList<Enrollment> participants, CancellationToken ct = default)
    {
        if (participants.Count == 0)
        {
            return Result<PriceBreakdown>.Fail(new Error(
                ErrorCodes.Validation, "Een prijsberekening vereist minstens één deelnemer."));
        }

        LessonSerieEntity? series = await lessonSeries.GetByIdPublicAsync(lessonSerieId, ct);
        if (series is null)
        {
            return Result<PriceBreakdown>.Fail(new Error(
                ErrorCodes.NotFound, "Lessenreeks niet gevonden."));
        }

        IReadOnlyList<LessonSeriePrice> options = await prices.GetBySeriesPublicAsync(lessonSerieId, ct);
        int groupSize = participants.Count;

        if (options.Count == 0)
            return Legacy(series, groupSize);

        Result<PriceBreakdown>? chosen = CalculateChosenOptions(options, participants, groupSize);
        return chosen ?? Legacy(series, groupSize);
    }

    /// <summary>
    /// Rekent op basis van de door de deelnemers gekozen prijsopties: bedrag per optie
    /// × aantal deelnemers dat die koos. Geeft null terug wanneer niemand een optie koos
    /// (dan volgt de legacy-fallback).
    /// </summary>
    private static Result<PriceBreakdown>? CalculateChosenOptions(
        IReadOnlyList<LessonSeriePrice> options, IReadOnlyList<Enrollment> participants, int groupSize)
    {
        if (participants.All(p => p.SelectedPriceOptionId is null)) return null;

        Dictionary<Guid, LessonSeriePrice> optionsById = options.ToDictionary(p => p.Id);

        List<PriceLine> lines = [];
        decimal total = 0m;
        foreach (IGrouping<Guid?, Enrollment> selectedGroup in participants.GroupBy(p => p.SelectedPriceOptionId))
        {
            if (selectedGroup.Key is null || !optionsById.TryGetValue(selectedGroup.Key.Value, out LessonSeriePrice? option))
            {
                return Result<PriceBreakdown>.Fail(new Error(
                    ErrorCodes.Validation, "Geselecteerde prijsoptie is niet geldig voor deze lessenreeks."));
            }

            decimal amount = Round(option.TotalPrice * selectedGroup.Count());
            lines.Add(Line(option, selectedGroup.Count(), amount));
            total += amount;
        }

        return Result<PriceBreakdown>.Ok(new PriceBreakdown
        {
            Total = total,
            GroupSize = groupSize,
            UsedLegacyPrice = false,
            Lines = lines,
        });
    }

    private static Result<PriceBreakdown> Legacy(LessonSerieEntity series, int groupSize)
    {
        decimal legacyTotal = Round(series.Price * groupSize);
        return Result<PriceBreakdown>.Ok(new PriceBreakdown
        {
            Total = legacyTotal,
            GroupSize = groupSize,
            UsedLegacyPrice = true,
            Lines =
            [
                new PriceLine
                {
                    Label = "Standaardprijs",
                    Category = ParticipantCategory.Adult,
                    Count = groupSize,
                    Amount = legacyTotal,
                }
            ],
        });
    }

    private static PriceLine Line(LessonSeriePrice option, int count, decimal amount) => new()
    {
        Label = string.IsNullOrWhiteSpace(option.Label) ? "Prijsoptie" : option.Label,
        Description = option.Description,
        Category = null,
        Count = count,
        Amount = amount,
    };

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
