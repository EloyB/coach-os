using CoachOS.API.Extensions;
using CoachOS.Application.Trainers;

namespace CoachOS.API.Endpoints.Trainers;

public class AddSelfAsTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trainers/me", async (ITrainerService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.AddSelfAsTrainerAsync(ctx.GetUserId(), ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Trainers");
    }
}
