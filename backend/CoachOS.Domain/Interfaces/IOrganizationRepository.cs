using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

/// <summary>
/// Leestoegang tot organisaties. <see cref="Organization"/> heeft geen tenant query
/// filter, dus dit werkt cross-org (gebruikt door superadmin-flows).
/// </summary>
public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, bool tracked = false, CancellationToken ct = default);
}
