using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Planning;

public interface IPlanningService
{
    Task<Result<PlanningOverviewDto>> GenerateProposalAsync(
        Guid seriesId, Guid organizationId, bool force = false, CancellationToken ct = default);

    Task<Result<PlanningOverviewDto>> GetPlanningOverviewAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct = default);
}
