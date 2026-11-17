using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

/// <summary>
/// Betalingen van CoachOS-abonnementen. Webhooks en jobs werken zonder tenant-context, dus
/// de implementatie negeert de tenant-filter en scopet expliciet op organisatie of Mollie-id.
/// </summary>
public interface ISubscriptionPaymentRepository
{
    Task AddAsync(SubscriptionPayment payment, CancellationToken ct = default);

    /// <summary>Tracked; null als de betaling niet bij de organisatie hoort.</summary>
    Task<SubscriptionPayment?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    /// <summary>Tracked, cross-org (webhook).</summary>
    Task<SubscriptionPayment?> GetByMolliePaymentIdAsync(string molliePaymentId, CancellationToken ct = default);

    /// <summary>AsNoTracking, laatst aangemaakte betaling eerst.</summary>
    Task<IReadOnlyList<SubscriptionPayment>> ListByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Tracked: de laatst aangemaakte openstaande eerste Mollie-betaling, of null.</summary>
    Task<SubscriptionPayment?> GetLatestOpenFirstAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// AsNoTracking: de laatst aangemaakte openstaande Mollie-betaling waar de club zelf langs de
    /// checkout gaat (eerste betaling of losse betaallink), met een Mollie payment-id; of null.
    /// </summary>
    Task<SubscriptionPayment?> GetLatestOpenCheckoutAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Laatste einddatum van een periode die echt aangerekend werd: betaald, of een automatische
    /// verlenging die nog loopt of mislukte (die periode wordt dan via een losse betaallink gedekt).
    /// Afgebroken, verlopen en terugbetaalde betalingen tellen niet mee.
    /// </summary>
    Task<DateOnly?> GetLatestBilledPeriodEndAsync(Guid subscriptionId, CancellationToken ct = default);

    /// <summary>AsNoTracking: betalingen waarvoor nu een factuur(poging) moet gebeuren.</summary>
    /// <summary>
    /// Betaalde betalingen waarvan de factuur onbetaald verstuurd is en nog als betaald
    /// doorgegeven moet worden (over alle organisaties; voor de worker).
    /// </summary>
    Task<IReadOnlyList<SubscriptionPayment>> ListInvoicePaidSyncDueAsync(
        DateTime nowUtc, int maxAttempts, int take, CancellationToken ct = default);

    Task<IReadOnlyList<SubscriptionPayment>> ListInvoiceDueAsync(
        DateTime nowUtc, int maxAttempts, int take, CancellationToken ct = default);

    /// <summary>AsNoTracking: alle betalingen van een abonnement.</summary>
    Task<IReadOnlyList<SubscriptionPayment>> ListBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);
}
