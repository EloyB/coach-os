using CoachOS.API.Extensions;
using CoachOS.Application.Onboarding;
using CoachOS.Application.Onboarding.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Onboarding;

public class GetOnboardingStateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/onboarding/state", async (
            IOnboardingService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result<OnboardingStateDto> result = await service.GetStateAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Onboarding");
    }
}
