using CoachOS.API.Extensions;
using CoachOS.Application.Billing;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Billing;

/// <summary>Abonnement, facturatiegegevens en betalingen van de eigen organisatie. Enkel Admin.</summary>
public class GetBillingOverviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/billing", async (IBillingService service, HttpContext ctx, CancellationToken ct) =>
            {
                Result<BillingOverviewDto> result = await service.GetOverviewAsync(ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("Billing");
    }
}
