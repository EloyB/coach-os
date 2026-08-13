using CoachOS.API.Extensions;
using CoachOS.Application.Planning;

namespace CoachOS.API.Endpoints.Planning;

public class SendAssignmentConfirmationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/planning/assignments/{assignmentId:guid}/send-confirmation",
            async (Guid id, Guid assignmentId, IConfirmationOrchestrationService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.SendAssignmentConfirmationAsync(
                    id, assignmentId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Planning");
    }
}
