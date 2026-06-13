using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

public class SaveCampFormEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/camps/{id:guid}/form", async (Guid id, SaveCampFormRequest request, ICampService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<Guid> result = await service.SaveFormAsync(id, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .AddEndpointFilter<ValidationFilter<SaveCampFormRequest>>()
        .WithTags("Camps");
    }
}
