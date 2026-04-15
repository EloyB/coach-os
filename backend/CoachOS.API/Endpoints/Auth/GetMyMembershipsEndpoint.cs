using CoachOS.API.Extensions;
using CoachOS.Application.Auth;

namespace CoachOS.API.Endpoints.Auth;

public class GetMyMembershipsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/memberships", async (
            IAuthService authService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await authService.GetMembershipsAsync(ctx.GetUserId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Auth");
    }
}
