using CoachOS.API.Extensions;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;
using CoachOS.API.Filters;

namespace CoachOS.API.Endpoints.Trainers;

public class SetHeadTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/trainers/{id:guid}/head-trainer-clubs",
            async (Guid id, SetHeadTrainerClubsRequest request, ITrainerService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.SetHeadTrainerClubsAsync(
                    id, ctx.GetOrganizationId(), request.ClubIds, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<SetHeadTrainerClubsRequest>>()
        .WithTags("Trainers");
    }
}
