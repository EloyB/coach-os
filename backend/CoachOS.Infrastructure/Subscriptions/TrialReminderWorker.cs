using CoachOS.Application.Subscriptions;
using CoachOS.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Subscriptions;

/// <summary>
/// Laat elk uur <see cref="ITrialReminderService"/> nagaan welke proef-herinneringen aan de
/// beurt zijn. De service zelf is idempotent, dus een herstart of een tweede API-instantie
/// verstuurt niets dubbel.
/// </summary>
public sealed class TrialReminderWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<TrialReminderWorker> logger) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(stoppingToken);
                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normale shutdown.
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            ITrialReminderService service = scope.ServiceProvider.GetRequiredService<ITrialReminderService>();
            Result<int> result = await service.SendDueRemindersAsync(ct);
            if (result.IsSuccess && result.Value > 0)
                logger.LogInformation("{Count} proef-herinnering(en) verstuurd", result.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Proef-herinneringen versturen mislukt");
        }
    }
}
