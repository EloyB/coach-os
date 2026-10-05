using CoachOS.API.Extensions;
using CoachOS.Application.Auth;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Auth;

public class ResendEmailVerificationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/resend-verification", async (
            bool? onlyIfExpired,
            IAuthService authService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result<bool> result = await authService.ResendEmailVerificationAsync(
                ctx.GetUserId(), onlyIfExpired ?? false, ct);
            return result.IsSuccess ? Results.Ok(new { sent = result.Value }) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Auth");
    }
}
