using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

/// <summary>
/// Abonnementen worden cross-org beheerd door de superadmin, dus de implementatie
/// negeert de tenant query filter en scopet expliciet op <c>organizationId</c>.
/// </summary>
public interface ISubscriptionRepository
{
    /// <summary>Het abonnement van een organisatie, of null. <paramref name="tracked"/> voor mutatie.</summary>
    Task<Subscription?> GetByOrganizationAsync(
        Guid organizationId, bool tracked = false, CancellationToken ct = default);

    Task AddAsync(Subscription subscription, CancellationToken ct = default);
}
