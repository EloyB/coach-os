using CoachOS.API.Extensions;
using CoachOS.Application.Planning;

namespace CoachOS.API.Endpoints.Planning;

public class DeleteAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{id:guid}/planning/assignments/{assignmentId:guid}",
            async (Guid id, Guid assignmentId, IAssignmentService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.DeleteAssignmentAsync(
                    id, assignmentId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Planning");
    }
}
