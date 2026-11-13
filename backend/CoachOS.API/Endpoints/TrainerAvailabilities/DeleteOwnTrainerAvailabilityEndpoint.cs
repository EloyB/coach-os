using CoachOS.API.Extensions;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.TrainerAvailabilities;

public class DeleteOwnTrainerAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/me/trainer-availabilities/{id:guid}", async (Guid id, ITrainerAvailabilityService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result result = await service.DeleteForTrainerAsync(id, ctx.GetOrganizationId(), ctx.GetUserId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Trainer"))
        .WithTags("TrainerAvailabilities");
    }
}
