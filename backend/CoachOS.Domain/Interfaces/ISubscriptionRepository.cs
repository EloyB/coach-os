using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
}
