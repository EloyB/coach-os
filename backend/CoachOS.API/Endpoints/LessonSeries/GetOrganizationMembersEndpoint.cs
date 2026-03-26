using CoachOS.API.Extensions;
using CoachOS.Application.LessonSeries;

namespace CoachOS.API.Endpoints.LessonSeries;

public class GetOrganizationMembersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/members", async (ILessonSeriesService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.GetMembersAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSeries");
    }
}
