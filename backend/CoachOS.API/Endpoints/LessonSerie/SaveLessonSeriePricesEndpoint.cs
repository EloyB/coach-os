using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.Pricing;

namespace CoachOS.API.Endpoints.LessonSerie;

public class SaveLessonSeriePricesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/lessonseries/{id:guid}/prices",
            async (Guid id, SaveLessonSeriePricesRequest request, ILessonSeriePricingService service,
                   HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.SavePricesAsync(id, ctx.GetOrganizationId(), request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<SaveLessonSeriePricesRequest>>()
        .WithTags("LessonSeries");
    }
}
