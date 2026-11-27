using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.AccountEmail;
using CoachOS.Application.AccountEmail.DTOs;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Auth;

public class AccountEmailEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/auth/email-change")
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Admin", "Trainer")
                .RequireAssertion(context => Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out _)))
            .RequireRateLimiting("account-email-change")
            .WithTags("Auth");

        group.MapPost("/request", async (RequestAccountEmailChange request, IAccountEmailService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result result = await service.RequestAsync(ctx.GetUserId(), request, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        }).AddEndpointFilter<ValidationFilter<RequestAccountEmailChange>>();

        group.MapPost("/confirm", async (ConfirmAccountEmailChange request, IAccountEmailService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<AuthResponseDto> result = await service.ConfirmAsync(ctx.GetUserId(), ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        }).AddEndpointFilter<ValidationFilter<ConfirmAccountEmailChange>>();
    }
}