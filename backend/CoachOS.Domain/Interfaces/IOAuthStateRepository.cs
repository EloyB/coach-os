using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IOAuthStateRepository
{
    Task AddAsync(OAuthState state, CancellationToken ct = default);

    /// <summary>Haalt een state op via de random token (tracked, want consumeren = delete).</summary>
    Task<OAuthState?> GetByStateAsync(string state, CancellationToken ct = default);

    Task DeleteAsync(OAuthState state, CancellationToken ct = default);

    /// <summary>Bulk-cleanup van verlopen rijen; geroepen door een hosted service of admin tool.</summary>
    Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
