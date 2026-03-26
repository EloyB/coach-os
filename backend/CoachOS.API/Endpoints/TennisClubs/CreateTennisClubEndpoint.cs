using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.TennisClubs;
using CoachOS.Application.TennisClubs.DTOs;

namespace CoachOS.API.Endpoints.TennisClubs;

public class CreateTennisClubEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/tennisclubs", async (CreateTennisClubRequest request, ITennisClubService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateTennisClubRequest>>()
        .WithTags("TennisClubs");
    }
}
