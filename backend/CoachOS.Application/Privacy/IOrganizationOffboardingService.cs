using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Privacy;

/// <summary>
/// Manuele offboarding door de superadmin: preview en definitieve verwijdering van een
/// organisatie. Cross-org — enkel bereikbaar via endpoints achter de SuperAdmin-policy.
/// </summary>
public interface IOrganizationOffboardingService
{
    /// <summary>Aantallen, einddatum en blokkeringen. Wijzigt niets en neemt geen locks.</summary>
    Task<Result<OrganizationOffboardingPreviewDto>> GetPreviewAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Verwijdert de organisatie in één transactie als er geen blokkeringen zijn en de
    /// bevestigingsnaam exact overeenkomt.
    /// </summary>
    Task<Result<OrganizationDeletedDto>> DeleteAsync(
        Guid organizationId, DeleteOrganizationRequest request, CancellationToken ct = default);
}
