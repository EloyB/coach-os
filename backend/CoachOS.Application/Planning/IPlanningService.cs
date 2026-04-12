using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Planning;

public interface IPlanningService
{
    Task<Result<PlanningOverviewDto>> GenerateProposalAsync(
        Guid seriesId, Guid organizationId, bool force = false, CancellationToken ct = default);

    Task<Result<bool>> DeleteAssignmentAsync(
        Guid seriesId, Guid assignmentId, Guid organizationId, CancellationToken ct = default);

    Task<Result<PlanningOverviewDto>> GetPlanningOverviewAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct = default);

    Task<Result<bool>> UpdateAssignmentAsync(
        Guid seriesId, Guid assignmentId, UpdateAssignmentRequest request,
        Guid organizationId, CancellationToken ct = default);

    Task<Result<bool>> CreateAssignmentAsync(
        Guid seriesId, CreateAssignmentRequest request,
        Guid organizationId, CancellationToken ct = default);

    Task<Result<Guid>> CreateGroupAsync(
        Guid seriesId, CreateGroupRequest request, Guid organizationId, CancellationToken ct = default);

    Task<Result<bool>> DissolveGroupAsync(
        Guid seriesId, Guid groupId, Guid organizationId, CancellationToken ct = default);

    Task<Result<bool>> ConfirmScheduleAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct = default);
}
