using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Auth;

public class VerifyEmailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/verify-email", async (
            VerifyEmailRequest request,
            IAuthService authService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result result = await authService.VerifyEmailAsync(ctx.GetUserId(), request.Code, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<VerifyEmailRequest>>()
        .WithTags("Auth");
    }
}
