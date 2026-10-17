using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Subscriptions;

/// <summary>
/// Abonnementsbeheer door de superadmin (cross-org). De club zelf beheert haar abonnement via
/// <c>IBillingService</c>.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>Het abonnement van een organisatie met facturatiegegevens en betalingen, of null.</summary>
    Task<Result<SubscriptionDto?>> GetForOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>De vaste plancatalogus (prijs + leerlinglimiet).</summary>
    IReadOnlyList<SubscriptionPlanOptionDto> GetPlanCatalog();

    /// <summary>
    /// Deal: plan en prijs (ook 0) tot een vaste einddatum, zonder automatische verlenging. Bij een
    /// prijs boven 0 volgt een factuur met overschrijving.
    /// </summary>
    Task<Result<SubscriptionDto>> AssignDealAsync(Guid organizationId, AssignDealRequest request, CancellationToken ct = default);

    /// <summary>Openstaande betaling per overschrijving manueel als betaald markeren.</summary>
    Task<Result<SubscriptionDto>> MarkPaymentPaidAsync(Guid organizationId, Guid paymentId, CancellationToken ct = default);

    /// <summary>Opzeggen namens de club (zelfde regels als in de app).</summary>
    Task<Result<SubscriptionDto>> CancelAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Mailt de beheerders van een legacy-klant een uitnodiging om een abonnement te kiezen.</summary>
    Task<Result> SendPaymentInvitationAsync(Guid organizationId, CancellationToken ct = default);
}
