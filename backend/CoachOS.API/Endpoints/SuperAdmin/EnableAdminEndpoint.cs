using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.SuperAdmin;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class EnableAdminEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/super-admin/admins/{userId:guid}/enable", async (
            Guid userId, ISuperAdminService service, CancellationToken ct) =>
        {
            var result = await service.EnableAdminAsync(userId, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .WithTags("SuperAdmin");
    }
}
