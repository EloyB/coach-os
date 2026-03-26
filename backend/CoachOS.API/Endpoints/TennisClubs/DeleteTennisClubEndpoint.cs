using CoachOS.API.Extensions;
using CoachOS.Application.TennisClubs;

namespace CoachOS.API.Endpoints.TennisClubs;

public class DeleteTennisClubEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/tennisclubs/{id:guid}", async (Guid id, ITennisClubService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("TennisClubs");
    }
}
