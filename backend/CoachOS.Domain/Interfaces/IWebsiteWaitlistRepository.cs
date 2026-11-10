using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

/// <summary>Only platform website waitlist data. No arbitrary tenant or mail access.</summary>
public interface IWebsiteWaitlistRepository
{
    Task<bool> ExistsAsync(string email, CancellationToken ct);
    Task AddAsync(WebsiteWaitlistEntry entry, WebsiteWaitlistMail mail, CancellationToken ct);
    /// <summary>Requires caller transaction; serializes with delivery and deletes only matching capability.</summary>
    Task RemoveByHashAsync(string hash, CancellationToken ct);
    Task<WebsiteWaitlistEntry?> FindForMailAsync(Guid entryId, CancellationToken ct);
    Task<WebsiteWaitlistMail?> FindMailAsync(Guid entryId, CancellationToken ct);
    Task AddMailAsync(WebsiteWaitlistMail mail, CancellationToken ct);
}
