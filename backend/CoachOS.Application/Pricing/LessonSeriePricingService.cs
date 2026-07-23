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

        List<LessonSeriePrice> rows = request.Prices.Select(p => new LessonSeriePrice
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            LessonSerieId = lessonSerieId,
            Category = (ParticipantCategory)p.Category,
            GroupSize = p.GroupSize,
            TotalPrice = p.TotalPrice,
        }).ToList();

        await prices.ReplaceForSeriesAsync(lessonSerieId, organizationId, rows, ct);
        await prices.SaveChangesAsync(ct);

        IReadOnlyList<LessonSeriePrice> saved = await prices.GetBySeriesAsync(lessonSerieId, organizationId, ct);
        return Result<List<LessonSeriePriceDto>>.Ok(saved.Select(ToDto).ToList());
    }

    private static LessonSeriePriceDto ToDto(LessonSeriePrice p) => new()
    {
        Id = p.Id,
        Category = (int)p.Category,
        CategoryLabel = p.Category == ParticipantCategory.Youth ? "Jeugd" : "Volwassenen",
        GroupSize = p.GroupSize,
        TotalPrice = p.TotalPrice,
    };
}
