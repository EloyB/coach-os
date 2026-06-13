using CoachOS.API.Extensions;
using CoachOS.Application.Camps;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

public class DeleteCampEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/camps/{id:guid}", async (Guid id, ICampService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result result = await service.DeleteAsync(id, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Camps");
    }
}
