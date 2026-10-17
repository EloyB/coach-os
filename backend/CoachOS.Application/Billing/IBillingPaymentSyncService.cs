using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Billing;

/// <summary>
/// Verwerkt de status van Mollie-betalingen van het platformaccount (webhook en terugkeer na
/// checkout). Idempotent: dezelfde betaling twee keer of gelijktijdig verwerken heeft hetzelfde
/// resultaat als één keer.
/// </summary>
public interface IBillingPaymentSyncService
{
    Task<Result> SyncAsync(string molliePaymentId, CancellationToken ct = default);

    /// <summary>
    /// Synct de laatste openstaande eerste betaling van de organisatie (terugkeer van Mollie,
    /// ook zonder webhook) en geeft het actuele overzicht terug.
    /// </summary>
    Task<Result<BillingOverviewDto>> SyncLatestCheckoutAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Herstel: betaalde Mollie-abonnementen zonder Mollie Subscription (crash of Mollie-fout na de
    /// eerste betaling) krijgen alsnog hun automatische verlenging. Geeft het aantal herstelde terug.
    /// </summary>
    Task<int> ReconcileMissingRenewalsAsync(DateOnly today, CancellationToken ct = default);
}
