using CoachOS.API.Extensions;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Subscriptions;

public class GetSubscriptionStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/subscription/status", async (
            ISubscriptionAccessService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result<SubscriptionStatusDto> result = await service.GetStatusAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Subscription");
    }
}
