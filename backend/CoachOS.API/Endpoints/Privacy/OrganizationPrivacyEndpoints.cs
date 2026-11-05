using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Privacy;

namespace CoachOS.API.Endpoints.Privacy;

public sealed class OrganizationPrivacyEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/privacy/organization/export", async (IOrganizationDataExport service, HttpContext context, CancellationToken ct) =>
        {
            byte[]? zip = await service.ExportAsync(context.GetOrganizationId(), context.GetUserId(), ct);
            if (zip is null) return Results.Forbid();
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Pragma = "no-cache";
            return Results.File(zip, "application/zip", "coachos-club-export.zip");
        }).RequireAuthorization(policy => policy.RequireRole("Admin")).WithTags("Privacy");

        app.MapPost("/super-admin/organizations/{organizationId:guid}/erasure", async (Guid organizationId, OrganizationErasureRequest request, IOrganizationErasureService service, HttpContext context, CancellationToken ct) =>
        {
            if (request.Confirmation?.Length > 250 || request.PreviewHash?.Length > 64) return Results.BadRequest();
            OrganizationErasureResult result = await service.ProcessAsync(organizationId, request, ct, context.GetUserId());
            if (result.Holds.Contains("forbidden")) return Results.Forbid();
            if (result.Holds.Contains("not_found")) return Results.NotFound();
            return Results.Ok(result);
        }).RequireAuthorization(AuthorizationPolicies.SuperAdmin).WithTags("Privacy");
    }
}
