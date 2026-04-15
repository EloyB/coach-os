using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class MagicLinkTokenRepository(ApplicationDbContext context) : IMagicLinkTokenRepository
{
    public async Task<MagicLinkToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => await context.MagicLinkTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(MagicLinkToken token, CancellationToken ct = default)
        => await context.MagicLinkTokens.AddAsync(token, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);

    public async Task<MagicLinkToken?> TryConsumeAsync(string tokenHash, DateTime now, CancellationToken ct = default)
    {
        // Atomic claim via conditional UPDATE. PostgreSQL lockt de row tijdens UPDATE —
        // twee parallelle requests zien er exact één met affected=1, de andere 0.
        var affected = await context.MagicLinkTokens
            .Where(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);

        if (affected == 0)
            return null;

        // Tracked entities kunnen nu stale zijn; lees vers terug zonder tracking.
        return await context.MagicLinkTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
    }
}
