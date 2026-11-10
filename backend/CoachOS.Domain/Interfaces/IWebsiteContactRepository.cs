using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

/// <summary>Platformdata van het websitecontactformulier; geen tenantdata.</summary>
public interface IWebsiteContactRepository
{
    Task AddAsync(WebsiteContactEntry entry, CancellationToken ct = default);

    /// <summary>Verwijdert direct (zonder SaveChanges) alle berichten ontvangen op of vóór <paramref name="cutoffUtc"/>.</summary>
    Task<int> DeleteReceivedOnOrBeforeAsync(DateTime cutoffUtc, CancellationToken ct = default);
}
