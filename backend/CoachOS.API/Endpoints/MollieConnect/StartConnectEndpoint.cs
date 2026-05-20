using CoachOS.API.Extensions;
using CoachOS.Application.MollieConnect;
using CoachOS.Application.MollieConnect.DTOs;
using CoachOS.Domain.Models;

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
            HttpContext ctx,
            CancellationToken ct) =>
        {
            string redirectUri = BuildRedirectUri(ctx);
            Result<StartConnectResponse> result = await service.StartAsync(
                ctx.GetOrganizationId(), redirectUri, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("MollieConnect");
    }

    /// <summary>
    /// Bouwt de redirect URI consistent op basis van de inkomende request, zodat
    /// dev (localhost) en prod (app.coach-os.be) hetzelfde codepad delen. De URL
    /// moet exact matchen met wat in het Mollie dashboard is geregistreerd.
    /// </summary>
    internal static string BuildRedirectUri(HttpContext ctx)
    {
        return $"{ctx.Request.Scheme}://{ctx.Request.Host}/api/oauth/mollie/callback";
    }
}
