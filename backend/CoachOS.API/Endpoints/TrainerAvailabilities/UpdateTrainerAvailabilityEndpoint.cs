using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.TrainerAvailabilities;

public class UpdateTrainerAvailabilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/trainer-availabilities/{id:guid}", async (Guid id, CreateTrainerAvailabilityRequest request, ITrainerAvailabilityService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result result = await service.UpdateAsync(id, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<CreateTrainerAvailabilityRequest>>()
        .WithTags("TrainerAvailabilities");
    }
}
