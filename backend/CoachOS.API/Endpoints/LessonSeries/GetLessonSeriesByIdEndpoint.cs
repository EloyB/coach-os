using CoachOS.API.Extensions;
using CoachOS.Application.LessonSeries;

namespace CoachOS.API.Endpoints.LessonSeries;

public class GetLessonSeriesByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}", async (Guid id, ILessonSeriesService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Errors.Select(e => e.Message));
        })
        .RequireAuthorization()
        .WithTags("LessonSeries");
    }
}
