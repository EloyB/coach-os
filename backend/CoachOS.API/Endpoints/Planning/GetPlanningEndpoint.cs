using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.Planning;

namespace CoachOS.API.Endpoints.Planning;

public class GetPlanningEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}/planning",
            async (Guid id, IPlanningService service, ILessonSerieService series, HttpContext ctx, CancellationToken ct) =>
            {
                var access = await HeadTrainerAccess.EnsureSerieAccessAsync(ctx, series, id, ct);
                if (!access.IsSuccess) return access.ToErrorResult();

                var result = await service.GetPlanningOverviewAsync(id, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(AuthorizationPolicies.EnrollmentsPlanningRead)
        .WithTags("Planning");
    }
}
