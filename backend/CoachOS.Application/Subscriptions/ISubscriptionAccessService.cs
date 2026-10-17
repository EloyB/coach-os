using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Subscriptions;

/// <summary>
/// De enige plek die bepaalt wat een organisatie op basis van haar abonnement mag.
/// </summary>
public interface ISubscriptionAccessService
{
    Task<Result<SubscriptionStatusDto>> GetStatusAsync(Guid organizationId, CancellationToken ct = default);
}
