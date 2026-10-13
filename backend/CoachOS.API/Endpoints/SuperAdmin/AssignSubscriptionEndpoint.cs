using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class AssignSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/super-admin/organizations/{organizationId:guid}/subscription", async (
            Guid organizationId,
            AssignSubscriptionRequest request,
            ISubscriptionService service,
            CancellationToken ct) =>
        {
            var result = await service.AssignPlanAsync(organizationId, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .AddEndpointFilter<ValidationFilter<AssignSubscriptionRequest>>()
        .WithTags("SuperAdmin");
    }
}
