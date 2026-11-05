using CoachOS.Application.Abstractions;
using CoachOS.Application.WebsiteWaitlist;
using CoachOS.Domain.Entities;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Email;

/// <summary>
/// Dedicated platform outbox: fixed template, no tenant/enrollment impersonation.
/// Transaction-level advisory lock serializes delivery/removal across API instances.
/// At-least-once SMTP: a crash after SMTP acceptance can redeliver the same link.
/// </summary>
public sealed class WebsiteWaitlistMailWorker(IServiceScopeFactory scopes, ILogger<WebsiteWaitlistMailWorker> logger)
    : BackgroundService
{
    internal const long LockId = 264289;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                await ProcessNextAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                    scope.ServiceProvider.GetRequiredService<IEmailService>(),
                    scope.ServiceProvider.GetRequiredService<TimeProvider>(), logger, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception)
            {
                // Never log provider exception text, recipient, payload, IP or token.
                logger.LogError("Website waitlist delivery iteration failed; durable work retained");
            }
            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    internal static async Task ProcessNextAsync(ApplicationDbContext db, IEmailService emailService,
        TimeProvider clock, ILogger logger, CancellationToken ct)
    {
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({LockId})", ct);
        DateTime now = clock.GetUtcNow().UtcDateTime;
        await new Repositories.WebsiteContactRepository(db).CleanupAsync(now, ct);
        int expired = await db.WebsiteWaitlistMails.Where(x => x.ExpiresAt <= now).ExecuteDeleteAsync(ct);
        if (expired > 0)
            logger.LogWarning("{Count} undelivered website waitlist mail payloads expired; operator review required", expired);
        if (now >= WebsiteWaitlistService.RetentionDeadline)
        {
            await db.WebsiteWaitlistMails.ExecuteDeleteAsync(ct);
            await db.WebsiteWaitlistEntries.ExecuteDeleteAsync(ct);
            await transaction.CommitAsync(ct);
            return;
        }
        WebsiteWaitlistMail? mail = await db.WebsiteWaitlistMails
            .Where(x => x.Attempts < 8 && x.AvailableAt <= now)
            .OrderBy(x => x.AvailableAt).FirstOrDefaultAsync(ct);
        if (mail is not null)
        {
            WebsiteWaitlistEntry entry = await db.WebsiteWaitlistEntries.AsNoTracking().SingleAsync(x => x.Id == mail.EntryId, ct);
            mail.Attempts++;
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            try
            {
                await emailService.SendWebsiteWaitlistAsync(entry.Email, mail.Token, timeout.Token);
                // Delete sensitive payload immediately after acceptance by SMTP.
                db.WebsiteWaitlistMails.Remove(mail);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                mail.AvailableAt = now.AddSeconds(Math.Min(Math.Pow(2, mail.Attempts - 1) * 5, 3600));
                if (mail.Attempts >= 8) mail.Token = string.Empty;
                logger.LogWarning("Website waitlist mail {MailId} failed, attempt {Attempt}; {State}",
                    mail.Id, mail.Attempts, mail.Attempts >= 8 ? "failed; operator required" : "retry pending");
            }
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }
}
