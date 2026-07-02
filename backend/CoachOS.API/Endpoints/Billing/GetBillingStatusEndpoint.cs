using CoachOS.API.Extensions;
using CoachOS.Application.Billing;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Billing;

public class GetBillingStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/billing/status", async (
            IBillingService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result<SubscriptionStatusDto> result = await service.GetStatusAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Billing");
    }
}
