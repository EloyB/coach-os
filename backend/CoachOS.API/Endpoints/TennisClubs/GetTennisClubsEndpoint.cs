using CoachOS.API.Extensions;
using CoachOS.Application.TennisClubs;

namespace CoachOS.API.Endpoints.TennisClubs;

public class GetTennisClubsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/tennisclubs", async (ITennisClubService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.GetAllAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("TennisClubs");
    }
}
