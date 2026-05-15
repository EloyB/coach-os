using CoachOS.Application.OrganizationSettings.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.OrganizationSettings;

public interface IOrganizationSettingsService
{
    /// <summary>
    /// Haalt de settings voor de gegeven org op. Maakt een rij aan met defaults
    /// als die nog niet bestaat (lazy provisioning) zodat de FE altijd een payload terugkrijgt.
    /// </summary>
    Task<Result<OrganizationSettingsDto>> GetAsync(Guid organizationId, CancellationToken ct = default);

    Task<Result<OrganizationSettingsDto>> UpdateAsync(
        Guid organizationId,
        UpdateOrganizationSettingsRequest request,
        CancellationToken ct = default);
}
