using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class MarkSubscriptionPaymentPaidEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/super-admin/organizations/{organizationId:guid}/subscription/payments/{paymentId:guid}/mark-paid", async (
                Guid organizationId, Guid paymentId, ISubscriptionService service, CancellationToken ct) =>
            {
                Result<SubscriptionDto> result = await service.MarkPaymentPaidAsync(organizationId, paymentId, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
            .WithTags("SuperAdmin");
    }
}
