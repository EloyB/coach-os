using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Billing;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Billing;

public class SaveBillingProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/billing/profile", async (
                SaveBillingProfileRequest request, IBillingService service, HttpContext ctx, CancellationToken ct) =>
            {
                Result<BillingProfileDto> result = await service.SaveBillingProfileAsync(ctx.GetOrganizationId(), request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .AddEndpointFilter<ValidationFilter<SaveBillingProfileRequest>>()
            .WithTags("Billing");
    }
}
