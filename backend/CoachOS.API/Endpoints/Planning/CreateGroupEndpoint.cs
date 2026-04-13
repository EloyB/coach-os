using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Planning;
using CoachOS.Application.Planning.DTOs;

namespace CoachOS.API.Endpoints.Planning;

public class CreateGroupEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/planning/groups",
            async (Guid id, CreateGroupRequest request,
                IAssignmentService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.CreateGroupAsync(id, request, ctx.GetOrganizationId(), ct);
                return result.IsSuccess
                    ? Results.Created($"/lessonseries/{id}/planning/groups/{result.Value}", result.Value)
                    : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<CreateGroupRequest>>()
        .WithTags("Planning");
    }
}
