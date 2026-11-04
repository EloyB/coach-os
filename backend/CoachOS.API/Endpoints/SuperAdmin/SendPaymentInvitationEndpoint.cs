using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class SendPaymentInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/super-admin/organizations/{organizationId:guid}/subscription/invite", async (
                Guid organizationId, ISubscriptionService service, CancellationToken ct) =>
            {
                Result result = await service.SendPaymentInvitationAsync(organizationId, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
            .WithTags("SuperAdmin");
    }
}
