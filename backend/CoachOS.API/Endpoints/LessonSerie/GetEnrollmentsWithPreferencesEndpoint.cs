using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Enrollments;

namespace CoachOS.API.Endpoints.LessonSerie;

public class GetEnrollmentsWithPreferencesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}/enrollments/planning",
            async (Guid id, IEnrollmentService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.GetSeriesEnrollmentsWithPreferencesAsync(
                    id, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(AuthorizationPolicies.EnrollmentsPlanningRead)
        .WithTags("Planning");
    }
}
