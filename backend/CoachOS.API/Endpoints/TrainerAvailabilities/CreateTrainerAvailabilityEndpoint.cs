using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.TrainerAvailabilities;

public class CreateTrainerAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trainer-availabilities", async (CreateTrainerAvailabilityRequest request, ITrainerAvailabilityService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<Guid> result = await service.CreateAsync(ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/trainer-availabilities/{result.Value}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateTrainerAvailabilityRequest>>()
        .WithTags("TrainerAvailabilities");
    }
}
