using CoachOS.API.Extensions;
using CoachOS.Application.Planning;

namespace CoachOS.API.Endpoints.Planning;

public class AdminConfirmAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/planning/assignments/{assignmentId:guid}/admin-confirm",
            async (Guid id, Guid assignmentId, IPlanningService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.AdminConfirmAssignmentAsync(
                    id, assignmentId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Planning");
    }
}
