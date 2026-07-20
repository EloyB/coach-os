using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

using LessonSerieEntity = CoachOS.Domain.Entities.LessonSerie;

namespace CoachOS.Application.Pricing;

/// <summary>
/// Centrale prijsberekening. Vóór deze service stond de formule
/// <c>Price * groepsgrootte</c> vijf keer gedupliceerd verspreid over vier services,
/// met één afwijkende (foute) variant in PaymentService. Alle callsites routeren
/// nu hierheen.
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

        IReadOnlyList<LessonSeriePrice> matrix = await prices.GetBySeriesPublicAsync(lessonSerieId, ct);
        int groupSize = participants.Count;

        // Geen matrix ingesteld → legacy gedrag: LessonSerie.Price geldt per persoon.
        if (matrix.Count == 0)
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
                        Category = ParticipantCategory.Adult,
                        Count = groupSize,
                        Amount = legacyTotal,
                    }
                ],
            });
        }

        // Matrix geeft een TOTAAL per groep. Bij een gemengde groep bestaat er geen
        // enkele juiste rij, dus nemen we per categorie het pro-rata aandeel:
        // aandeel per persoon = rij.TotalPrice / rij.GroupSize.
        List<PriceLine> lines = [];
        decimal total = 0m;

        IEnumerable<IGrouping<ParticipantCategory, Enrollment>> byCategory = participants
            .GroupBy(p => p.Category ?? ParticipantCategory.Adult)
            .OrderBy(g => g.Key);

        foreach (IGrouping<ParticipantCategory, Enrollment> group in byCategory)
        {
            LessonSeriePrice? row = SelectRow(matrix, group.Key, groupSize);

            decimal amount = row is null
                // Categorie ontbreekt volledig in de matrix → legacy prijs per persoon.
                ? Round(series.Price * group.Count())
                : Round(row.TotalPrice / row.GroupSize * group.Count());

            lines.Add(new PriceLine
            {
                Category = group.Key,
                Count = group.Count(),
                Amount = amount,
            });
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

    /// <summary>
    /// Kiest de tariefrij voor een categorie. Exacte match op groepsgrootte wint.
    /// Bestaat die niet, dan de dichtstbijzijnde gedefinieerde grootte — bij gelijke
    /// afstand de grotere groep, omdat dat het goedkopere tarief per persoon oplevert
    /// en we de klant niet willen overvragen op een grootte die de club niet instelde.
    /// </summary>
    private static LessonSeriePrice? SelectRow(
        IReadOnlyList<LessonSeriePrice> matrix, ParticipantCategory category, int groupSize)
    {
        List<LessonSeriePrice> forCategory = matrix.Where(p => p.Category == category).ToList();
        if (forCategory.Count == 0) return null;

        return forCategory
            .OrderBy(p => Math.Abs(p.GroupSize - groupSize))
            .ThenByDescending(p => p.GroupSize)
            .First();
    }

    /// <summary>
    /// Mollie weigert bedragen met meer dan 2 decimalen. Per regel afronden i.p.v.
    /// op het eindtotaal, zodat de getoonde specificatie exact optelt tot het
    /// aangerekende bedrag.
    /// </summary>
    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
