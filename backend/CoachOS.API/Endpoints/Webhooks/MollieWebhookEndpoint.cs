using CoachOS.Application.Payments;
using CoachOS.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace CoachOS.API.Endpoints.Webhooks;

/// <summary>
/// Mollie payment webhook. Mollie POSTet hier <c>application/x-www-form-urlencoded</c>
/// met enkel <c>id=tr_xxx</c>; we vertrouwen die body NIET en halen de
/// authoritative payment status zelf op via <see cref="IPaymentService"/>.
///
/// Mollie verwacht een 2xx response anders re-tried hij — we returnen daarom
/// altijd 200 OK, ook bij interne fouten (de service logt). Anders zou een
/// transient bug Mollie's retry-backoff veroorzaken en de hele flow blokkeren.
/// </summary>
public class MollieWebhookEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/mollie", async (
            [FromForm] string? id,
            IPaymentService service,
            ILogger<MollieWebhookEndpoint> logger,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                logger.LogWarning("Mollie webhook ontving geen payment id.");
                return Results.Ok();
            }

            Result result = await service.SyncPaymentFromMollieAsync(id, ct);
            if (!result.IsSuccess)
            {
                logger.LogWarning("Mollie webhook sync faalde voor {Id}: {Errors}",
                    id, string.Join(", ", result.Errors.Select(e => e.Message)));
            }
            return Results.Ok();
        })
        .AllowAnonymous()
        .RequireRateLimiting("public")
        .DisableAntiforgery()
        .WithTags("Webhooks");
    }
}
