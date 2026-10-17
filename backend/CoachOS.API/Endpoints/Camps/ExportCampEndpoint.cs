using CoachOS.API.Extensions;
using CoachOS.Application.Export;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

public class ExportCampEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/camps/{id:guid}/export", async (Guid id, ICampExportService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<ExportFileDto> result = await service.ExportCampAsync(id, ctx.GetOrganizationId(), ct);
            return result.IsSuccess
                ? Results.File(result.Value!.Content, result.Value.ContentType, result.Value.FileName)
                : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Camps");
    }
}
