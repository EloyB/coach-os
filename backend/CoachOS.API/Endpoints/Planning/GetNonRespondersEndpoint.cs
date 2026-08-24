using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.Planning;

namespace CoachOS.API.Endpoints.Planning;

public class GetNonRespondersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}/planning/non-responders",
            async (Guid id, IConfirmationOrchestrationService service, ILessonSerieService series, HttpContext ctx, CancellationToken ct) =>
            {
                var access = await HeadTrainerAccess.EnsureSerieAccessAsync(ctx, series, id, ct);
                if (!access.IsSuccess) return access.ToErrorResult();

                var result = await service.GetNonRespondersAsync(id, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(AuthorizationPolicies.EnrollmentsPlanningRead)
        .WithTags("Planning");
    }
}
