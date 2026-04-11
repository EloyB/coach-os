using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;

namespace CoachOS.API.Endpoints.Auth;

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (
            RegisterRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.RegisterAsync(
                request.OrganizationName, request.FirstName, request.LastName,
                request.Email, request.Password, ct);

            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
        .RequireRateLimiting("auth")
        .WithTags("Auth");
    }
}
