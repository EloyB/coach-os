using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.OrganizationSettings;
using CoachOS.Application.OrganizationSettings.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.OrganizationSettings;

public class UpdateOrganizationSettingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/organization/settings", async (
            UpdateOrganizationSettingsRequest request,
            IOrganizationSettingsService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result<OrganizationSettingsDto> result = await service.UpdateAsync(
                ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<UpdateOrganizationSettingsRequest>>()
        .WithTags("OrganizationSettings");
    }
}
