using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Dashboard;

public interface IDashboardService
{
    Task<Result<DashboardSummaryDto>> GetSummaryAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<InboxDto>> GetInboxAsync(Guid organizationId, int limit = 10, CancellationToken ct = default);
    Task<Result<DashboardMetricsDto>> GetMetricsAsync(Guid organizationId, int weeks = 7, CancellationToken ct = default);
}
