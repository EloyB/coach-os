using CoachOS.API.Extensions;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.TrainerAvailabilities;

public class DeleteTrainerAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trainer-availabilities/{id:guid}", async (Guid id, ITrainerAvailabilityService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result result = await service.DeleteAsync(id, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("TrainerAvailabilities");
    }
}
