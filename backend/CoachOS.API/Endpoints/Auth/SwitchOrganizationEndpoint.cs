using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;

namespace CoachOS.API.Endpoints.Auth;

public class SwitchOrganizationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/switch-organization", async (
            SwitchOrganizationRequest request,
            IAuthService authService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await authService.SwitchOrganizationAsync(ctx.GetUserId(), request.OrganizationId, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<SwitchOrganizationRequest>>()
        .WithTags("Auth");
    }
}
