using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.StudentAuth;
using CoachOS.Application.StudentAuth.DTOs;

namespace CoachOS.API.Endpoints.Auth;

public class RedeemMagicLinkEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/magic/redeem", async (
            RedeemMagicLinkRequest request,
            IStudentMagicLinkService service,
            CancellationToken ct) =>
        {
            var result = await service.RedeemAsync(request.Token, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<RedeemMagicLinkRequest>>()
        .RequireRateLimiting("auth")
        .WithTags("Auth");
    }
}
