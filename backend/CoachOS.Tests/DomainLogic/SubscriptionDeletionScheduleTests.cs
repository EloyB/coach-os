using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using AwesomeAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.DomainLogic;

[TestFixture]
public class SubscriptionDeletionScheduleTests
{
    private static readonly DateOnly Today = new(2026, 11, 10);

    [Test]
    public void ActivatePaidPlan_ClearsScheduledDeletion()
    {
        Subscription subscription = Subscription.StartTrial(Guid.NewGuid(), Today.AddDays(-150));
        subscription.ScheduledDeletionOn = Today.AddDays(5);
        subscription.DeletionStage = OrganizationDeletionStage.FinalNotice;

        subscription.ActivatePaidPlan(SubscriptionPlan.Set, BillingInterval.Yearly, BillingPaymentMethod.Mollie,
            Today, Today.AddYears(1).AddDays(-1), 100m);

        subscription.ScheduledDeletionOn.Should().BeNull();
        subscription.DeletionStage.Should().BeNull();
    }

    [Test]
    public void ClearDeletionSchedule_ClearsDateAndStage()
    {
        Subscription subscription = Subscription.StartTrial(Guid.NewGuid(), Today.AddDays(-150));
        subscription.ScheduledDeletionOn = Today;
        subscription.DeletionStage = OrganizationDeletionStage.HandedToSuperAdmin;

        subscription.ClearDeletionSchedule();

        subscription.ScheduledDeletionOn.Should().BeNull();
        subscription.DeletionStage.Should().BeNull();
    }

    [Test]
    public void StartTrial_HasNoScheduledDeletion()
    {
        Subscription subscription = Subscription.StartTrial(Guid.NewGuid(), Today);

        subscription.ScheduledDeletionOn.Should().BeNull();
        subscription.DeletionStage.Should().BeNull();
    }
}
