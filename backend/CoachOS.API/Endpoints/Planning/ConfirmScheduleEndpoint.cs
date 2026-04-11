using CoachOS.API.Extensions;
using CoachOS.Application.Planning;

namespace CoachOS.API.Endpoints.Planning;

public class ConfirmScheduleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/planning/confirm",
            async (Guid id, IPlanningService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.ConfirmScheduleAsync(id, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Planning");
    }
}
