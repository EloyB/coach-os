using CoachOS.Application.Reschedule.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Reschedule;

public interface IRescheduleService
{
    Task<Result<Guid>> RequestAsync(
        Guid assignmentId, Guid organizationId, CreateRescheduleRequest request, CancellationToken ct = default);

    Task<Result<List<RescheduleRequestDto>>> GetPendingAsync(
        Guid organizationId, CancellationToken ct = default);

    Task<Result> ResolveAsync(
        Guid id, Guid organizationId, Guid resolvedByUserId, ResolveRescheduleRequest request, CancellationToken ct = default);
}
