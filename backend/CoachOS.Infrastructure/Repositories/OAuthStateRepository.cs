using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class OAuthStateRepository(ApplicationDbContext context) : IOAuthStateRepository
{
    public async Task AddAsync(OAuthState state, CancellationToken ct = default)
    {
        await context.OAuthStates.AddAsync(state, ct);
    }

    public async Task<OAuthState?> GetByStateAsync(string state, CancellationToken ct = default)
    {
        return await context.OAuthStates
            .FirstOrDefaultAsync(s => s.State == state, ct);
    }

    public Task DeleteAsync(OAuthState state, CancellationToken ct = default)
    {
        context.OAuthStates.Remove(state);
        return Task.CompletedTask;
    }

    public async Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken ct = default)
    {
        return await context.OAuthStates
            .Where(s => s.ExpiresAt < utcNow)
            .ExecuteDeleteAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
