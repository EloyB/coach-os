using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.SuperAdmin;
using CoachOS.Application.SuperAdmin.DTOs;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class SetOrganizationEarlyBirdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/super-admin/organizations/{organizationId:guid}/early-bird", async (
            Guid organizationId,
            ToggleEarlyBirdRequest request,
            ISuperAdminService service,
            CancellationToken ct) =>
        {
            var result = await service.SetOrganizationEarlyBirdAsync(organizationId, request.IsEarlyBird, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .AddEndpointFilter<ValidationFilter<ToggleEarlyBirdRequest>>()
        .WithTags("SuperAdmin");
    }
}
