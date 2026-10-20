using CoachOS.API.Extensions;
using CoachOS.Application.Billing;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Billing;

/// <summary>Na terugkeer van Mollie: status ophalen (ook zonder webhook) en het overzicht teruggeven.</summary>
public class SyncCheckoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/billing/checkout/sync", async (
                IBillingPaymentSyncService service, HttpContext ctx, CancellationToken ct) =>
            {
                Result<BillingOverviewDto> result = await service.SyncLatestCheckoutAsync(ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("Billing");
    }
}
