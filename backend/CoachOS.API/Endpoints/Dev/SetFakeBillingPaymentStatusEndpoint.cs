using CoachOS.API.Extensions;
using CoachOS.Application.Billing;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Mollie;

namespace CoachOS.API.Endpoints.Dev;

/// <summary>
/// Enkel Development (namespace Endpoints.Dev): laat een betaling van de fake-Mollie slagen of
/// mislukken en verwerkt ze meteen, zoals de webhook zou doen. Bestaat enkel als er geen echte
/// Mollie-key is (anders 404).
/// </summary>
public class SetFakeBillingPaymentStatusEndpoint : IEndpoint
{
    public sealed record FakeStatusRequest(string Status);

    private static readonly string[] AllowedStatuses = ["paid", "failed", "canceled", "expired", "open"];

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/dev/billing/fake-payments/{id}/status", async (
                string id, FakeStatusRequest request, IServiceProvider services,
                IBillingPaymentSyncService sync, HttpContext ctx, CancellationToken ct) =>
            {
                FakeBillingMollieClient? fake = services.GetService<FakeBillingMollieClient>();
                if (fake is null)
                    return Results.NotFound();
                // Enkel betalingen van de eigen organisatie.
                if (!await sync.IsOwnPaymentAsync(ctx.GetOrganizationId(), id, ct))
                    return Results.NotFound();
                if (!AllowedStatuses.Contains(request.Status))
                    return Results.BadRequest(new[] { "Ongeldige status" });
                if (!fake.SetFakeStatus(id, request.Status))
                    return Results.NotFound();

                Result result = await sync.SyncAsync(id, ct);
                return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors.Select(e => e.Message));
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("Dev");
    }
}
