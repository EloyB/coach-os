using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.SuperAdmin;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class DisableAdminEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/super-admin/admins/{userId:guid}/disable", async (
            Guid userId, ISuperAdminService service, CancellationToken ct) =>
        {
            var result = await service.DisableAdminAsync(userId, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .WithTags("SuperAdmin");
    }
}
