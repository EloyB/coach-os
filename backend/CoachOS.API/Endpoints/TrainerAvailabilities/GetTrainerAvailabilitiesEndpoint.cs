using CoachOS.API.Extensions;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.TrainerAvailabilities;

public class GetTrainerAvailabilitiesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trainer-availabilities", async (ITrainerAvailabilityService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<List<TrainerAvailabilityDto>> result = await service.GetAllAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("TrainerAvailabilities");
    }
}
