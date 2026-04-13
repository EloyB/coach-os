using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Planning;

public interface IConfirmationOrchestrationService
{
    Task<Result<bool>> ConfirmScheduleAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct = default);

    Task<Result<List<NonResponderDto>>> GetNonRespondersAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct = default);

    Task<Result<bool>> ResendConfirmationEmailAsync(
        Guid seriesId, Guid assignmentId, Guid organizationId, CancellationToken ct = default);

    Task<Result<bool>> AdminConfirmAssignmentAsync(
        Guid seriesId, Guid assignmentId, Guid organizationId, CancellationToken ct = default);
}
