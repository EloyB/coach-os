using CoachOS.API.Extensions;
using CoachOS.Application.LessonSeries;

namespace CoachOS.API.Endpoints.LessonSeries;

public class GetLessonSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries", async (ILessonSeriesService service, HttpContext ctx, CancellationToken ct) =>
        {
            Guid orgId = ctx.GetOrganizationId();
            Guid? trainerId = ctx.IsTrainer() ? ctx.GetUserId() : null;
            var result = await service.GetAllAsync(orgId, trainerId, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSeries");
    }
}
