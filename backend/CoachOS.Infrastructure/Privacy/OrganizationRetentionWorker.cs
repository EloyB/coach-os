using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Privacy;

public sealed class OrganizationRetentionOptions
{
    public const string Section = "OrganizationRetention";

    /// <summary>Automatische verwijdering van organisaties na de bewaartermijn. Standaard uit.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Standaard enkel tellen en loggen; pas bij <c>false</c> worden mails verstuurd, verwijderingen
    /// gepland en organisaties verwijderd.
    /// </summary>
    public bool DryRun { get; set; } = true;
}

/// <summary>
/// Laat elk uur <see cref="IOrganizationRetentionService"/> nagaan welke organisaties een
/// verwijdermail krijgen of verwijderd worden. De service is idempotent, dus een herstart of een
/// tweede API-instantie doet niets dubbel. Logt enkel aantallen, nooit persoonsgegevens.
/// </summary>
public sealed class OrganizationRetentionWorker(
    IServiceScopeFactory scopes,
    IOptions<OrganizationRetentionOptions> options,
    ILogger<OrganizationRetentionWorker> logger) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OrganizationRetentionOptions settings = options.Value;
        if (!settings.Enabled) return;
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(settings.DryRun, stoppingToken);
                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task RunOnceAsync(bool dryRun, CancellationToken ct)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            IOrganizationRetentionService service = scope.ServiceProvider.GetRequiredService<IOrganizationRetentionService>();
            Result<OrganizationRetentionReport> result = await service.RunAsync(dryRun, ct);
            if (!result.IsSuccess || result.Value is null || !result.Value.HasActivity) return;
            OrganizationRetentionReport report = result.Value;
            logger.LogInformation(
                "Organisatieretentie: {FirstNotices} eerste mails, {FinalNotices} laatste mails, {Deleted} verwijderd, " +
                "{HandedToSuperAdmin} aan superadmin gemeld, {Cancelled} vervallen, {Failed} mislukt (dry-run: {DryRun})",
                report.FirstNotices, report.FinalNotices, report.Deleted, report.HandedToSuperAdmin,
                report.Cancelled, report.Failed, dryRun);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Geen exceptiontekst loggen: databasefouten kunnen persoonsgegevens bevatten.
            logger.LogError("Organisatieretentie mislukt ({ErrorType})", ex.GetType().Name);
        }
    }
}
