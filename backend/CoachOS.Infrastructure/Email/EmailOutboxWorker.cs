using CoachOS.Application.Abstractions;
using System.Text.Json;
using CoachOS.Domain.Entities;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Email;

/// <summary>
/// Delivers durable email outbox messages outside the HTTP request. Claiming uses a
/// short transaction so multiple API instances do not send the same message concurrently.
/// Failed messages are retried with backoff and eventually retained as failed for inspection.
/// </summary>
public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    // Met de backoff hieronder blijft een mail zo'n 2,5 uur proberen voor hij als mislukt
    // blijft staan: genoeg om een storing bij de mailprovider te overbruggen.
    private const int MaxAttempts = 12;
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);

    // Zolang een bericht in behandeling is, mag geen andere instantie het oppikken. Crasht
    // de worker midden in het versturen, dan komt het na deze termijn opnieuw aan de beurt.
    private static readonly TimeSpan ProcessingLease = TimeSpan.FromMinutes(5);

    // Na het versturen is de inhoud (adres, naam, inloglinks, codes) niet meer nodig:
    // gewist, zodat er geen persoonsgegevens of geheimen in de database achterblijven.
    internal const string ScrubbedPayload = "{}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                bool processed = await ProcessNextAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email outbox worker iteration failed");
                await Task.Delay(IdleDelay, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // De echte SMTP-service, niet IEmailService: die zet mails net in de outbox.
        var smtp = scope.ServiceProvider.GetRequiredService<EmailService>();

        EmailOutboxMessage? message = await ClaimNextAsync(db, ct);
        if (message is null)
            return false;

        try
        {
            await DispatchAsync(message.Type, message.Payload, smtp, ct);
            message.Status = EmailOutboxStatuses.Completed;
            message.ProcessedAt = DateTime.UtcNow;
            message.LastError = null;
            message.Payload = ScrubbedPayload;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            message.Status = message.Attempts >= MaxAttempts
                ? EmailOutboxStatuses.Failed
                : EmailOutboxStatuses.Pending;
            message.AvailableAt = DateTime.UtcNow.Add(GetRetryDelay(message.Attempts));
            message.LastError = ex.Message[..Math.Min(ex.Message.Length, 2000)];
            logger.LogError(ex,
                "Email outbox message {MessageId} ({Type}) failed on attempt {Attempt}; status {Status}",
                message.Id, message.Type, message.Attempts, message.Status);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task<EmailOutboxMessage?> ClaimNextAsync(
        ApplicationDbContext db, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        DateTime now = DateTime.UtcNow;
        EmailOutboxMessage? message = await db.EmailOutboxMessages
            .FromSqlRaw("""
                SELECT * FROM "EmailOutboxMessages"
                WHERE ("Status" = {0} OR "Status" = {1})
                  AND "AvailableAt" <= {2}
                ORDER BY "AvailableAt", "CreatedAt"
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """, EmailOutboxStatuses.Pending, EmailOutboxStatuses.Processing, now)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ct);

        if (message is null)
        {
            await transaction.CommitAsync(ct);
            return null;
        }

        message.Status = EmailOutboxStatuses.Processing;
        message.Attempts++;
        // Lease: een bericht in behandeling is pas na deze termijn opnieuw te claimen.
        // Voorheen kwam het meteen weer in aanmerking (AvailableAt = nu), waardoor een
        // tweede instantie dezelfde mail tegelijk kon versturen.
        message.AvailableAt = now.Add(ProcessingLease);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return message;
    }

    /// <summary>
    /// Verstuurt één outbox-bericht via <paramref name="sender"/>: de mails die de
    /// <see cref="OutboxEmailService"/> inplande, en de inschrijvingsmails die
    /// <c>EnrollmentService</c> in de eigen transactie aanmaakt.
    /// </summary>
    internal static async Task DispatchAsync(
        string type, string payload, IEmailService sender, CancellationToken ct)
    {
        if (OutboxEmails.TryDeserialize(type, payload, out IOutboxEmail? mail))
        {
            await mail!.SendAsync(sender, ct);
            return;
        }

        switch (type)
        {
            case EmailOutboxMessageTypes.EnrollmentConfirmation:
            {
                var data = JsonSerializer.Deserialize<EnrollmentConfirmationEmailPayload>(payload)
                    ?? throw new InvalidOperationException("Invalid enrollment confirmation payload");
                await sender.SendEnrollmentConfirmationAsync(
                    data.Email, data.Name, data.SeriesName,
                    data.ParticipantNames, ct);
                break;
            }
            case EmailOutboxMessageTypes.GroupMemberAdded:
            {
                var data = JsonSerializer.Deserialize<GroupMemberAddedEmailPayload>(payload)
                    ?? throw new InvalidOperationException("Invalid group member added payload");
                await sender.SendGroupMemberAddedAsync(
                    data.Email, data.Name, data.SeriesName, data.GroupName, ct);
                break;
            }
            case EmailOutboxMessageTypes.TrainerNotification:
            {
                var data = JsonSerializer.Deserialize<TrainerEnrollmentNotificationEmailPayload>(payload)
                    ?? throw new InvalidOperationException("Invalid trainer notification payload");
                await sender.SendEnrollmentNotificationToTrainerAsync(
                    data.TrainerEmail, data.TrainerName, data.StudentName,
                    data.StudentEmail, data.SeriesName,
                    data.Responses.Select(r => (r.FieldLabel, r.Value)).ToList(), ct);
                break;
            }
            default:
                throw new InvalidOperationException($"Unknown email outbox message type '{type}'");
        }
    }

    private static TimeSpan GetRetryDelay(int attempts)
        => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, Math.Max(0, attempts - 1)) * 5, 3600));
}
