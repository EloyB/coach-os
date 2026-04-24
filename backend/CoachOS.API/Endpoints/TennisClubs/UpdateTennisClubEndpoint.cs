using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.TennisClubs;
using CoachOS.Application.TennisClubs.DTOs;

namespace CoachOS.API.Endpoints.TennisClubs;

public class UpdateTennisClubEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/tennisclubs/{id:guid}", async (Guid id, UpdateTennisClubRequest request, ITennisClubService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<UpdateTennisClubRequest>>()
        .WithTags("TennisClubs");
    }
}
