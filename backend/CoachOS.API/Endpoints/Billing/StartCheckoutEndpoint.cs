using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Billing;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Billing;

public class StartCheckoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/billing/checkout", async (
                CheckoutRequest request, IBillingService service, HttpContext ctx, CancellationToken ct) =>
            {
                Result<CheckoutResultDto> result = await service.StartCheckoutAsync(ctx.GetOrganizationId(), request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .AddEndpointFilter<ValidationFilter<CheckoutRequest>>()
            .WithTags("Billing");
    }
}
