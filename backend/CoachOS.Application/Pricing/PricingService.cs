using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

using LessonSerieEntity = CoachOS.Domain.Entities.LessonSerie;

namespace CoachOS.Application.Pricing;

/// <summary>
/// Centrale prijsberekening voor de flexibele prijsopties van een lessenreeks.
/// Volgorde:
/// 1. manueel gekozen prijsopties op de inschrijvingen;
/// 2. prijs per groepsgrootte;
/// 3. prijs per tariefcategorie;
/// 4. vaste prijs per deelnemer;
/// 5. legacy LessonSerie.Price fallback.
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

        Result<PriceBreakdown>? manual = CalculateManualOptions(options, participants, groupSize);
        if (manual is not null) return manual;

        Result<PriceBreakdown>? group = CalculateGroupSize(options, participants, groupSize, series);
        if (group is not null) return group;

        Result<PriceBreakdown>? tariff = CalculateTariffCategory(options, participants, groupSize);
        if (tariff is not null) return tariff;

        LessonSeriePrice? fixedPrice = options
            .Where(p => p.Mode == PricingMode.FixedPerParticipant)
            .OrderBy(p => p.SortOrder)
            .FirstOrDefault();
        if (fixedPrice is not null)
        {
            decimal total = Round(fixedPrice.TotalPrice * groupSize);
            return Result<PriceBreakdown>.Ok(new PriceBreakdown
            {
                Total = total,
                GroupSize = groupSize,
                UsedLegacyPrice = false,
                Lines = [Line(fixedPrice, groupSize, total)],
            });
        }

        return Legacy(series, groupSize);
    }

    private static Result<PriceBreakdown>? CalculateManualOptions(
        IReadOnlyList<LessonSeriePrice> options, IReadOnlyList<Enrollment> participants, int groupSize)
    {
        if (participants.All(p => p.SelectedPriceOptionId is null)) return null;

        Dictionary<Guid, LessonSeriePrice> manualOptions = options
            .Where(p => p.Mode == PricingMode.ManualOption)
            .ToDictionary(p => p.Id);

        List<PriceLine> lines = [];
        decimal total = 0m;
        foreach (IGrouping<Guid?, Enrollment> selectedGroup in participants.GroupBy(p => p.SelectedPriceOptionId))
        {
            if (selectedGroup.Key is null || !manualOptions.TryGetValue(selectedGroup.Key.Value, out LessonSeriePrice? option))
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

    private static Result<PriceBreakdown>? CalculateGroupSize(
        IReadOnlyList<LessonSeriePrice> options,
        IReadOnlyList<Enrollment> participants,
        int groupSize,
        LessonSerieEntity series)
    {
        List<LessonSeriePrice> groupOptions = options
            .Where(p => p.Mode == PricingMode.GroupSize && p.GroupSize.HasValue)
            .ToList();
        if (groupOptions.Count == 0) return null;

        // Legacy matrix compatibility: oude rijen hadden category + groupSize. Dan blijft
        // de bestaande pro-rata logica behouden.
        if (groupOptions.Any(p => p.Category.HasValue))
        {
            List<PriceLine> lines = [];
            decimal total = 0m;
            bool usedLegacy = false;
            foreach (IGrouping<ParticipantCategory, Enrollment> categoryGroup in participants
                         .GroupBy(p => p.Category ?? ParticipantCategory.Adult)
                         .OrderBy(g => g.Key))
            {
                LessonSeriePrice? row = SelectGroupRow(groupOptions, groupSize, categoryGroup.Key);
                decimal amount;
                if (row is null)
                {
                    amount = Round(series.Price * categoryGroup.Count());
                    usedLegacy = true;
                    lines.Add(new PriceLine
                    {
                        Label = "Standaardprijs",
                        Category = categoryGroup.Key,
                        Count = categoryGroup.Count(),
                        Amount = amount,
                    });
                }
                else
                {
                    amount = Round(row.TotalPrice / row.GroupSize!.Value * categoryGroup.Count());
                    lines.Add(Line(row, categoryGroup.Count(), amount));
                }

                total += amount;
            }

            if (lines.Count > 0)
            {
                return Result<PriceBreakdown>.Ok(new PriceBreakdown
                {
                    Total = total,
                    GroupSize = groupSize,
                    UsedLegacyPrice = usedLegacy,
                    Lines = lines,
                });
            }
        }

        LessonSeriePrice? option = SelectGroupRow(groupOptions, groupSize, null);
        if (option is null) return null;

        decimal totalPrice = Round(option.TotalPrice);
        return Result<PriceBreakdown>.Ok(new PriceBreakdown
        {
            Total = totalPrice,
            GroupSize = groupSize,
            UsedLegacyPrice = false,
            Lines = [Line(option, groupSize, totalPrice)],
        });
    }

    private static Result<PriceBreakdown>? CalculateTariffCategory(
        IReadOnlyList<LessonSeriePrice> options, IReadOnlyList<Enrollment> participants, int groupSize)
    {
        List<LessonSeriePrice> tariffOptions = options
            .Where(p => p.Mode == PricingMode.TariffCategory && p.Category.HasValue)
            .ToList();
        if (tariffOptions.Count == 0) return null;

        List<PriceLine> lines = [];
        decimal total = 0m;
        foreach (IGrouping<ParticipantCategory, Enrollment> categoryGroup in participants
                     .GroupBy(p => p.Category ?? ParticipantCategory.Adult)
                     .OrderBy(g => g.Key))
        {
            LessonSeriePrice? option = tariffOptions.FirstOrDefault(p => p.Category == categoryGroup.Key);
            if (option is null) continue;

            decimal amount = Round(option.TotalPrice * categoryGroup.Count());
            lines.Add(Line(option, categoryGroup.Count(), amount));
            total += amount;
        }

        return lines.Count == 0
            ? null
            : Result<PriceBreakdown>.Ok(new PriceBreakdown
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

    private static LessonSeriePrice? SelectGroupRow(
        IReadOnlyList<LessonSeriePrice> options, int groupSize, ParticipantCategory? category)
    {
        IEnumerable<LessonSeriePrice> candidates = options;
        if (category.HasValue)
            candidates = candidates.Where(p => p.Category == category.Value);
        else
            candidates = candidates.Where(p => p.Category is null);

        return candidates
            .OrderBy(p => Math.Abs(p.GroupSize!.Value - groupSize))
            .ThenByDescending(p => p.GroupSize)
            .FirstOrDefault();
    }

    private static PriceLine Line(LessonSeriePrice option, int count, decimal amount) => new()
    {
        Label = string.IsNullOrWhiteSpace(option.Label) ? "Prijsoptie" : option.Label,
        Description = option.Description,
        Category = option.Category,
        Count = count,
        Amount = amount,
    };

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
