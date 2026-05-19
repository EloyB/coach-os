using CoachOS.API.Extensions;
using CoachOS.Application.Auth;

namespace CoachOS.API.Endpoints.Auth;

/// <summary>
/// Bevestigt of een invite-token nog bruikbaar is. De FE roept dit bij page-load
/// zodat een reeds geaccepteerde of verlopen link niet langer het wachtwoord-formulier toont.
/// </summary>
public class ValidateInviteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/invite/{token}/validate", async (
            string token,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.ValidateInviteTokenAsync(token, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .AllowAnonymous()
        .RequireRateLimiting("auth")
        .WithTags("Auth");
    }
}
