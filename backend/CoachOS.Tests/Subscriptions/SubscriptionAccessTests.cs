using CoachOS.Domain.Enums;
using CoachOS.Domain.Subscriptions;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Subscriptions;

[TestFixture]
public class SubscriptionAccessTests
{
    private static readonly DateTime Now = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Trialing_WithinTrial_HasAccess() =>
        SubscriptionAccess.HasAppAccess(SubscriptionStatus.Trialing, Now.AddDays(5), null, Now)
            .Should().BeTrue();

    [Test]
    public void Trialing_TrialExpired_NoAccess() =>
        SubscriptionAccess.HasAppAccess(SubscriptionStatus.Trialing, Now.AddDays(-1), null, Now)
            .Should().BeFalse();

    [Test]
    public void Active_PeriodNotEnded_HasAccess() =>
        SubscriptionAccess.HasAppAccess(SubscriptionStatus.Active, null, Now.AddDays(10), Now)
            .Should().BeTrue();

    [Test]
    public void Active_PeriodEnded_NoAccess() =>
        SubscriptionAccess.HasAppAccess(SubscriptionStatus.Active, null, Now.AddDays(-1), Now)
            .Should().BeFalse();

    [Test]
    public void PastDue_WithinGracePeriodEnd_HasAccess() =>
        SubscriptionAccess.HasAppAccess(SubscriptionStatus.PastDue, null, Now.AddDays(3), Now)
            .Should().BeTrue();

    [TestCase(SubscriptionStatus.Expired)]
    [TestCase(SubscriptionStatus.Canceled)]
    public void Terminal_NoAccess(SubscriptionStatus status) =>
        SubscriptionAccess.HasAppAccess(status, null, Now.AddDays(10), Now)
            .Should().BeFalse();
}
