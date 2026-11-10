using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Subscriptions;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

[TestFixture]
public class OrganizationRetentionClockTests
{
    private static readonly DateOnly Today = new(2026, 11, 10);

    private static Subscription Sub(SubscriptionStatus status) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        Status = status,
        StartDate = new DateOnly(2026, 1, 1)
    };

    [Test]
    public void EndedOn_NoSubscription_IsNull()
        => OrganizationRetentionClock.EndedOn(null).Should().BeNull();

    [Test]
    public void EndedOn_Legacy_IsNull()
        => OrganizationRetentionClock.EndedOn(Sub(SubscriptionStatus.Legacy)).Should().BeNull();

    [Test]
    public void EndedOn_ActivePaying_IsNull_EvenWithPastPeriodAndHistoricTrial()
    {
        Subscription s = Sub(SubscriptionStatus.Active);
        s.Plan = SubscriptionPlan.Set;
        s.TrialEndsAt = Today.AddDays(-300);
        s.CurrentPeriodEnd = Today.AddDays(-5); // overschrijving, verlenging nog niet verwerkt

        OrganizationRetentionClock.EndedOn(s).Should().BeNull();
    }

    [Test]
    public void EndedOn_TrialExpiredWithoutPlan_IsTrialEnd()
    {
        Subscription s = Sub(SubscriptionStatus.Trialing);
        s.TrialEndsAt = Today.AddDays(-100);

        OrganizationRetentionClock.EndedOn(s).Should().Be(Today.AddDays(-100));
        OrganizationRetentionClock.DeletableFrom(s).Should().Be(Today.AddDays(-10));
    }

    [Test]
    public void EndedOn_TrialRunning_IsFutureTrialEnd()
    {
        Subscription s = Sub(SubscriptionStatus.Trialing);
        s.TrialEndsAt = Today.AddDays(12);

        OrganizationRetentionClock.EndedOn(s).Should().Be(Today.AddDays(12));
    }

    [Test]
    public void EndedOn_TrialingWithPlan_IsNull()
    {
        Subscription s = Sub(SubscriptionStatus.Trialing);
        s.TrialEndsAt = Today.AddDays(-100);
        s.Plan = SubscriptionPlan.Game;

        OrganizationRetentionClock.EndedOn(s).Should().BeNull();
    }

    [Test]
    public void EndedOn_LockedAfterTrialCancellation_IsLastTrialDay()
    {
        Subscription s = Sub(SubscriptionStatus.Active);
        s.LockAfterTrialCancellation(Today);

        OrganizationRetentionClock.EndedOn(s).Should().Be(Today.AddDays(-1));
    }

    [Test]
    public void EndedOn_CancelledAtPeriodEnd_InFuture_IsPeriodEnd()
    {
        Subscription s = Sub(SubscriptionStatus.Cancelled);
        s.CurrentPeriodEnd = Today.AddDays(40);
        s.CancelAt = Today.AddDays(40);

        OrganizationRetentionClock.EndedOn(s).Should().Be(Today.AddDays(40));
    }

    [Test]
    public void EndedOn_ImmediateCancellation_CancelAtBeforePeriodEnd_IsCancelDay()
    {
        // BillingService: CancelAt = vandaag, CurrentPeriodEnd blijft later staan.
        Subscription s = Sub(SubscriptionStatus.Cancelled);
        s.CurrentPeriodEnd = Today.AddDays(200);
        s.CancelAt = Today;

        OrganizationRetentionClock.EndedOn(s).Should().Be(Today);
    }

    [Test]
    public void EndedOn_CancelledWithoutCancelAt_FallsBackToPeriodEnd()
    {
        Subscription s = Sub(SubscriptionStatus.Cancelled);
        s.CurrentPeriodEnd = Today.AddDays(-3);

        OrganizationRetentionClock.EndedOn(s).Should().Be(Today.AddDays(-3));
    }

    [Test]
    public void EndedOn_CancelledWithoutAnyDate_IsNull()
        => OrganizationRetentionClock.EndedOn(Sub(SubscriptionStatus.Cancelled)).Should().BeNull();

    [Test]
    public void DeletableFrom_IsNinetyDaysAfterEnd()
    {
        Subscription s = Sub(SubscriptionStatus.Cancelled);
        s.CancelAt = new DateOnly(2026, 1, 31);
        s.CurrentPeriodEnd = new DateOnly(2026, 1, 31);

        OrganizationRetentionClock.DeletableFrom(s).Should().Be(new DateOnly(2026, 5, 1));
    }
}
