using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Subscriptions;

/// <summary>
/// Abonnementsbeheer door de superadmin (cross-org). De club zelf beheert haar abonnement via
/// <c>IBillingService</c>.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>Het abonnement van een organisatie, of null als er nog geen is.</summary>
    Task<Result<SubscriptionDto?>> GetForOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>De vaste plancatalogus (prijs + leerlinglimiet).</summary>
    IReadOnlyList<SubscriptionPlanOptionDto> GetPlanCatalog();
}
