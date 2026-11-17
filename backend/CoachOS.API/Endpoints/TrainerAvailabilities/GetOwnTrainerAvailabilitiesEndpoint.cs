using CoachOS.API.Extensions;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.TrainerAvailabilities;

public class GetOwnTrainerAvailabilitiesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/me/trainer-availabilities", async (ITrainerAvailabilityService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<List<TrainerAvailabilityDto>> result = await service.GetForTrainerAsync(ctx.GetOrganizationId(), ctx.GetUserId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Trainer"))
        .WithTags("TrainerAvailabilities");
    }
}
