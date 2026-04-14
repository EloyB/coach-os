using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.StudentAuth;
using CoachOS.Application.StudentAuth.DTOs;

namespace CoachOS.API.Endpoints.Auth;

public class RequestMagicLinkEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/magic/request", async (
            RequestMagicLinkRequest request,
            IStudentMagicLinkService service,
            CancellationToken ct) =>
        {
            // Always return 202 — never leak whether an email is known.
            await service.RequestAsync(request.Email, ct);
            return Results.Accepted();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<RequestMagicLinkRequest>>()
        .RequireRateLimiting("auth")
        .WithTags("Auth");
    }
}
