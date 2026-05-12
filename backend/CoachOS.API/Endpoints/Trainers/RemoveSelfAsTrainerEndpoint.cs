using CoachOS.API.Extensions;
using CoachOS.Application.Trainers;

namespace CoachOS.API.Endpoints.Trainers;

public class RemoveSelfAsTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trainers/me", async (ITrainerService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.RemoveSelfAsTrainerAsync(ctx.GetUserId(), ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Trainers");
    }
}
