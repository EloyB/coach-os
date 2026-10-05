using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.Subscriptions;

public class TrialReminderService(
    ISubscriptionRepository subscriptionRepo,
    IEmailService emailService,
    IOptions<AppOptions> appOptions,
    TimeProvider timeProvider,
    ILogger<TrialReminderService> logger) : ITrialReminderService
{
    // Dag in de proef → resterende dagen waarop die herinnering aan de beurt is.
    private static readonly (int ReminderDay, int MaxDaysRemaining)[] Milestones =
    [
        (30, 0),
        (27, Subscription.TrialDays - 27),
        (20, Subscription.TrialDays - 20),
    ];

    // Herinneringen vertrekken enkel overdag (Brusselse tijd), niet om middernacht.
    private const int SendFromHour = 8;
    private const int SendUntilHour = 18;

    public async Task<Result<int>> SendDueRemindersAsync(CancellationToken ct = default)
    {
        int brusselsHour = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TimeProviderExtensions.BrusselsTimeZone).Hour;
        if (brusselsHour < SendFromHour || brusselsHour >= SendUntilHour)
            return Result<int>.Ok(0);

        DateOnly today = timeProvider.GetBrusselsToday();
        IReadOnlyList<Subscription> trials = await subscriptionRepo.GetAllTrialingReadOnlyAsync(ct);
        int sent = 0;

        foreach (Subscription trial in trials)
        {
            int? dueDay = GetDueReminderDay(trial, today);
            if (dueDay is null)
                continue;

            // Eerst claimen, dan versturen: bij meerdere API-instanties wint er precies één.
            bool claimed = await subscriptionRepo.TrySetLastTrialReminderDayAsync(
                trial.Id, trial.LastTrialReminderDay, dueDay, ct);
            if (!claimed)
                continue;

            try
            {
                int daysRemaining = Math.Max(0, trial.TrialEndsAt!.Value.DayNumber - today.DayNumber);
                await emailService.SendTrialReminderAsync(
                    trial.Organization.Email,
                    trial.Organization.Name,
                    daysRemaining,
                    trial.TrialEndsAt.Value,
                    appOptions.Value.FrontendBaseUrl,
                    ct);
                sent++;
            }
            catch (Exception ex)
            {
                // Claim terugdraaien zodat de volgende run het opnieuw probeert. Ook bij een
                // shutdown midden in het versturen — daarom zonder het (geannuleerde) token.
                await subscriptionRepo.TrySetLastTrialReminderDayAsync(
                    trial.Id, dueDay, trial.LastTrialReminderDay, CancellationToken.None);

                if (ex is OperationCanceledException)
                    throw;

                logger.LogError(ex,
                    "Proef-herinnering dag {ReminderDay} mislukt voor organisatie {OrganizationId}",
                    dueDay, trial.OrganizationId);
            }
        }

        return Result<int>.Ok(sent);
    }

    /// <summary>
    /// De verste herinnering die aan de beurt is en nog niet verstuurd werd. Lag de job een
    /// tijd stil, dan vertrekt enkel de meest recente — geen inhaalreeks van drie mails.
    /// </summary>
    private static int? GetDueReminderDay(Subscription trial, DateOnly today)
    {
        if (trial.TrialEndsAt is null)
            return null;

        int daysRemaining = trial.TrialEndsAt.Value.DayNumber - today.DayNumber;

        foreach ((int reminderDay, int maxDaysRemaining) in Milestones)
        {
            if (daysRemaining <= maxDaysRemaining)
                return trial.LastTrialReminderDay >= reminderDay ? null : reminderDay;
        }

        return null;
    }
}
