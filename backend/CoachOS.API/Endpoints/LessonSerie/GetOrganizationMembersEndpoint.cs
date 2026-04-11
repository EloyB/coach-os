using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;

namespace CoachOS.API.Endpoints.LessonSerie;

public class GetOrganizationMembersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/members", async (ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.GetMembersAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSerie");
    }
}
