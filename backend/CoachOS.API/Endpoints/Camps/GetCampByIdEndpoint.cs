using CoachOS.API.Extensions;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

public class GetCampByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/camps/{id:guid}", async (Guid id, ICampService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<CampDetailDto> result = await service.GetByIdAsync(id, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Camps");
    }
}
