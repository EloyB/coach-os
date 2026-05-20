using CoachOS.Application.Configuration;
using CoachOS.Application.MollieConnect;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Options;

namespace CoachOS.API.Endpoints.MollieConnect;

/// <summary>
/// Mollie OAuth callback. Anoniem — Mollie roept dit aan in de browser van de
/// admin. Server-side redirect naar de frontend settings-pagina met een
/// query param zodat de UI een toast kan tonen.
/// </summary>
public class CallbackEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/oauth/mollie/callback", async (
            string? code,
            string? state,
            string? error,
            IMollieConnectService service,
            IOptions<AppOptions> appOptions,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            string settingsUrl = $"{appOptions.Value.FrontendBaseUrl.TrimEnd('/')}/dashboard/settings";

            // Mollie stuurt error= wanneer de gebruiker weigerde of er iets misging.
            if (!string.IsNullOrEmpty(error))
            {
                return Results.Redirect($"{settingsUrl}?mollie=error");
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return Results.Redirect($"{settingsUrl}?mollie=error");
            }

            string redirectUri = StartConnectEndpoint.BuildRedirectUri(ctx);
            Result<Guid> result = await service.HandleCallbackAsync(code, state, redirectUri, ct);

            string queryValue = result.IsSuccess ? "connected" : "error";
            return Results.Redirect($"{settingsUrl}?mollie={queryValue}");
        })
        .AllowAnonymous()
        .WithTags("MollieConnect");
    }
}
