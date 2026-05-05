using CoachOS.API.Filters;
using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;

namespace CoachOS.API.Endpoints.Auth;

public class ForgotPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/forgot-password", async (
            ForgotPasswordRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.ForgotPasswordAsync(request.Email, request.ResetBaseUrl, ct);

            // Always return 200 to prevent e-mail enumeration.
            return Results.Ok();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<ForgotPasswordRequest>>()
        .RequireRateLimiting("auth")
        .WithTags("Auth");
    }
}
