using CoachOS.API.Extensions;
using CoachOS.Application.Auth;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Auth;

public class ResendEmailVerificationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/resend-verification", async (
            IAuthService authService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result result = await authService.ResendEmailVerificationAsync(ctx.GetUserId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Auth");
    }
}
