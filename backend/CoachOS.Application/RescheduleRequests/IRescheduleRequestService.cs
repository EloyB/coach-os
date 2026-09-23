using CoachOS.Application.RescheduleRequests.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.RescheduleRequests;

public interface IRescheduleRequestService
{
    Task<Result<Guid>> RequestAsync(
        Guid assignmentId, Guid organizationId, CreateRescheduleRequest request, CancellationToken ct = default);

    Task<Result<List<RescheduleRequestDto>>> GetPendingAsync(
        Guid organizationId, CancellationToken ct = default);

    Task<Result> ResolveAsync(
        Guid id, Guid organizationId, Guid resolvedByUserId, ResolveRescheduleRequest request, CancellationToken ct = default);
}
