using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class CancelOrganizationSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/super-admin/organizations/{organizationId:guid}/subscription/cancel", async (
                Guid organizationId, ISubscriptionService service, CancellationToken ct) =>
            {
                Result<SubscriptionDto> result = await service.CancelAsync(organizationId, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
            .WithTags("SuperAdmin");
    }
}
