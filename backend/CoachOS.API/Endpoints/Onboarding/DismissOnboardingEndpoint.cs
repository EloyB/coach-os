using CoachOS.API.Extensions;
using CoachOS.Application.Onboarding;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Onboarding;

public class DismissOnboardingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/onboarding/dismiss", async (
            IOnboardingService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result result = await service.DismissAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Onboarding");
    }
}
