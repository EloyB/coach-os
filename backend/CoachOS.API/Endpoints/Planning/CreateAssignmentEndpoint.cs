using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Planning;
using CoachOS.Application.Planning.DTOs;

namespace CoachOS.API.Endpoints.Planning;

public class CreateAssignmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/planning/assignments",
            async (Guid id, CreateAssignmentRequest request,
                IPlanningService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.CreateAssignmentAsync(
                    id, request, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<CreateAssignmentRequest>>()
        .WithTags("Planning");
    }
}
