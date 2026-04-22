using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Interfaces;

public interface IRescheduleRequestRepository
{
    Task<RescheduleRequest?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<List<RescheduleRequest>> GetPendingByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<int> CountPendingByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<bool> HasPendingForAssignmentAsync(Guid assignmentId, CancellationToken ct = default);
    Task AddAsync(RescheduleRequest request, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
