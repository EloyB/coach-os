using CoachOS.API.Extensions;
using CoachOS.Application.Billing;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Billing;

public class CancelSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/billing/cancel", async (IBillingService service, HttpContext ctx, CancellationToken ct) =>
            {
                Result<BillingOverviewDto> result = await service.CancelAsync(ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("Billing");
    }
}
