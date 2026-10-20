using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

/// <summary>
/// Abonnementen worden ook cross-org gebruikt (superadmin, systeemjobs, webhooks); methodes die dat
/// doen negeren de tenant query filter en scopen expliciet op <c>organizationId</c> of id.
/// </summary>
public interface ISubscriptionRepository
{
    /// <summary>Het abonnement van een organisatie, of null. <paramref name="tracked"/> voor mutatie. Cross-org.</summary>
    Task<Subscription?> GetByOrganizationAsync(
        Guid organizationId, bool tracked = false, CancellationToken ct = default);

    Task AddAsync(Subscription subscription, CancellationToken ct = default);

    /// <summary>Tracked, cross-org: het abonnement dat bij een Mollie Subscription hoort (verlengingswebhook).</summary>
    Task<Subscription?> GetByMollieSubscriptionIdAsync(string mollieSubscriptionId, CancellationToken ct = default);

    /// <summary>
    /// Tracked, cross-org: actieve of opgezegde abonnementen per overschrijving of deal waarvan de
    /// periode uiterlijk op <paramref name="periodEndHorizon"/> eindigt (dagelijkse billing-job).
    /// </summary>
    Task<IReadOnlyList<Subscription>> ListForBillingMaintenanceAsync(DateOnly periodEndHorizon, CancellationToken ct = default);

    /// <summary>
    /// Tracked, cross-org: actieve Mollie-abonnementen (geen deal, niet opgezegd) zonder Mollie
    /// Subscription — de automatische verlenging werd niet (of niet volledig) aangemaakt.
    /// </summary>
    Task<IReadOnlyList<Subscription>> ListMissingAutomaticRenewalAsync(CancellationToken ct = default);

    /// <summary>AsNoTracking read; gebruik voor toegangs- en statuschecks.</summary>
    Task<Subscription?> GetByOrganizationReadOnlyAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Alle lopende proeven over álle organisaties heen, inclusief de organisatie zelf.
    /// Enkel voor de systeemjob die proef-herinneringen verstuurt — nooit vanuit een request.
    /// </summary>
    Task<IReadOnlyList<Subscription>> GetAllTrialingReadOnlyAsync(CancellationToken ct = default);

    /// <summary>
    /// Zet <see cref="Subscription.LastTrialReminderDay"/> atomair van <paramref name="expectedCurrent"/>
    /// naar <paramref name="newValue"/>. Geeft false terug als een andere instantie voor was.
    /// </summary>
    Task<bool> TrySetLastTrialReminderDayAsync(
        Guid subscriptionId, int? expectedCurrent, int? newValue, CancellationToken ct = default);
}
