using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IOrganizationSettingsRepository
{
    /// <summary>Tracked read; gebruik voor mutaties.</summary>
    Task<OrganizationSettings?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>AsNoTracking read; gebruik voor query-paden zoals trainerlijst-filtering.</summary>
    Task<OrganizationSettings?> GetByOrganizationReadOnlyAsync(Guid organizationId, CancellationToken ct = default);

    Task AddAsync(OrganizationSettings settings, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
