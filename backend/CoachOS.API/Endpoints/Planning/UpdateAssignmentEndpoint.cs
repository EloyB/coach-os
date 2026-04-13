using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Planning;
using CoachOS.Application.Planning.DTOs;

namespace CoachOS.API.Endpoints.Planning;

public class UpdateAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/lessonseries/{id:guid}/planning/assignments/{assignmentId:guid}",
            async (Guid id, Guid assignmentId, UpdateAssignmentRequest request,
                IAssignmentService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.UpdateAssignmentAsync(
                    id, assignmentId, request, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<UpdateAssignmentRequest>>()
        .WithTags("Planning");
    }
}
