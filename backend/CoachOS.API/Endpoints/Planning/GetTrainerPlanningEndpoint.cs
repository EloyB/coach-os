using CoachOS.API.Extensions;
using CoachOS.Application.Planning;

namespace CoachOS.API.Endpoints.Planning;

public class GetTrainerPlanningEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trainer/planning",
            async (ITrainerPlanningService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.GetAllAsync(ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Planning");
    }
}
