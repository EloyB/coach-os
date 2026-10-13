using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Subscriptions;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class RefreshSubscriptionPaymentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/super-admin/organizations/{organizationId:guid}/subscription/refresh", async (
            Guid organizationId,
            ISubscriptionService service,
            CancellationToken ct) =>
        {
            var result = await service.RefreshPaymentStatusAsync(organizationId, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .WithTags("SuperAdmin");
    }
}
