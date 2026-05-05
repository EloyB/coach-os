using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;

namespace CoachOS.API.Endpoints.Auth;

public class ResetPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/reset-password", async (
            ResetPasswordRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, ct);

            return result.IsSuccess ? Results.Ok() : result.ToErrorResult();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<ResetPasswordRequest>>()
        .RequireRateLimiting("auth")
        .WithTags("Auth");
    }
}
