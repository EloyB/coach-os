using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.SuperAdmin;

/// <summary>Offboarding-preview: aantallen, einddatum en blokkeringen. Wijzigt niets.</summary>
public class GetOrganizationOffboardingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/super-admin/organizations/{organizationId:guid}/offboarding", async (
                Guid organizationId, IOrganizationOffboardingService service, CancellationToken ct) =>
            {
                Result<OrganizationOffboardingPreviewDto> result = await service.GetPreviewAsync(organizationId, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
            .WithTags("SuperAdmin");
    }
}
