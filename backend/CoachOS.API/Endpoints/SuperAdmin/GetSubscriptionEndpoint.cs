using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Subscriptions;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class GetSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/super-admin/organizations/{organizationId:guid}/subscription", async (
            Guid organizationId,
            ISubscriptionService service,
            CancellationToken ct) =>
        {
            var result = await service.GetForOrganizationAsync(organizationId, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .WithTags("SuperAdmin");
    }
}
