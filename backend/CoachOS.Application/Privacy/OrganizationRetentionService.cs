using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.Privacy;

/// <summary>
/// Verwijdert organisaties 90 dagen na het einde van de proef of het abonnement. Verloop per
/// organisatie:
/// <list type="number">
/// <item>30 dagen voor <see cref="OrganizationRetentionClock.DeletableFrom"/>: verwijderdatum
/// vastleggen (nooit vroeger dan 30 dagen na deze mail) en de eerste mail versturen.</item>
/// <item>7 dagen voor die datum: de laatste mail (ligt de datum al voorbij omdat de job stillag,
/// dan schuift ze op naar 7 dagen na deze mail).</item>
/// <item>Op de datum: verwijderen via <see cref="IOrganizationOffboardingService.DeleteExpiredAsync"/>.</item>
/// </list>
/// Clubs met facturen worden niet verwijderd (wettelijke bewaarplicht): de superadmin krijgt één
/// melding. Kiest een club opnieuw een plan, dan vervalt de planning.
/// </summary>
public class OrganizationRetentionService(
    ISubscriptionRepository subscriptions,
    IOrganizationOffboardingRepository offboardingData,
    IOrganizationOffboardingService offboarding,
    IEmailService email,
    IEmailOrganizationContext mailOrganization,
    IOptions<AppOptions> appOptions,
    IOptions<SuperAdminOptions> superAdminOptions,
    TimeProvider timeProvider,
    ILogger<OrganizationRetentionService> logger) : IOrganizationRetentionService
{
    public const int FirstNoticeDays = 30;
    public const int FinalNoticeDays = 7;

    // Mails vertrekken enkel overdag (Brusselse tijd); de hele run volgt dat venster.
    private const int RunFromHour = 8;
    private const int RunUntilHour = 18;

    private enum Outcome { None, FirstNotice, FinalNotice, Deleted, HandedToSuperAdmin, Cancelled, Failed }

    public async Task<Result<OrganizationRetentionReport>> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        int brusselsHour = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TimeProviderExtensions.BrusselsTimeZone).Hour;
        if (brusselsHour < RunFromHour || brusselsHour >= RunUntilHour)
            return Result<OrganizationRetentionReport>.Ok(OrganizationRetentionReport.Empty);

        DateOnly today = timeProvider.GetBrusselsToday();
        IReadOnlyList<Subscription> candidates = await subscriptions.ListForRetentionReadOnlyAsync(ct);

        List<Outcome> outcomes = [];
        foreach (Subscription subscription in candidates)
            outcomes.Add(await ProcessSafelyAsync(subscription, today, dryRun, ct));

        return Result<OrganizationRetentionReport>.Ok(new OrganizationRetentionReport(
            outcomes.Count(o => o == Outcome.FirstNotice),
            outcomes.Count(o => o == Outcome.FinalNotice),
            outcomes.Count(o => o == Outcome.Deleted),
            outcomes.Count(o => o == Outcome.HandedToSuperAdmin),
            outcomes.Count(o => o == Outcome.Cancelled),
            outcomes.Count(o => o == Outcome.Failed)));
    }

    private async Task<Outcome> ProcessSafelyAsync(Subscription subscription, DateOnly today, bool dryRun, CancellationToken ct)
    {
        try
        {
            return await ProcessAsync(subscription, today, dryRun, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Geen exceptiontekst loggen: databasefouten kunnen persoonsgegevens bevatten.
            logger.LogError("Organisatieretentie mislukt voor organisatie {OrganizationId} ({ErrorType})",
                subscription.OrganizationId, ex.GetType().Name);
            return Outcome.Failed;
        }
    }

    private async Task<Outcome> ProcessAsync(Subscription subscription, DateOnly today, bool dryRun, CancellationToken ct)
    {
        DateOnly? deletableFrom = OrganizationRetentionClock.DeletableFrom(subscription);

        if (subscription.ScheduledDeletionOn is not DateOnly scheduledOn)
            return await StartAsync(subscription, deletableFrom, today, dryRun, ct);

        // De club ging verder (nieuw plan, verlengde proef): de geplande datum klopt niet meer.
        if (deletableFrom is null || deletableFrom.Value > scheduledOn || subscription.DeletionStage is null)
            return await CancelAsync(subscription, dryRun, ct);

        bool due = subscription.DeletionStage switch
        {
            OrganizationDeletionStage.FirstNotice => today >= scheduledOn.AddDays(-FinalNoticeDays),
            OrganizationDeletionStage.FinalNotice => today >= scheduledOn,
            _ => false
        };
        if (!due)
            return Outcome.None;

        // Sinds de eerste mail een betaling gestart (bv. een afgebroken checkout): de offboarding
        // weigert dan, dus niet verder aftellen maar de superadmin verwittigen.
        if (await HasSubscriptionPaymentsAsync(subscription, ct))
            return await HandToSuperAdminAsync(subscription, scheduledOn, dryRun, ct);

        return subscription.DeletionStage == OrganizationDeletionStage.FirstNotice
            ? await SendFinalNoticeAsync(subscription, scheduledOn, today, dryRun, ct)
            : await DeleteAsync(subscription, scheduledOn, dryRun, ct);
    }

    private async Task<bool> HasSubscriptionPaymentsAsync(Subscription subscription, CancellationToken ct)
        => (await offboardingData.CountDataAsync(subscription.OrganizationId, ct)).SubscriptionPayments > 0;

    private async Task<Outcome> StartAsync(
        Subscription subscription, DateOnly? deletableFrom, DateOnly today, bool dryRun, CancellationToken ct)
    {
        if (deletableFrom is not DateOnly from || today < from.AddDays(-FirstNoticeDays))
            return Outcome.None;

        if (await HasSubscriptionPaymentsAsync(subscription, ct))
        {
            return today >= from
                ? await HandToSuperAdminAsync(subscription, from, dryRun, ct)
                : Outcome.None;
        }

        // Nooit verwijderen zonder dat de club minstens 30 dagen vooraf verwittigd is.
        DateOnly deletionOn = Max(from, today.AddDays(FirstNoticeDays));
        bool sent = await ClaimAndNotifyAsync(subscription, deletionOn, OrganizationDeletionStage.FirstNotice,
            finalNotice: false, dryRun, ct);
        return sent ? Outcome.FirstNotice : Outcome.None;
    }

    private async Task<Outcome> SendFinalNoticeAsync(
        Subscription subscription, DateOnly scheduledOn, DateOnly today, bool dryRun, CancellationToken ct)
    {
        // Lag de job stil, dan schuift de datum op: de laatste mail komt altijd 7 dagen vooraf.
        DateOnly deletionOn = Max(scheduledOn, today.AddDays(FinalNoticeDays));
        bool sent = await ClaimAndNotifyAsync(subscription, deletionOn, OrganizationDeletionStage.FinalNotice,
            finalNotice: true, dryRun, ct);
        return sent ? Outcome.FinalNotice : Outcome.None;
    }

    private async Task<bool> ClaimAndNotifyAsync(
        Subscription subscription, DateOnly deletionOn, OrganizationDeletionStage stage, bool finalNotice,
        bool dryRun, CancellationToken ct)
    {
        if (dryRun) return true;

        // Eerst claimen, dan versturen: bij meerdere API-instanties wint er precies één.
        if (!await ClaimAsync(subscription, deletionOn, stage, ct))
            return false;

        // De mail hoort bij de club: zo ruimt de offboarding haar mee op (geen adres achterlaten).
        mailOrganization.Use(subscription.OrganizationId);
        try
        {
            await email.SendOrganizationDeletionNoticeAsync(
                subscription.Organization.Email,
                subscription.Organization.Name,
                deletionOn,
                finalNotice,
                $"{appOptions.Value.FrontendBaseUrl.TrimEnd('/')}/dashboard/settings",
                ct);
        }
        catch
        {
            // Claim terugdraaien zodat de volgende run het opnieuw probeert.
            await RevertClaimAsync(subscription, deletionOn, stage);
            throw;
        }
        finally
        {
            mailOrganization.Clear();
        }

        logger.LogInformation(
            "Organisatieretentie: verwijdermail ({Stage}) voor organisatie {OrganizationId}, verwijdering op {DeletionOn:yyyy-MM-dd}",
            stage, subscription.OrganizationId, deletionOn);
        return true;
    }

    private async Task<Outcome> HandToSuperAdminAsync(
        Subscription subscription, DateOnly deletableFrom, bool dryRun, CancellationToken ct)
    {
        if (dryRun) return Outcome.HandedToSuperAdmin;

        string to = superAdminOptions.Value.Email;
        if (string.IsNullOrWhiteSpace(to))
        {
            // Niet claimen: zodra het adres ingesteld is, volgt de melding alsnog.
            logger.LogWarning(
                "Organisatieretentie: organisatie {OrganizationId} heeft facturen en wordt niet automatisch verwijderd; geen superadmin-adres geconfigureerd",
                subscription.OrganizationId);
            return Outcome.None;
        }

        if (!await ClaimAsync(subscription, deletableFrom, OrganizationDeletionStage.HandedToSuperAdmin, ct))
            return Outcome.None;

        try
        {
            await email.SendBillingSuperAdminNoticeAsync(
                to,
                "Organisatie handmatig verwijderen",
                $"De bewaartermijn van {subscription.Organization.Name} is verstreken op {deletableFrom:dd/MM/yyyy}, " +
                "maar de club heeft facturen met wettelijke bewaarplicht. De automatische verwijdering slaat haar over: handel dit manueel af.",
                $"{appOptions.Value.FrontendBaseUrl.TrimEnd('/')}/super-admin",
                ct);
        }
        catch
        {
            await RevertClaimAsync(subscription, deletableFrom, OrganizationDeletionStage.HandedToSuperAdmin);
            throw;
        }

        logger.LogInformation(
            "Organisatieretentie: organisatie {OrganizationId} heeft facturen, gemeld aan de superadmin",
            subscription.OrganizationId);
        return Outcome.HandedToSuperAdmin;
    }

    private async Task<Outcome> DeleteAsync(
        Subscription subscription, DateOnly scheduledOn, bool dryRun, CancellationToken ct)
    {
        if (dryRun) return Outcome.Deleted;

        Result<OrganizationDeletedDto> result =
            await offboarding.DeleteExpiredAsync(subscription.OrganizationId, scheduledOn, ct);
        if (result.IsSuccess)
            return Outcome.Deleted;

        logger.LogError(
            "Organisatieretentie: verwijdering van organisatie {OrganizationId} geweigerd ({ErrorCodes})",
            subscription.OrganizationId, string.Join(", ", result.Errors.Select(e => e.Code)));
        return Outcome.Failed;
    }

    private async Task<Outcome> CancelAsync(Subscription subscription, bool dryRun, CancellationToken ct)
    {
        if (dryRun) return Outcome.Cancelled;

        bool cancelled = await subscriptions.TrySetDeletionScheduleAsync(
            subscription.Id, subscription.ScheduledDeletionOn, subscription.DeletionStage, null, null, ct);
        if (cancelled)
        {
            logger.LogInformation("Organisatieretentie: geplande verwijdering van organisatie {OrganizationId} vervallen",
                subscription.OrganizationId);
        }
        return cancelled ? Outcome.Cancelled : Outcome.None;
    }

    private Task<bool> ClaimAsync(
        Subscription subscription, DateOnly deletionOn, OrganizationDeletionStage stage, CancellationToken ct)
        => subscriptions.TrySetDeletionScheduleAsync(
            subscription.Id, subscription.ScheduledDeletionOn, subscription.DeletionStage, deletionOn, stage, ct);

    /// <summary>
    /// Draait een claim terug na een mislukte mail, zonder het (mogelijk geannuleerde) token.
    /// Mislukt ook dat, dan staat de stap als verstuurd zonder mail: luid loggen, want een
    /// gemiste laatste mail moet manueel opgevolgd worden.
    /// </summary>
    private async Task RevertClaimAsync(Subscription subscription, DateOnly deletionOn, OrganizationDeletionStage stage)
    {
        try
        {
            await subscriptions.TrySetDeletionScheduleAsync(
                subscription.Id, deletionOn, stage, subscription.ScheduledDeletionOn, subscription.DeletionStage,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogCritical(
                "Organisatieretentie: mail ({Stage}) voor organisatie {OrganizationId} mislukt en de planning kon niet teruggezet worden ({ErrorType}); zet ScheduledDeletionOn/DeletionStage manueel terug",
                stage, subscription.OrganizationId, ex.GetType().Name);
        }
    }

    private static DateOnly Max(DateOnly first, DateOnly second) => first > second ? first : second;
}
