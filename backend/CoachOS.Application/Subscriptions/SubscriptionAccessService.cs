using CoachOS.Application.Configuration;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.Subscriptions;

public class SubscriptionAccessService(
    ISubscriptionRepository subscriptionRepo,
    IOptions<BillingOptions> billingOptions,
    TimeProvider timeProvider) : ISubscriptionAccessService
{
    public async Task<Result<SubscriptionStatusDto>> GetStatusAsync(Guid organizationId, CancellationToken ct = default)
    {
        Subscription? subscription = await subscriptionRepo.GetByOrganizationReadOnlyAsync(organizationId, ct);

        // Geen abonnement = organisatie van vóór het abonnementsmodel. Behandelen als legacy
        // zodat een ontbrekende rij nooit een bestaande klant kan blokkeren.
        if (subscription is null || subscription.Status != SubscriptionStatus.Trialing || subscription.TrialEndsAt is null)
        {
            SubscriptionStatus status = subscription?.Status ?? SubscriptionStatus.Legacy;

            // Locked: proef stopgezet na opzegging; altijd enkel-lezen tot er een plan gekozen is.
            SubscriptionAccessLevel level = status == SubscriptionStatus.Locked
                ? SubscriptionAccessLevel.ReadOnly
                : SubscriptionAccessLevel.Full;

            return Result<SubscriptionStatusDto>.Ok(new SubscriptionStatusDto(
                Status: status.ToString(),
                AccessLevel: level.ToString(),
                TrialEndsAt: null,
                TrialDaysRemaining: null,
                IsTrialExpired: false,
                CurrentPeriodEnd: subscription?.CurrentPeriodEnd,
                CancelAt: subscription?.CancelAt));
        }

        DateOnly today = timeProvider.GetBrusselsToday();
        DateOnly trialEndsAt = subscription.TrialEndsAt.Value;
        int daysRemaining = trialEndsAt.DayNumber - today.DayNumber;
        bool isExpired = daysRemaining < 0;

        SubscriptionAccessLevel accessLevel = isExpired && billingOptions.Value.EnforceTrialExpiry
            ? SubscriptionAccessLevel.ReadOnly
            : SubscriptionAccessLevel.Full;

        return Result<SubscriptionStatusDto>.Ok(new SubscriptionStatusDto(
            Status: SubscriptionStatus.Trialing.ToString(),
            AccessLevel: accessLevel.ToString(),
            TrialEndsAt: trialEndsAt,
            TrialDaysRemaining: Math.Max(0, daysRemaining),
            IsTrialExpired: isExpired));
    }
}
