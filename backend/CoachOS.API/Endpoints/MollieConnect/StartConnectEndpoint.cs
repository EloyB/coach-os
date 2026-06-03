using CoachOS.API.Extensions;
using CoachOS.Application.Configuration;
using CoachOS.Application.MollieConnect;
using CoachOS.Application.MollieConnect.DTOs;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Options;

namespace CoachOS.API.Endpoints.MollieConnect;

/// <summary>
/// Genereert de Mollie OAuth-autorisatie-URL voor de huidige organisatie.
/// Frontend roept dit aan en doet vervolgens <c>window.location.href = AuthorizationUrl</c>.
/// </summary>
public class StartConnectEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/mollie-connect/start", async (
            IMollieConnectService service,
            IOptions<MollieOptions> mollieOptions,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            string redirectUri = BuildRedirectUri(ctx, mollieOptions.Value);
            Result<StartConnectResponse> result = await service.StartAsync(
                ctx.GetOrganizationId(), redirectUri, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("MollieConnect");
    }

    /// <summary>
    /// Bepaalt de redirect URI die zowel in de OAuth-autorisatie als in de
    /// token-exchange wordt meegestuurd. Wanneer <see cref="MollieOptions.RedirectUri"/>
    /// is geconfigureerd (prod-secret) wordt die exact gebruikt — dit garandeert een
    /// match met het Mollie dashboard ongeacht het scheme dat een reverse proxy
    /// doorgeeft. Is hij leeg (lokaal dev), dan wordt hij afgeleid van de inkomende
    /// request (<c>{scheme}://{host}/api/oauth/mollie/callback</c>).
    /// </summary>
    internal static string BuildRedirectUri(HttpContext ctx, MollieOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.RedirectUri))
        {
            return options.RedirectUri;
        }

        return $"{ctx.Request.Scheme}://{ctx.Request.Host}/api/oauth/mollie/callback";
    }
}
