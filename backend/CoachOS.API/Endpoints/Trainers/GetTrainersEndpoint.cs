using CoachOS.API.Extensions;
using CoachOS.Application.Trainers;

namespace CoachOS.API.Endpoints.Trainers;

public class GetTrainersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trainers", async (bool? all, ITrainerService service, HttpContext ctx, CancellationToken ct) =>
        {
            // ?all=true: beheerpagina, altijd alle leden incl. extra admins.
            var result = await service.GetTrainersAsync(ctx.GetOrganizationId(), all ?? false, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Trainers");
    }
}
