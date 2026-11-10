using CoachOS.API.Extensions;
using CoachOS.Application.Export;
using CoachOS.Application.Privacy;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Privacy;

/// <summary>ZIP-export van alle gegevens van de eigen club, voor clubbeheerders.</summary>
public class ExportClubDataEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/privacy/organization/export", async (IClubExportService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<ExportFileDto> result = await service.ExportAsync(ctx.GetOrganizationId(), ctx.GetUserId(), ct);
            if (!result.IsSuccess)
                return result.ToErrorResult();

            ctx.Response.Headers.CacheControl = "no-store";
            return Results.File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Privacy");
    }
}
