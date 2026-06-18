using CoachOS.API.Extensions;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

public class GetCampsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/camps", async (ICampService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<List<CampDto>> result = await service.GetAllAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Camps");
    }
}
