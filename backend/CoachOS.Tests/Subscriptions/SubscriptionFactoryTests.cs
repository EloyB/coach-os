using CoachOS.Domain.Enums;
using CoachOS.Domain.Subscriptions;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Subscriptions;

[TestFixture]
public class SubscriptionFactoryTests
{
    private static readonly DateTime Now = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void CreateTrial_SetsTrialingStatusAndTrialEnd()
    {
        var orgId = Guid.NewGuid();

        var sub = SubscriptionFactory.CreateTrial(orgId, trialDays: 60, utcNow: Now);

        sub.OrganizationId.Should().Be(orgId);
        sub.Status.Should().Be(SubscriptionStatus.Trialing);
        sub.TrialEndsAt.Should().Be(Now.AddDays(60));
        sub.CurrentPeriodEnd.Should().BeNull();
        sub.Id.Should().NotBe(Guid.Empty);
    }
}
