using CoachOS.Application.Billing;
using CoachOS.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace CoachOS.API.Endpoints.Webhooks;

/// <summary>
/// Webhook van ons eigen Mollie-account (CoachOS-abonnementen), los van de Connect-webhook
/// <c>/webhooks/mollie</c>. Mollie stuurt enkel <c>id=tr_xxx</c>; de status halen we zelf op.
/// Antwoordt 200, behalve 503 als Mollie zelf niet bereikbaar was (dan probeert Mollie opnieuw).
/// </summary>
public class MollieBillingWebhookEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/mollie-billing", async (
                [FromForm] string? id,
                IBillingPaymentSyncService service,
                ILogger<MollieBillingWebhookEndpoint> logger,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogWarning("Billing-webhook zonder payment id.");
                    return Results.Ok();
                }

                Result result = await service.SyncAsync(id, ct);
                if (!result.IsSuccess)
                {
                    logger.LogWarning("Billing-webhook sync faalde voor {Id}: {Errors}",
                        id, string.Join(", ", result.Errors.Select(e => e.Message)));
                }
                return Results.StatusCode(BillingWebhookResponses.StatusCodeFor(result));
            })
            .AllowAnonymous()
            .RequireRateLimiting("public")
            .DisableAntiforgery()
            .WithTags("Webhooks");
    }
}
