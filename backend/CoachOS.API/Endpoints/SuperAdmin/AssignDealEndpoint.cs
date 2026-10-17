using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class AssignDealEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/super-admin/organizations/{organizationId:guid}/subscription/deal", async (
                Guid organizationId, AssignDealRequest request, ISubscriptionService service, CancellationToken ct) =>
            {
                Result<SubscriptionDto> result = await service.AssignDealAsync(organizationId, request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
            .AddEndpointFilter<ValidationFilter<AssignDealRequest>>()
            .WithTags("SuperAdmin");
    }
}
