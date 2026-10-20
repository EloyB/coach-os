using CoachOS.Application.Billing;
using CoachOS.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Billing;

/// <summary>
/// Maakt elke 30 seconden openstaande facturen aan en draait één keer per Brusselse dag de
/// dagelijkse opvolging (verlengingen per overschrijving, deals). De service is idempotent.
/// </summary>
public sealed class BillingWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<BillingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private DateOnly? _lastDailyRun;

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
            IBillingMaintenanceService service = scope.ServiceProvider.GetRequiredService<IBillingMaintenanceService>();

            DateOnly today = timeProvider.GetBrusselsToday();
            if (_lastDailyRun != today)
            {
                await service.RunDailyAsync(today, ct);
                IBillingPaymentSyncService sync = scope.ServiceProvider.GetRequiredService<IBillingPaymentSyncService>();
                int repaired = await sync.ReconcileMissingRenewalsAsync(today, ct);
                if (repaired > 0)
                    logger.LogWarning("{Count} ontbrekende automatische verlenging(en) hersteld", repaired);
                _lastDailyRun = today;
            }

            int created = await service.ProcessPendingInvoicesAsync(ct);
            if (created > 0)
                logger.LogInformation("{Count} factuur/facturen aangemaakt", created);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Billing-job mislukt");
        }
    }
}
