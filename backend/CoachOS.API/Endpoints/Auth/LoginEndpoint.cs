using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;

namespace CoachOS.API.Endpoints.Auth;

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            LoginRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.LoginAsync(request.Email, request.Password, ct);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<LoginRequest>>()
        .RequireRateLimiting("auth")
        .WithTags("Auth");
    }
}
