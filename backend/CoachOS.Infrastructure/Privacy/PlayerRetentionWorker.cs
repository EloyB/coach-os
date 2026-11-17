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

    /// <summary>Bewaartermijn voor spelersdossiers. Standaard uit.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Standaard enkel tellen en loggen; pas bij <c>false</c> wordt er gewist, geanonimiseerd of
    /// opgeruimd. Geldt voor de bewaartermijn én voor de opruiming.
    /// </summary>
    public bool DryRun { get; set; } = true;

    /// <summary>Optionele beperking tot deze clubs. Leeg = alle organisaties.</summary>
    public Guid[] OrganizationIds { get; set; } = [];

    /// <summary>
    /// Opruimen van verstuurde mails en verlopen links (raakt geen spelersdossiers). Standaard aan;
    /// zolang <see cref="DryRun"/> aan staat, wordt er enkel geteld.
    /// </summary>
    public bool CleanupEnabled { get; set; } = true;
}

/// <summary>
/// Dagelijkse run: bewaartermijn per organisatie en de opruiming van technische records.
/// Logt enkel aantallen en organisatie-id's, nooit persoonsgegevens.
/// </summary>
public sealed class PlayerRetentionWorker(
    IServiceScopeFactory scopes,
    IOptions<PlayerRetentionOptions> options,
    ILogger<PlayerRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PlayerRetentionOptions settings = options.Value;
        if (!settings.Enabled && !settings.CleanupEnabled) return;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (settings.Enabled) await RunRetentionAsync(settings, stoppingToken);
                if (settings.CleanupEnabled) await RunCleanupAsync(settings.DryRun, stoppingToken);
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task RunRetentionAsync(PlayerRetentionOptions settings, CancellationToken ct)
    {
        IReadOnlyList<Guid> organizationIds;
        try
        {
            organizationIds = settings.OrganizationIds.Length > 0
                ? settings.OrganizationIds.Distinct().ToList()
                : await ListOrganizationsAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError("Spelerretentie: organisaties ophalen mislukt ({ErrorType})", ex.GetType().Name);
            return;
        }

        foreach (Guid organizationId in organizationIds)
            await RunForOrganizationAsync(organizationId, settings.DryRun, ct);
    }

    private async Task<IReadOnlyList<Guid>> ListOrganizationsAsync(CancellationToken ct)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IPlayerRetentionService>().GetOrganizationIdsAsync(ct);
    }

    private async Task RunForOrganizationAsync(Guid organizationId, bool dryRun, CancellationToken ct)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            IPlayerRetentionService service = scope.ServiceProvider.GetRequiredService<IPlayerRetentionService>();
            Result<PlayerRetentionReport> result = await service.RunAsync(organizationId, dryRun, ct);
            if (!result.IsSuccess || result.Value is null || result.Value.Due == 0) return;
            PlayerRetentionReport report = result.Value;
            logger.LogInformation(
                "Spelerretentie {OrganizationId}: {Candidates} inschrijvingen, termijn verstreken: {DueErase} te wissen, " +
                "{DueAnonymize} te anonimiseren, {Blocked} geblokkeerd; uitgevoerd: {Erased} gewist, {Anonymized} geanonimiseerd (dry-run: {DryRun})",
                organizationId, report.Candidates, report.DueForErasure, report.DueForAnonymization, report.DueButBlocked,
                report.Erased, report.Anonymized, dryRun);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Geen exceptiontekst loggen: databasefouten kunnen persoonsgegevens bevatten.
            logger.LogError("Spelerretentie mislukt voor organisatie {OrganizationId} ({ErrorType})",
                organizationId, ex.GetType().Name);
        }
    }

    private async Task RunCleanupAsync(bool dryRun, CancellationToken ct)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            IPrivacyCleanupService service = scope.ServiceProvider.GetRequiredService<IPrivacyCleanupService>();
            Result<PrivacyCleanupReport> result = await service.RunAsync(dryRun, ct);
            if (!result.IsSuccess || result.Value is null) return;
            PrivacyCleanupReport report = result.Value;
            logger.LogInformation(
                "Privacy-opruiming: {Mails} verstuurde mails, {FailedMails} mislukte mails gewist, {MagicLinks} inloglinks, {Tokens} bevestigingslinks (dry-run: {DryRun})",
                report.CompletedMails, report.ScrubbedFailedMails, report.ExpiredMagicLinks, report.ExpiredConfirmationTokens, dryRun);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError("Privacy-opruiming mislukt ({ErrorType})", ex.GetType().Name);
        }
    }
}
