using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public sealed class WebsiteWaitlistRepository(ApplicationDbContext db) : IWebsiteWaitlistRepository
{
    public Task<bool> ExistsAsync(string email, CancellationToken ct) =>
        db.WebsiteWaitlistEntries.AsNoTracking().AnyAsync(x => x.Email == email, ct);

    public async Task<WebsiteWaitlistEntry?> FindForMailAsync(Guid entryId, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({Email.WebsiteWaitlistMailWorker.LockId})", ct);
        return await db.WebsiteWaitlistEntries.SingleOrDefaultAsync(x => x.Id == entryId, ct);
    }

    public Task<WebsiteWaitlistMail?> FindMailAsync(Guid entryId, CancellationToken ct) =>
        db.WebsiteWaitlistMails.SingleOrDefaultAsync(x => x.EntryId == entryId, ct);

    public async Task AddMailAsync(WebsiteWaitlistMail mail, CancellationToken ct) =>
        await db.WebsiteWaitlistMails.AddAsync(mail, ct);

    public async Task AddAsync(WebsiteWaitlistEntry entry, WebsiteWaitlistMail mail, CancellationToken ct)
    {
        await db.WebsiteWaitlistEntries.AddAsync(entry, ct);
        await db.WebsiteWaitlistMails.AddAsync(mail, ct);
    }

    public async Task RemoveByHashAsync(string hash, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({Email.WebsiteWaitlistMailWorker.LockId})", ct);
        IQueryable<Guid> ids = db.WebsiteWaitlistEntries.Where(x => x.UnsubscribeTokenHash == hash).Select(x => x.Id);
        await db.WebsiteWaitlistMails.Where(x => ids.Contains(x.EntryId)).ExecuteDeleteAsync(ct);
        await db.WebsiteWaitlistEntries.Where(x => x.UnsubscribeTokenHash == hash).ExecuteDeleteAsync(ct);
    }
}
