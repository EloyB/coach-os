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
}
