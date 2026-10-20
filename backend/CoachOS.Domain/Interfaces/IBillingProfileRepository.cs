using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IBillingProfileRepository
{
    /// <summary>Facturatieprofiel van een organisatie (cross-org; ook voor jobs en superadmin).</summary>
    Task<BillingProfile?> GetByOrganizationAsync(Guid organizationId, bool tracked = false, CancellationToken ct = default);

    Task AddAsync(BillingProfile profile, CancellationToken ct = default);
}
