using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Application.WebsiteContact.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.WebsiteContact;

public sealed class WebsiteContactService(
    IWebsiteContactRepository repository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IOptions<WebsiteOptions> options,
    TimeProvider clock,
    ILogger<WebsiteContactService> logger) : IWebsiteContactService
{
    // Losgekoppeld van de request: een bezoeker die de pagina sluit, mag de melding niet afbreken.
    private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(20);

    public async Task<Result> SubmitAsync(SubmitWebsiteContactRequest request, CancellationToken ct = default)
    {
        WebsiteContactEntry entry = CreateEntry(request, clock.GetUtcNow().UtcDateTime);
        await repository.AddAsync(entry, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await NotifyAsync(entry);
        return Result.Ok();
    }

    private static WebsiteContactEntry CreateEntry(SubmitWebsiteContactRequest request, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        Email = request.Email.Trim().ToLowerInvariant(),
        Message = request.Message.Trim(),
        Organization = string.IsNullOrWhiteSpace(request.Organization) ? null : request.Organization.Trim(),
        CreatedAt = now,
    };

    /// <summary>Het bericht is al opgeslagen: een mislukte melding wordt gelogd (zonder PII), nooit gegooid.</summary>
    private async Task NotifyAsync(WebsiteContactEntry entry)
    {
        string recipient = options.Value.ContactNotificationEmail;
        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogWarning("Contactbericht {EntryId} opgeslagen zonder melding: Website:ContactNotificationEmail is leeg", entry.Id);
            return;
        }

        try
        {
            using CancellationTokenSource timeout = new(NotificationTimeout);
            await emailService.SendWebsiteContactNotificationAsync(recipient,
                new WebsiteContactNotification(entry.Name, entry.Email, entry.Organization, entry.Message, entry.CreatedAt),
                timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogError("Melding voor contactbericht {EntryId} mislukt ({ErrorType}); bericht staat wel in de database",
                entry.Id, ex.GetType().Name);
        }
    }
}
