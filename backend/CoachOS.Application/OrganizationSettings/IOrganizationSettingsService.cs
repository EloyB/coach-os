using CoachOS.Application.OrganizationSettings.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.OrganizationSettings;

public interface IOrganizationSettingsService
{
    /// <summary>
    /// Haalt de settings voor de gegeven org op. Maakt een rij aan met defaults
    /// als die nog niet bestaat (lazy provisioning) zodat de FE altijd een payload terugkrijgt.
    /// <paramref name="currentUserId"/> wordt gebruikt om context-velden te berekenen
    /// (zoals openstaande lessen) voor waarschuwingen in de UI.
    /// </summary>
    Task<Result<OrganizationSettingsDto>> GetAsync(
        Guid organizationId,
        Guid currentUserId,
        CancellationToken ct = default);

    Task<Result<OrganizationSettingsDto>> UpdateAsync(
        Guid organizationId,
        Guid currentUserId,
        UpdateOrganizationSettingsRequest request,
        CancellationToken ct = default);
}
