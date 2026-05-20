using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IMollieConnectionRepository
{
    /// <summary>Tracked read; gebruik voor mutaties (token refresh).</summary>
    Task<MollieConnection?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>AsNoTracking read; gebruik voor read-only checks zoals "is verbonden?".</summary>
    Task<MollieConnection?> GetByOrganizationReadOnlyAsync(Guid organizationId, CancellationToken ct = default);

    Task AddAsync(MollieConnection connection, CancellationToken ct = default);

    Task DeleteByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
