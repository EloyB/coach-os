using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Privacy;

/// <summary>
/// Offboarding: preview en definitieve verwijdering van een organisatie, manueel door de
/// superadmin of automatisch na de bewaartermijn. Cross-org — enkel bereikbaar via endpoints
/// achter de SuperAdmin-policy of vanuit de retentiejob.
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

    /// <summary>
    /// Automatische verwijdering na de bewaartermijn (systeemjob). Zelfde transactie en
    /// blokkeringen als <see cref="DeleteAsync"/>, maar in plaats van een bevestigingsnaam moet
    /// de club de laatste mail voor exact <paramref name="scheduledOn"/> gekregen hebben en moet
    /// die datum bereikt zijn. Wordt onder de lock opnieuw gecontroleerd.
    /// </summary>
    Task<Result<OrganizationDeletedDto>> DeleteExpiredAsync(
        Guid organizationId, DateOnly scheduledOn, CancellationToken ct = default);
}
