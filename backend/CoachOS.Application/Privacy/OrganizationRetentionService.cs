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
/// <item>Op de datum, en pas als de laatste mail echt verstuurd is: verwijderen via
/// <see cref="IOrganizationOffboardingService.DeleteExpiredAsync"/>. Mislukte of verdwenen laatste
/// mail: opnieuw versturen met een datum 7 dagen later.</item>
/// </list>
/// Clubs met facturen worden niet verwijderd (wettelijke bewaarplicht): de superadmin krijgt één
/// melding en een club die al een verwijdermail kreeg, hoort dat de verwijdering niet doorgaat.
/// Kiest een club opnieuw een plan, dan vervalt de planning. Een verlopen proef telt niet als
/// einde zolang <see cref="BillingOptions.EnforceTrialExpiry"/> uit staat (volledige toegang).
/// </summary>
public class OrganizationRetentionService(
    ISubscriptionRepository subscriptions,
    IOrganizationOffboardingRepository offboardingData,
    IOrganizationOffboardingService offboarding,
    IOrganizationDeletionNoticeStatus noticeStatus,
    IEmailService email,
    IEmailOrganizationContext mailOrganization,
    IOptions<AppOptions> appOptions,
    IOptions<SuperAdminOptions> superAdminOptions,
    IOptions<BillingOptions> billingOptions,
    TimeProvider timeProvider,
    ILogger<OrganizationRetentionService> logger) : IOrganizationRetentionService
{
    public const int FirstNoticeDays = 30;
    public const int FinalNoticeDays = 7;

    // Mails vertrekken enkel overdag (Brusselse tijd); de hele run volgt dat venster.
    private const int RunFromHour = 8;
    private const int RunUntilHour = 18;

    private enum Outcome
    {
        None, FirstNotice, FinalNotice, Deleted, HandedToSuperAdmin, Cancelled, Failed,
        SkippedOpenTrial, SkippedNoEmail
    }

    public async Task<Result<OrganizationRetentionReport>> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        int brusselsHour = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TimeProviderExtensions.BrusselsTimeZone).Hour;
        if (brusselsHour < RunFromHour || brusselsHour >= RunUntilHour)
            return Result<OrganizationRetentionReport>.Ok(OrganizationRetentionReport.Empty);

        DateOnly today = timeProvider.GetBrusselsToday();
        IReadOnlyList<Subscription> candidates = await subscriptions.ListForRetentionReadOnlyAsync(ct);

        List<(Guid OrganizationId, Outcome Outcome)> outcomes = [];
        foreach (Subscription subscription in candidates)
            outcomes.Add((subscription.OrganizationId, await ProcessSafelyAsync(subscription, today, dryRun, ct)));

        LogSkipped(outcomes);
        return Result<OrganizationRetentionReport>.Ok(new OrganizationRetentionReport(
            Count(outcomes, Outcome.FirstNotice),
            Count(outcomes, Outcome.FinalNotice),
            Count(outcomes, Outcome.Deleted),
            Count(outcomes, Outcome.HandedToSuperAdmin),
            Count(outcomes, Outcome.Cancelled),
            Count(outcomes, Outcome.Failed),
            Count(outcomes, Outcome.SkippedOpenTrial) + Count(outcomes, Outcome.SkippedNoEmail)));
    }

    private static int Count(List<(Guid OrganizationId, Outcome Outcome)> outcomes, Outcome outcome)
        => outcomes.Count(o => o.Outcome == outcome);

    /// <summary>Eén logregel per soort overgeslagen organisaties, in plaats van één per organisatie.</summary>
    private void LogSkipped(List<(Guid OrganizationId, Outcome Outcome)> outcomes)
    {
        int openTrials = Count(outcomes, Outcome.SkippedOpenTrial);
        if (openTrials > 0)
        {
            logger.LogInformation(
                "Organisatieretentie: {Count} verlopen proef(en) overgeslagen: Billing:EnforceTrialExpiry staat uit, dus ze hebben nog volledige toegang",
                openTrials);
        }

        List<Guid> withoutEmail = outcomes.Where(o => o.Outcome == Outcome.SkippedNoEmail).Select(o => o.OrganizationId).ToList();
        if (withoutEmail.Count > 0)
        {
            logger.LogWarning(
                "Organisatieretentie: {Count} organisatie(s) zonder mailadres overgeslagen; vul het adres aan: {OrganizationIds}",
                withoutEmail.Count, string.Join(", ", withoutEmail));
        }
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
        bool openTrial = IsOpenTrial(subscription);
        DateOnly? deletableFrom = openTrial ? null : OrganizationRetentionClock.DeletableFrom(subscription);

        if (subscription.ScheduledDeletionOn is not DateOnly scheduledOn)
        {
            if (openTrial)
                return OrganizationRetentionClock.EndedOn(subscription) < today ? Outcome.SkippedOpenTrial : Outcome.None;
            return await StartAsync(subscription, deletableFrom, today, dryRun, ct);
        }

        // De club ging verder (nieuw plan, verlengde proef, proef weer volledig toegankelijk):
        // de geplande datum klopt niet meer.
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
            return await HandToSuperAdminAsync(subscription, scheduledOn, deletableFrom.Value, today, dryRun, ct);

        if (!HasEmail(subscription))
            return Outcome.SkippedNoEmail;

        if (subscription.DeletionStage == OrganizationDeletionStage.FirstNotice)
            return await SendFinalNoticeAsync(subscription, scheduledOn, today, dryRun, ct);

        return await DeleteWhenFinalNoticeDeliveredAsync(subscription, scheduledOn, today, dryRun, ct);
    }

    /// <summary>Proef zonder plan die volledige toegang houdt omdat de proefvervaldag niet afgedwongen wordt.</summary>
    private bool IsOpenTrial(Subscription subscription)
        => subscription.Status == SubscriptionStatus.Trialing
           && subscription.Plan is null
           && !billingOptions.Value.EnforceTrialExpiry;

    private static bool HasEmail(Subscription subscription)
        => !string.IsNullOrWhiteSpace(subscription.Organization.Email);

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
                ? await HandToSuperAdminAsync(subscription, from, from, today, dryRun, ct)
                : Outcome.None;
        }

        if (!HasEmail(subscription))
            return Outcome.SkippedNoEmail;

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

    /// <summary>
    /// Verwijdert enkel als de laatste mail voor exact deze datum verstuurd is. Nog onderweg:
    /// wachten. Mislukt of verdwenen: opnieuw versturen met een datum 7 dagen later.
    /// </summary>
    private async Task<Outcome> DeleteWhenFinalNoticeDeliveredAsync(
        Subscription subscription, DateOnly scheduledOn, DateOnly today, bool dryRun, CancellationToken ct)
    {
        // Na de claim van de laatste mail plant de job voor deze organisatie enkel nog die mail in.
        EmailDeliveryStatus status = subscription.DeletionStageChangedAt is DateTime queuedSince
            ? await noticeStatus.GetAsync(subscription.OrganizationId, queuedSince, ct)
            : EmailDeliveryStatus.Missing;
        switch (status)
        {
            case EmailDeliveryStatus.Sent:
                return await DeleteAsync(subscription, scheduledOn, dryRun, ct);
            case EmailDeliveryStatus.Pending:
                return Outcome.None;
            default:
                logger.LogWarning(
                    "Organisatieretentie: laatste verwijdermail voor organisatie {OrganizationId} niet verstuurd ({Status}); verwijdering uitgesteld en mail opnieuw ingepland",
                    subscription.OrganizationId, status);
                return await SendFinalNoticeAsync(subscription, scheduledOn, today, dryRun, ct);
        }
    }

    private async Task<bool> ClaimAndNotifyAsync(
        Subscription subscription, DateOnly deletionOn, OrganizationDeletionStage stage, bool finalNotice,
        bool dryRun, CancellationToken ct)
    {
        if (dryRun) return true;

        // Eerst claimen, dan versturen: bij meerdere API-instanties wint er precies één.
        if (!await ClaimAsync(subscription, deletionOn, stage, ct))
            return false;

        try
        {
            await SendClubMailAsync(subscription, () => email.SendOrganizationDeletionNoticeAsync(
                subscription.Organization.Email,
                subscription.Organization.Name,
                deletionOn,
                finalNotice,
                $"{FrontendBaseUrl}/dashboard/settings?org={subscription.OrganizationId}",
                ct));
        }
        catch
        {
            // Claim terugdraaien zodat de volgende run het opnieuw probeert.
            await RevertClaimAsync(subscription, deletionOn, stage);
            throw;
        }

        logger.LogInformation(
            "Organisatieretentie: verwijdermail ({Stage}) voor organisatie {OrganizationId}, verwijdering op {DeletionOn:yyyy-MM-dd}",
            stage, subscription.OrganizationId, deletionOn);
        return true;
    }

    /// <summary>
    /// Clubmail: hoort bij de organisatie in de outbox (de offboarding ruimt ze mee op), daarna
    /// de context weer leegmaken zodat een latere platformmail in dezelfde run er niet aan hangt.
    /// </summary>
    private async Task SendClubMailAsync(Subscription subscription, Func<Task> send)
    {
        mailOrganization.Use(subscription.OrganizationId);
        try
        {
            await send();
        }
        finally
        {
            mailOrganization.Clear();
        }
    }

    private async Task<Outcome> HandToSuperAdminAsync(
        Subscription subscription, DateOnly scheduleOn, DateOnly deletableFrom, DateOnly today,
        bool dryRun, CancellationToken ct)
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

        bool clubWasNotified = subscription.DeletionStage is OrganizationDeletionStage.FirstNotice
            or OrganizationDeletionStage.FinalNotice;

        if (!await ClaimAsync(subscription, scheduleOn, OrganizationDeletionStage.HandedToSuperAdmin, ct))
            return Outcome.None;

        try
        {
            string retention = deletableFrom <= today
                ? $"De bewaartermijn van {subscription.Organization.Name} liep af op {deletableFrom:dd/MM/yyyy}"
                : $"De bewaartermijn van {subscription.Organization.Name} loopt af op {deletableFrom:dd/MM/yyyy}";
            string notified = clubWasNotified
                ? " De club kreeg al een verwijdermail en hoort nu dat de verwijdering niet doorgaat."
                : string.Empty;
            await email.SendBillingSuperAdminNoticeAsync(
                to,
                "Organisatie handmatig verwijderen",
                $"{retention}, maar de club heeft facturen met wettelijke bewaarplicht. " +
                $"De automatische verwijdering slaat haar over: handel dit manueel af.{notified}",
                $"{FrontendBaseUrl}/super-admin",
                ct);
        }
        catch
        {
            await RevertClaimAsync(subscription, scheduleOn, OrganizationDeletionStage.HandedToSuperAdmin);
            throw;
        }

        if (clubWasNotified)
            await TellClubDeletionCancelledAsync(subscription, ct);

        logger.LogInformation(
            "Organisatieretentie: organisatie {OrganizationId} heeft facturen, gemeld aan de superadmin",
            subscription.OrganizationId);
        return Outcome.HandedToSuperAdmin;
    }

    /// <summary>
    /// De club kreeg een verwijderdatum die nu niet doorgaat. Mislukt deze mail, dan blijft de
    /// overdracht staan (de superadmin is al verwittigd); enkel loggen.
    /// </summary>
    private async Task TellClubDeletionCancelledAsync(Subscription subscription, CancellationToken ct)
    {
        if (!HasEmail(subscription)) return;
        try
        {
            await SendClubMailAsync(subscription, () => email.SendOrganizationDeletionCancelledAsync(
                subscription.Organization.Email, subscription.Organization.Name, ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                "Organisatieretentie: mail 'verwijdering gaat niet door' voor organisatie {OrganizationId} mislukt ({ErrorType})",
                subscription.OrganizationId, ex.GetType().Name);
        }
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

    private string FrontendBaseUrl => appOptions.Value.FrontendBaseUrl.TrimEnd('/');

    private Task<bool> ClaimAsync(
        Subscription subscription, DateOnly deletionOn, OrganizationDeletionStage stage, CancellationToken ct)
        => subscriptions.TrySetDeletionScheduleAsync(
            subscription.Id, subscription.ScheduledDeletionOn, subscription.DeletionStage, deletionOn, stage, ct);

    /// <summary>
    /// Draait een claim terug na een mislukte mail, zonder het (mogelijk geannuleerde) token.
    /// Mislukt ook dat, dan staat de stap als verstuurd zonder mail: luid loggen. Een gemiste
    /// laatste mail vangt de controle op de bezorgstatus nog op vóór er verwijderd wordt.
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
