using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Onboarding;
using CoachOS.Application.Onboarding.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Onboarding;

public class SetTrainerModeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/onboarding/trainer-mode", async (
            SetTrainerModeRequest request,
            IOnboardingService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result<OnboardingStateDto> result = await service.SetTrainerModeAsync(
                ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<SetTrainerModeRequest>>()
        .WithTags("Onboarding");
    }
}
