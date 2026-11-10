using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Privacy;

public sealed class PlayerRetentionOptions
{
    public const string Section = "PlayerRetention";

    /// <summary>Standaard uit.</summary>
    public bool Enabled { get; set; }

    /// <summary>Standaard enkel tellen en loggen; pas bij <c>false</c> wordt er gewist.</summary>
    public bool DryRun { get; set; } = true;

    /// <summary>Expliciete opt-in per club.</summary>
    public Guid[] OrganizationIds { get; set; } = [];
}

/// <summary>Dagelijkse bewaartermijn-run voor de opgegeven clubs. Logt enkel aantallen, geen persoonsgegevens.</summary>
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
                    await RunForOrganizationAsync(organizationId, stoppingToken);
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task RunForOrganizationAsync(Guid organizationId, CancellationToken ct)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            IPlayerRetentionService service = scope.ServiceProvider.GetRequiredService<IPlayerRetentionService>();
            bool dryRun = options.Value.DryRun;
            Result<PlayerRetentionReport> result = await service.RunAsync(organizationId, dryRun, ct);
            if (!result.IsSuccess || result.Value is null) return;
            logger.LogInformation(
                "Spelerretentie {OrganizationId}: {Candidates} inschrijvingen, {Due} verwijderbaar, {Erased} gewist (dry-run: {DryRun})",
                organizationId, result.Value.Candidates, result.Value.RetentionDue, result.Value.Erased, dryRun);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Geen exceptiontekst loggen: databasefouten kunnen persoonsgegevens bevatten.
            logger.LogError("Spelerretentie mislukt voor organisatie {OrganizationId} ({ErrorType})",
                organizationId, ex.GetType().Name);
        }
    }
}
