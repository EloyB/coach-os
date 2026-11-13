using CoachOS.Application.Abstractions;
using CoachOS.Domain.Entities;
using CoachOS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CoachOS.Infrastructure.Email;

/// <summary>Zet een mail in de outbox; de <see cref="EmailOutboxWorker"/> verstuurt hem.</summary>
internal interface IEmailOutboxWriter
{
    Task EnqueueAsync(IOutboxEmail mail);
}

/// <summary>
/// Schrijft de outbox-rij in een eigen scope met een eigen DbContext. Zo flusht het
/// inplannen van een mail nooit (half afgewerkte) wijzigingen van de aanroeper, en
/// staat de mail vast zodra deze methode terugkeert — ook als de aanroeper daarna faalt,
/// net zoals een rechtstreeks verstuurde mail vroeger al weg was.
/// </summary>
internal sealed class EmailOutboxWriter(
    IServiceScopeFactory scopeFactory,
    ITenantContext tenant) : IEmailOutboxWriter
{
    public async Task EnqueueAsync(IOutboxEmail mail)
    {
        (string type, string payload) = OutboxEmails.Serialize(mail);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.EmailOutboxMessages.Add(new EmailOutboxMessage
        {
            // Tenant van het verzoek dat de mail veroorzaakte, zodat offboarding van een
            // organisatie ook haar wachtende mails opruimt. Publiek/achtergrond: geen tenant.
            OrganizationId = tenant.OrganizationId == Guid.Empty ? null : tenant.OrganizationId,
            // Geen inschrijving gekend: via het adres vindt "speler wissen" deze mail terug.
            RecipientEmail = mail.Recipient.Trim().ToLowerInvariant(),
            Type = type,
            Payload = payload,
            Status = EmailOutboxStatuses.Pending,
            AvailableAt = DateTime.UtcNow,
        });

        // Bewust niet annuleerbaar: de zakelijke wijziging is op dit punt al opgeslagen.
        // Een afgebroken verzoek (time-out, gebruiker sluit de pagina) mag de mail niet
        // meer doen verdwijnen; dat was net het probleem bij het rechtstreeks versturen.
        await db.SaveChangesAsync(CancellationToken.None);
    }
}
