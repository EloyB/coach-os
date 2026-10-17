using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Billing;

/// <summary>Abonnement van de eigen organisatie, voor club-admins (self-service).</summary>
public interface IBillingService
{
    Task<Result<BillingOverviewDto>> GetOverviewAsync(Guid organizationId, CancellationToken ct = default);

    Task<Result<BillingProfileDto>> SaveBillingProfileAsync(
        Guid organizationId, SaveBillingProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Start een abonnement. Mollie: geeft de checkout-URL terug (abonnement wordt actief via
    /// webhook/sync). Overschrijving (enkel jaarlijks): meteen actief, factuur met 30 dagen termijn.
    /// </summary>
    Task<Result<CheckoutResultDto>> StartCheckoutAsync(
        Guid organizationId, CheckoutRequest request, CancellationToken ct = default);

    /// <summary>
    /// Zegt het lopende abonnement op. Loopt de betaalde periode al: stopt aan het einde ervan
    /// (geen terugbetaling). Betaald tijdens de proef, periode nog niet begonnen: terugbetaling,
    /// geen proef meer en het account wordt vergrendeld tot er een plan gekozen wordt.
    /// </summary>
    Task<Result<BillingOverviewDto>> CancelAsync(Guid organizationId, CancellationToken ct = default);
}
