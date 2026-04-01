using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Dashboard;

public interface IDashboardService
{
    Task<Result<DashboardSummaryDto>> GetSummaryAsync(Guid organizationId, CancellationToken ct = default);
}
