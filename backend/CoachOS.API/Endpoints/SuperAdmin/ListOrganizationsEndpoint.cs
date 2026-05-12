using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.SuperAdmin;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class ListOrganizationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/super-admin/organizations", async (ISuperAdminService service, CancellationToken ct) =>
        {
            var result = await service.ListOrganizationsAsync(ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .WithTags("SuperAdmin");
    }
}
