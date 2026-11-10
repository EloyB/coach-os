using CoachOS.Application.WebsiteWaitlist.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.WebsiteWaitlist;

public interface IWebsiteWaitlistService
{
    /// <summary>Nieuw of dubbel: beide geven Ok. Na het wachtlijsteinde: <see cref="WebsiteWaitlistErrors.Closed"/>.</summary>
    Task<Result> RegisterAsync(RegisterWebsiteWaitlistRequest request, CancellationToken ct = default);

    /// <summary>Idempotent: een onbekende link geeft ook Ok, zodat er niets te raden valt.</summary>
    Task<Result> UnsubscribeAsync(UnsubscribeWebsiteWaitlistRequest request, CancellationToken ct = default);

    /// <summary>Superadmin: standaard dry-run; enkel met <paramref name="apply"/> wordt een mail klaargezet.</summary>
    Task<Result<ManagedWebsiteWaitlistMailResult>> QueueManagedMailAsync(Guid entryId, bool apply, CancellationToken ct = default);
}
