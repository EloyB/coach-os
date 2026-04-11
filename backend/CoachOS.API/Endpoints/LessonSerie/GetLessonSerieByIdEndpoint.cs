using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;

namespace CoachOS.API.Endpoints.LessonSerie;

public class GetLessonSerieByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}", async (Guid id, ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Errors.Select(e => e.Message));
        })
        .RequireAuthorization()
        .WithTags("LessonSerie");
    }
}
