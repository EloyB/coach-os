using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Auth;
using CoachOS.Application.Trainers.DTOs;

namespace CoachOS.API.Endpoints.Auth;

/// <summary>
/// Generieke accept-invite endpoint — werkt voor zowel trainer- als admin-invites.
/// De pending OrganizationMembership van de user bepaalt z'n rol na acceptatie.
/// </summary>
public class AcceptInviteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/accept-invite", async (
            AcceptInviteRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.AcceptInviteAsync(request.Token, request.Password, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<AcceptInviteRequest>>()
        .RequireRateLimiting("auth")
        .WithTags("Auth");
    }
}
