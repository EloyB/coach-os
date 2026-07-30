using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Billing;

public interface IBillingService
{
    Task<Result<SubscriptionStatusDto>> GetStatusAsync(Guid organizationId, CancellationToken ct = default);
}
