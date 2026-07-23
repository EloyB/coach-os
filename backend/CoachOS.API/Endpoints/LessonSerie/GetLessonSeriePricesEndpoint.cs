using CoachOS.API.Extensions;
using CoachOS.Application.Pricing;

namespace CoachOS.API.Endpoints.LessonSerie;

public class GetLessonSeriePricesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}/prices",
            async (Guid id, ILessonSeriePricingService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.GetPricesAsync(id, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("LessonSeries");
    }
}
