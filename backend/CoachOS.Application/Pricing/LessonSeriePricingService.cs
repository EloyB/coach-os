using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

using LessonSerieEntity = CoachOS.Domain.Entities.LessonSerie;

namespace CoachOS.Application.Pricing;

public class LessonSeriePricingService(
    ILessonSerieRepository lessonSeries,
    ILessonSeriePriceRepository prices) : ILessonSeriePricingService
{
    public async Task<Result<List<LessonSeriePriceDto>>> GetPricesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default)
    {
        LessonSerieEntity? series = await lessonSeries.GetByIdAsync(lessonSerieId, organizationId, ct);
        if (series is null)
        {
            return Result<List<LessonSeriePriceDto>>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));
        }

        IReadOnlyList<LessonSeriePrice> rows = await prices.GetBySeriesAsync(lessonSerieId, organizationId, ct);
        return Result<List<LessonSeriePriceDto>>.Ok(rows.Select(ToDto).ToList());
    }

    public async Task<Result<List<LessonSeriePriceDto>>> SavePricesAsync(
        Guid lessonSerieId, Guid organizationId, SaveLessonSeriePricesRequest request,
        CancellationToken ct = default)
    {
        LessonSerieEntity? series = await lessonSeries.GetByIdAsync(lessonSerieId, organizationId, ct);
        if (series is null)
        {
            return Result<List<LessonSeriePriceDto>>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));
        }

        List<LessonSeriePrice> rows = request.Prices
            .Select((p, index) => new LessonSeriePrice
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                LessonSerieId = lessonSerieId,
                Label = p.Label.Trim(),
                Description = string.IsNullOrWhiteSpace(p.Description) ? null : p.Description.Trim(),
                Mode = (PricingMode)p.Mode,
                Category = p.Category.HasValue ? (ParticipantCategory)p.Category.Value : null,
                GroupSize = p.GroupSize,
                TotalPrice = p.TotalPrice,
                SortOrder = p.SortOrder == 0 ? index : p.SortOrder,
                ReusableKey = string.IsNullOrWhiteSpace(p.ReusableKey) ? null : p.ReusableKey.Trim(),
            }).ToList();

        await prices.ReplaceForSeriesAsync(lessonSerieId, organizationId, rows, ct);
        await prices.SaveChangesAsync(ct);

        IReadOnlyList<LessonSeriePrice> saved = await prices.GetBySeriesAsync(lessonSerieId, organizationId, ct);
        return Result<List<LessonSeriePriceDto>>.Ok(saved.Select(ToDto).ToList());
    }

    public static LessonSeriePriceDto ToDto(LessonSeriePrice p) => new()
    {
        Id = p.Id,
        Label = string.IsNullOrWhiteSpace(p.Label) ? BuildFallbackLabel(p) : p.Label,
        Description = p.Description,
        Mode = (int)p.Mode,
        ModeLabel = p.Mode switch
        {
            PricingMode.FixedPerParticipant => "Vaste prijs per deelnemer",
            PricingMode.GroupSize => "Prijs per groepsgrootte",
            PricingMode.TariffCategory => "Prijs per tariefcategorie",
            PricingMode.ManualOption => "Manueel gekozen optie",
            _ => "Prijsoptie",
        },
        Category = p.Category.HasValue ? (int)p.Category.Value : null,
        CategoryLabel = p.Category switch
        {
            ParticipantCategory.Youth => "Jeugd",
            ParticipantCategory.Adult => "Volwassenen",
            _ => null,
        },
        GroupSize = p.GroupSize,
        TotalPrice = p.TotalPrice,
        SortOrder = p.SortOrder,
        ReusableKey = p.ReusableKey,
    };

    private static string BuildFallbackLabel(LessonSeriePrice p)
        => p.Mode switch
        {
            PricingMode.GroupSize when p.GroupSize == 1 => "Privéles",
            PricingMode.GroupSize when p.GroupSize.HasValue => $"Groep van {p.GroupSize} personen",
            PricingMode.TariffCategory when p.Category == ParticipantCategory.Youth => "Jeugd",
            PricingMode.TariffCategory when p.Category == ParticipantCategory.Adult => "Volwassenen",
            PricingMode.FixedPerParticipant => "Standaardprijs",
            _ => "Prijsoptie",
        };
}
