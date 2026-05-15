using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Configuration;
using CoachOS.Application.SuperAdmin;
using Microsoft.Extensions.Options;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class ResendAdminInviteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/super-admin/admins/{userId:guid}/resend-invite", async (
            Guid userId,
            ISuperAdminService service,
            IOptions<AppOptions> appOptions,
            CancellationToken ct) =>
        {
            var result = await service.ResendAdminInviteAsync(userId, appOptions.Value.FrontendBaseUrl, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .WithTags("SuperAdmin");
    }
}
