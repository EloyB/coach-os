using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;

namespace CoachOS.API.Endpoints.LessonSerie;

public class GetLessonSerieEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries", async (ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            var orgId = ctx.GetOrganizationId();
            Guid? trainerId = ctx.IsTrainer() ? ctx.GetUserId() : null;
            var result = await service.GetAllAsync(orgId, trainerId, ctx.GetHeadTrainerClubIds(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSerie");
    }
}
