using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;

namespace CoachOS.API.Endpoints.Trainers;

public class UpdateTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/trainers/{id:guid}", async (Guid id, UpdateTrainerRequest request, ITrainerService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<UpdateTrainerRequest>>()
        .WithTags("Trainers");
    }
}
