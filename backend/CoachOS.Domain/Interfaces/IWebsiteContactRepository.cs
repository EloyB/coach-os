using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IWebsiteContactRepository
{
    Task AddAsync(WebsiteContactEntry entry, CancellationToken ct);
    Task<int> CleanupAsync(DateTime now, CancellationToken ct);
}
