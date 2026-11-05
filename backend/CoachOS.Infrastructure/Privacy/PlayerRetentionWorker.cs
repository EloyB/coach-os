using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Privacy;

public sealed class PlayerRetentionWorker(
    IServiceScopeFactory scopes,
    IOptions<PlayerRetentionOptions> options,
    ILogger<PlayerRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (Guid organizationId in options.Value.OrganizationIds.Distinct())
                {
                    try
                    {
                        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                        await scope.ServiceProvider.GetRequiredService<PlayerRetentionPreview>()
                            .RunAsync(organizationId, stoppingToken);
                    }
                    catch (Exception error) when (error is not OperationCanceledException)
                    {
                        // Avoid database exception text potentially containing personal data.
                        logger.LogError("Retention dry-run failed for organization {OrganizationId}; no deletion performed", organizationId);
                    }
                }
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
