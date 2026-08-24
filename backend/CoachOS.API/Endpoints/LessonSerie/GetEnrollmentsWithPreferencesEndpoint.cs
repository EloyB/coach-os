using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Enrollments;
using CoachOS.Application.LessonSerie;

namespace CoachOS.API.Endpoints.LessonSerie;

public class GetEnrollmentsWithPreferencesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}/enrollments/planning",
            async (Guid id, IEnrollmentService service, ILessonSerieService series, HttpContext ctx, CancellationToken ct) =>
            {
                var access = await HeadTrainerAccess.EnsureSerieAccessAsync(ctx, series, id, ct);
                if (!access.IsSuccess) return access.ToErrorResult();

                var result = await service.GetSeriesEnrollmentsWithPreferencesAsync(
                    id, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(AuthorizationPolicies.EnrollmentsPlanningRead)
        .WithTags("Planning");
    }
}
