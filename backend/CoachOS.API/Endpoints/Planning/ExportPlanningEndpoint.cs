using CoachOS.API.Extensions;
using CoachOS.Application.Export;

namespace CoachOS.API.Endpoints.Planning;

public class ExportPlanningEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}/planning/export",
            async (Guid id, IPlanningExportService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.ExportSeriePlanningAsync(id, ctx.GetOrganizationId(), ct);
                return result.IsSuccess
                    ? Results.File(result.Value!.Content, result.Value.ContentType, result.Value.FileName)
                    : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Planning");
    }
}
