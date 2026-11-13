using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.TrainerAvailabilities;

public class UpdateOwnTrainerAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/me/trainer-availabilities/{id:guid}", async (Guid id, SaveOwnTrainerAvailabilityRequest request, ITrainerAvailabilityService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result result = await service.UpdateForTrainerAsync(id, ctx.GetOrganizationId(), ctx.GetUserId(), request, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Trainer"))
        .AddEndpointFilter<ValidationFilter<SaveOwnTrainerAvailabilityRequest>>()
        .WithTags("TrainerAvailabilities");
    }
}
