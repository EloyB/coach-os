using CoachOS.API.Extensions;
using CoachOS.Application.Trainers;

namespace CoachOS.API.Endpoints.Trainers;

public class RemoveTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/trainers/{id:guid}/remove", async (Guid id, ITrainerService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.RemoveAsync(id, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Trainers");
    }
}
