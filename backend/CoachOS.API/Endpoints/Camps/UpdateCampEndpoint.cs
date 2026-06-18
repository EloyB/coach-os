using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

public class UpdateCampEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/camps/{id:guid}", async (Guid id, UpdateCampRequest request, ICampService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result result = await service.UpdateAsync(id, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .AddEndpointFilter<ValidationFilter<UpdateCampRequest>>()
        .WithTags("Camps");
    }
}
