using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public sealed class WebsiteContactRepository(ApplicationDbContext db) : IWebsiteContactRepository
{
    public async Task AddAsync(WebsiteContactEntry entry, CancellationToken ct = default)
        => await db.WebsiteContactEntries.AddAsync(entry, ct);

    // Eenvoudige vergelijking op de kolom zelf, zodat de CreatedAt-index bruikbaar blijft.
    public Task<int> DeleteReceivedOnOrBeforeAsync(DateTime cutoffUtc, CancellationToken ct = default)
        => db.WebsiteContactEntries.Where(x => x.CreatedAt <= cutoffUtc).ExecuteDeleteAsync(ct);
}
