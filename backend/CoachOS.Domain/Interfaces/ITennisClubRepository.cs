using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ITennisClubRepository
{
    Task<TennisClub?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<TennisClub>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task AddAsync(TennisClub club, CancellationToken ct = default);
    Task DeleteAsync(TennisClub club, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, Guid organizationId, Guid? excludeId, CancellationToken ct = default);
    Task<bool> AnyByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
