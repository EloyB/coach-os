using CoachOS.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Privacy;

public sealed class OrganizationRetentionWorker(IServiceScopeFactory scopes,
    IOptions<OrganizationRetentionOptions> options, TimeProvider clock,
    ILogger<OrganizationRetentionWorker> logger) : BackgroundService
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
                        await scope.ServiceProvider.GetRequiredService<OrganizationRetentionPreview>()
                            .RunAsync(organizationId, clock.GetBrusselsToday(), stoppingToken);
                        await scope.ServiceProvider.GetRequiredService<OrganizationLifecycleMaintenance>().RunAsync(organizationId, stoppingToken);
                    }
                    catch (Exception error) when (error is not OperationCanceledException)
                    {
                        logger.LogError("Organization retention failed for {OrganizationId}; inspect review holds and delivery state; no completion claimed", organizationId);
                    }
                }
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
