using System.Text.Json;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
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
    private const int MaxAttempts = 8;
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);

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
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        EmailOutboxMessage? message = await ClaimNextAsync(db, ct);
        if (message is null)
            return false;

        try
        {
            await DispatchAsync(message, emailService, ct);
            message.Status = EmailOutboxStatuses.Completed;
            message.ProcessedAt = DateTime.UtcNow;
            message.LastError = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            message.Status = message.Attempts >= MaxAttempts
                ? EmailOutboxStatuses.Failed
                : EmailOutboxStatuses.Pending;
            message.AvailableAt = DateTime.UtcNow.Add(GetRetryDelay(message.Attempts));
            message.LastError = ex.Message[..Math.Min(ex.Message.Length, 2000)];
            logger.LogError(ex,
                "Email outbox message {MessageId} failed on attempt {Attempt}; status {Status}",
                message.Id, message.Attempts, message.Status);
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
        message.AvailableAt = now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return message;
    }

    private static async Task DispatchAsync(
        EmailOutboxMessage message,
        IEmailService emailService,
        CancellationToken ct)
    {
        switch (message.Type)
        {
            case EmailOutboxMessageTypes.EnrollmentConfirmation:
            {
                var payload = JsonSerializer.Deserialize<EnrollmentConfirmationEmailPayload>(message.Payload)
                    ?? throw new InvalidOperationException("Invalid enrollment confirmation payload");
                await emailService.SendEnrollmentConfirmationAsync(
                    payload.Email, payload.Name, payload.SeriesName, payload.TrainerName,
                    payload.ParticipantNames, ct);
                break;
            }
            case EmailOutboxMessageTypes.TrainerNotification:
            {
                var payload = JsonSerializer.Deserialize<TrainerEnrollmentNotificationEmailPayload>(message.Payload)
                    ?? throw new InvalidOperationException("Invalid trainer notification payload");
                await emailService.SendEnrollmentNotificationToTrainerAsync(
                    payload.TrainerEmail, payload.TrainerName, payload.StudentName,
                    payload.StudentEmail, payload.SeriesName,
                    payload.Responses.Select(r => (r.FieldLabel, r.Value)).ToList(), ct);
                break;
            }
            default:
                throw new InvalidOperationException($"Unknown email outbox message type '{message.Type}'");
        }
    }

    private static TimeSpan GetRetryDelay(int attempts)
        => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, Math.Max(0, attempts - 1)) * 5, 3600));
}
