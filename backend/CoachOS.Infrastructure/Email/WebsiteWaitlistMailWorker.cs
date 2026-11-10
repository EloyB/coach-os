using CoachOS.Application.Abstractions;
using CoachOS.Application.WebsiteContact;
using CoachOS.Application.WebsiteWaitlist;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Email;

/// <summary>
/// Platform-outbox voor de wachtlijstmail (vaste template, geen tenantdata) plus de
/// bewaartermijn van websitecontactberichten. Een transactionele advisory-lock serialiseert
/// bezorging en uitschrijven over API-instanties. SMTP is at-least-once: een crash na
/// acceptatie kan dezelfde link opnieuw versturen.
/// </summary>
public sealed class WebsiteWaitlistMailWorker(
    IServiceScopeFactory scopes, TimeProvider clock, ILogger<WebsiteWaitlistMailWorker> logger) : BackgroundService
{
    internal const long LockId = 264289;
    internal const int MaxAttempts = 8;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ContactCleanupInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTime nextContactCleanup = DateTime.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DateTime now = clock.GetUtcNow().UtcDateTime;
                if (now >= nextContactCleanup)
                {
                    await RunContactCleanupAsync(now, stoppingToken);
                    nextContactCleanup = now.Add(ContactCleanupInterval);
                }
                await RunMailIterationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                // Nooit providerfouttekst, ontvanger, payload of token loggen.
                logger.LogError("Website-achtergrondtaak mislukt ({ErrorType}); werk blijft bewaard", ex.GetType().Name);
            }
            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    private async Task RunContactCleanupAsync(DateTime now, CancellationToken ct)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        IWebsiteContactRepository contacts = scope.ServiceProvider.GetRequiredService<IWebsiteContactRepository>();
        int removed = await contacts.DeleteReceivedOnOrBeforeAsync(WebsiteContactRetention.CutoffFor(now), ct);
        if (removed > 0)
            logger.LogInformation("{Count} verlopen websitecontactberichten verwijderd", removed);
    }

    private async Task RunMailIterationAsync(CancellationToken ct)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        await ProcessNextAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
            scope.ServiceProvider.GetRequiredService<IEmailService>(), clock, logger, ct);
    }

    internal static async Task ProcessNextAsync(ApplicationDbContext db, IEmailService emailService,
        TimeProvider clock, ILogger logger, CancellationToken ct)
    {
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({LockId})", ct);
        DateTime now = clock.GetUtcNow().UtcDateTime;

        int expired = await db.WebsiteWaitlistMails.Where(x => x.ExpiresAt <= now).ExecuteDeleteAsync(ct);
        if (expired > 0)
            logger.LogWarning("{Count} onbezorgde wachtlijstmails verlopen; controle door beheerder nodig", expired);

        if (now >= WebsiteWaitlistService.RetentionDeadline)
        {
            await db.WebsiteWaitlistMails.ExecuteDeleteAsync(ct);
            await db.WebsiteWaitlistEntries.ExecuteDeleteAsync(ct);
            await transaction.CommitAsync(ct);
            return;
        }

        WebsiteWaitlistMail? mail = await db.WebsiteWaitlistMails
            .Where(x => x.Attempts < MaxAttempts && x.AvailableAt <= now)
            .OrderBy(x => x.AvailableAt)
            .FirstOrDefaultAsync(ct);
        if (mail is not null)
        {
            await DeliverAsync(db, emailService, mail, now, logger, ct);
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }

    private static async Task DeliverAsync(ApplicationDbContext db, IEmailService emailService,
        WebsiteWaitlistMail mail, DateTime now, ILogger logger, CancellationToken ct)
    {
        WebsiteWaitlistEntry entry = await db.WebsiteWaitlistEntries.AsNoTracking().SingleAsync(x => x.Id == mail.EntryId, ct);
        mail.Attempts++;
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            await emailService.SendWebsiteWaitlistAsync(entry.Email, mail.Token, timeout.Token);
            // Gevoelige payload meteen weg zodra SMTP de mail aanvaardt.
            db.WebsiteWaitlistMails.Remove(mail);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            mail.AvailableAt = now.AddSeconds(Math.Min(Math.Pow(2, mail.Attempts - 1) * 5, 3600));
            bool exhausted = mail.Attempts >= MaxAttempts;
            if (exhausted) mail.Token = string.Empty;
            logger.LogWarning("Wachtlijstmail {MailId} mislukt, poging {Attempt}; {State}",
                mail.Id, mail.Attempts, exhausted ? "opgegeven, beheerder nodig" : "nieuwe poging gepland");
        }
    }
}
