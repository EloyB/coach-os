using CoachOS.API.Extensions;
using CoachOS.Application.Planning;

namespace CoachOS.API.Endpoints.Planning;

public class DissolveGroupEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{id:guid}/planning/groups/{groupId:guid}",
            async (Guid id, Guid groupId, IAssignmentService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.DissolveGroupAsync(id, groupId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Planning");
    }
}
