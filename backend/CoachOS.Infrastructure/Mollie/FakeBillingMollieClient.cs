using CoachOS.Application.Abstractions;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Mollie;

/// <summary>
/// Dev-fallback voor <see cref="IBillingMollieClient"/> wanneer er nog geen
/// platform-API-key is geconfigureerd. Doet geen echte Mollie-calls: geeft
/// placeholder-ids en een niet-functionele checkout-URL terug zodat de volledige
/// flow (plan koppelen → betaallink → status) lokaal gebouwd en getest kan worden.
/// Gebruik de handmatige "markeer als betaald" om een betaling af te ronden.
/// </summary>
public class FakeBillingMollieClient(ILogger<FakeBillingMollieClient> logger) : IBillingMollieClient
{
    public bool IsConfigured => false;

    public Task<Result<string>> CreateCustomerAsync(string name, string email, CancellationToken ct = default)
    {
        string id = $"cst_fake_{Guid.NewGuid():N}";
        logger.LogInformation("FakeBillingMollieClient: customer aangemaakt ({Id}) voor {Email}", id, email);
        return Task.FromResult(Result<string>.Ok(id));
    }

    public Task<Result<BillingPaymentResult>> CreatePaymentAsync(
        BillingPaymentRequest request, CancellationToken ct = default)
    {
        string id = $"tr_fake_{Guid.NewGuid():N}";
        string checkoutUrl = $"https://fake-mollie.local/checkout/{id}";
        logger.LogInformation(
            "FakeBillingMollieClient: betaling {Id} van {Amount} {Currency} (geen echte Mollie-key geconfigureerd)",
            id, request.Amount, request.Currency);
        return Task.FromResult(Result<BillingPaymentResult>.Ok(new BillingPaymentResult(id, "open", checkoutUrl)));
    }

    public Task<Result<BillingPaymentSnapshot>> GetPaymentAsync(string paymentId, CancellationToken ct = default)
    {
        // Geen state: blijft "open". Rond af via de handmatige "markeer als betaald".
        return Task.FromResult(Result<BillingPaymentSnapshot>.Ok(new BillingPaymentSnapshot(paymentId, "open", null)));
    }
}
