using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IMagicLinkTokenRepository
{
    Task<MagicLinkToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(MagicLinkToken token, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Atomisch de token consumeren: markeert <c>UsedAt</c> op <paramref name="now"/> als
    /// de token bestaat, nog niet gebruikt is én nog niet verlopen. Bij succes wordt de
    /// geconsumeerde entity teruggegeven; anders null. Voorkomt race-condities waarbij
    /// twee parallelle redeem-requests dezelfde token zouden kunnen inwisselen.
    /// </summary>
    Task<MagicLinkToken?> TryConsumeAsync(string tokenHash, DateTime now, CancellationToken ct = default);
}
