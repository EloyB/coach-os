using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

public class CreateCampEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/camps", async (CreateCampRequest request, ICampService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<Guid> result = await service.CreateAsync(ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.Created($"/api/camps/{result.Value}", result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .AddEndpointFilter<ValidationFilter<CreateCampRequest>>()
        .WithTags("Camps");
    }
}
