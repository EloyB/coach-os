using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ISubscriptionRepository
{
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
