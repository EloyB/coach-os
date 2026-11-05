using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public sealed class WebsiteContactRepository(ApplicationDbContext db) : IWebsiteContactRepository
{
    public async Task AddAsync(WebsiteContactEntry entry, CancellationToken ct) => await db.WebsiteContactEntries.AddAsync(entry, ct);
    public Task<int> CleanupAsync(DateTime now, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync($"""
        DELETE FROM "WebsiteContactEntries"
        WHERE ("CreatedAt" AT TIME ZONE 'UTC') + INTERVAL '12 months' <= ({now} AT TIME ZONE 'UTC')
        """, ct);
}
