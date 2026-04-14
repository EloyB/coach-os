using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IMagicLinkTokenRepository
{
    Task<MagicLinkToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(MagicLinkToken token, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
