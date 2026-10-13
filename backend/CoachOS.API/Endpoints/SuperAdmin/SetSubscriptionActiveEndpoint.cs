using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class SetSubscriptionActiveEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/super-admin/organizations/{organizationId:guid}/subscription/active", async (
            Guid organizationId,
            SetSubscriptionActiveRequest request,
            ISubscriptionService service,
            CancellationToken ct) =>
        {
            var result = await service.SetActiveAsync(organizationId, request.IsActive, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .AddEndpointFilter<ValidationFilter<SetSubscriptionActiveRequest>>()
        .WithTags("SuperAdmin");
    }
}
