using CoachOS.Application.WebsiteContact.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.WebsiteContact;

public interface IWebsiteContactService
{
    /// <summary>Slaat het bericht op en meldt het daarna (best effort) aan het ingestelde adres.</summary>
    Task<Result> SubmitAsync(SubmitWebsiteContactRequest request, CancellationToken ct = default);
}
