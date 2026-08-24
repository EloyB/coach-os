using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Export;
using CoachOS.Application.LessonSerie;

namespace CoachOS.API.Endpoints.Planning;

public class ExportPlanningEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}/planning/export",
            async (Guid id, IPlanningExportService service, ILessonSerieService series, HttpContext ctx, CancellationToken ct) =>
            {
                var access = await HeadTrainerAccess.EnsureSerieAccessAsync(ctx, series, id, ct);
                if (!access.IsSuccess) return access.ToErrorResult();

                var result = await service.ExportSeriePlanningAsync(id, ctx.GetOrganizationId(), ct);
                return result.IsSuccess
                    ? Results.File(result.Value!.Content, result.Value.ContentType, result.Value.FileName)
                    : result.ToErrorResult();
            })
        .RequireAuthorization(AuthorizationPolicies.EnrollmentsPlanningRead)
        .WithTags("Planning");
    }
}
