using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Subscriptions;

/// <summary>
/// Beheer van CoachOS-abonnementen door de superadmin. Cross-org: alle methodes
/// krijgen de <c>organizationId</c> expliciet mee en worden enkel ontsloten achter
/// de SuperAdmin authorization policy.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>Het abonnement van een organisatie, of <c>null</c> als er nog geen is.</summary>
    Task<Result<SubscriptionDto?>> GetForOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Kent een plan + facturatieperiode toe (maakt aan of werkt bij).</summary>
    Task<Result<SubscriptionDto>> AssignPlanAsync(
        Guid organizationId, AssignSubscriptionRequest request, CancellationToken ct = default);

    /// <summary>(De)activeert het abonnement.</summary>
    Task<Result<SubscriptionDto>> SetActiveAsync(Guid organizationId, bool isActive, CancellationToken ct = default);

    /// <summary>Maakt (of hergebruikt) een Mollie customer en genereert een eenmalige betaallink.</summary>
    Task<Result<SubscriptionDto>> CreatePaymentLinkAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Haalt de actuele Mollie-status van de laatste betaling op.</summary>
    Task<Result<SubscriptionDto>> RefreshPaymentStatusAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Markeert de laatste betaling handmatig als betaald en activeert het abonnement.</summary>
    Task<Result<SubscriptionDto>> MarkPaidAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>De beschikbare plannen (prijzen + limieten) voor de keuzelijst.</summary>
    IReadOnlyList<SubscriptionPlanOptionDto> GetPlanCatalog();
}
