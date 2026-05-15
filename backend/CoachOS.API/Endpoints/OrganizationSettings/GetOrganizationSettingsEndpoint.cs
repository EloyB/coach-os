using CoachOS.API.Extensions;
using CoachOS.Application.OrganizationSettings;
using CoachOS.Application.OrganizationSettings.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.OrganizationSettings;

public class GetOrganizationSettingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/organization/settings", async (
            IOrganizationSettingsService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result<OrganizationSettingsDto> result = await service.GetAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("OrganizationSettings");
    }
}
