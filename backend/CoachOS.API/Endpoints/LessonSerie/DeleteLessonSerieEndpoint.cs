using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;

namespace CoachOS.API.Endpoints.LessonSerie;

public class DeleteLessonSerieEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{id:guid}", async (Guid id, ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSerie");
    }
}
