using CoachOS.API.Extensions;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;

namespace CoachOS.API.Endpoints.Trainers;

public class SetHeadTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/trainers/{id:guid}/head-trainer",
            async (Guid id, SetHeadTrainerRequest request, ITrainerService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.SetHeadTrainerAsync(
                    id, ctx.GetOrganizationId(), request.IsHeadTrainer, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Trainers");
    }
}
