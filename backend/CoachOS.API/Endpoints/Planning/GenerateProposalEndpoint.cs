using CoachOS.API.Extensions;
using CoachOS.Application.Planning;

namespace CoachOS.API.Endpoints.Planning;

public class GenerateProposalEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/planning/generate",
            async (Guid id, bool? force, IPlanningService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.GenerateProposalAsync(id, ctx.GetOrganizationId(), force ?? false, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Planning");
    }
}
