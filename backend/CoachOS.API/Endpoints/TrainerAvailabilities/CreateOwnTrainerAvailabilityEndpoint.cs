using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.TrainerAvailabilities;

public class CreateOwnTrainerAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/me/trainer-availabilities", async (SaveOwnTrainerAvailabilityRequest request, ITrainerAvailabilityService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<Guid> result = await service.CreateForTrainerAsync(ctx.GetOrganizationId(), ctx.GetUserId(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/me/trainer-availabilities/{result.Value}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Trainer"))
        .AddEndpointFilter<ValidationFilter<SaveOwnTrainerAvailabilityRequest>>()
        .WithTags("TrainerAvailabilities");
    }
}
