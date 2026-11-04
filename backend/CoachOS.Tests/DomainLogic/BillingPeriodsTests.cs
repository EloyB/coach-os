using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Subscriptions;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.DomainLogic;

[TestFixture]
public class BillingPeriodsTests
{
    private static readonly DateOnly Today = new(2026, 10, 17);

    [Test]
    public void FirstPeriodStart_TrialRunning_StartsDayAfterTrialEnd()
    {
        Subscription trial = Subscription.StartTrial(Guid.NewGuid(), new DateOnly(2026, 10, 5));

        BillingPeriods.FirstPeriodStart(trial, Today).Should().Be(new DateOnly(2026, 11, 5));
    }

    [Test]
    public void FirstPeriodStart_TrialEndsToday_StartsTomorrow()
    {
        Subscription trial = Subscription.StartTrial(Guid.NewGuid(), Today.AddDays(-Subscription.TrialDays));

        BillingPeriods.FirstPeriodStart(trial, Today).Should().Be(Today.AddDays(1));
    }

    [Test]
    public void FirstPeriodStart_TrialExpired_StartsToday()
    {
        Subscription trial = Subscription.StartTrial(Guid.NewGuid(), new DateOnly(2026, 8, 1));

        BillingPeriods.FirstPeriodStart(trial, Today).Should().Be(Today);
    }

    [TestCase(SubscriptionStatus.Legacy)]
    [TestCase(SubscriptionStatus.Locked)]
    [TestCase(SubscriptionStatus.Cancelled)]
    public void FirstPeriodStart_NotTrialing_StartsToday(SubscriptionStatus status)
    {
        Subscription subscription = new() { Status = status, TrialEndsAt = Today.AddDays(10) };

        BillingPeriods.FirstPeriodStart(subscription, Today).Should().Be(Today);
    }

    [Test]
    public void PeriodEnd_Monthly_IsOneMonthMinusOneDay()
        => BillingPeriods.PeriodEnd(new DateOnly(2026, 11, 5), BillingInterval.Monthly)
            .Should().Be(new DateOnly(2026, 12, 4));

    [Test]
    public void PeriodEnd_Yearly_IsOneYearMinusOneDay()
        => BillingPeriods.PeriodEnd(new DateOnly(2026, 11, 5), BillingInterval.Yearly)
            .Should().Be(new DateOnly(2027, 11, 4));

    [Test]
    public void PeriodEnd_MonthlyFromJan31_EndsEndOfFebruaryMinusOneDay()
        => BillingPeriods.PeriodEnd(new DateOnly(2027, 1, 31), BillingInterval.Monthly)
            .Should().Be(new DateOnly(2027, 2, 27));

    [Test]
    public void MollieInterval_MapsToMollieSyntax()
    {
        BillingPeriods.MollieInterval(BillingInterval.Monthly).Should().Be("1 month");
        BillingPeriods.MollieInterval(BillingInterval.Yearly).Should().Be("12 months");
    }
}
