using CoachOS.Application.Auth;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.DomainLogic;

[TestFixture]
public class SubscriptionStartTrialTests
{
    [Test]
    public void StartTrial_CreatesThirtyDayTrialWithoutPlan()
    {
        Guid orgId = Guid.NewGuid();
        DateOnly today = new(2026, 10, 5);

        Subscription subscription = Subscription.StartTrial(orgId, today);

        subscription.Id.Should().NotBeEmpty();
        subscription.OrganizationId.Should().Be(orgId);
        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
        subscription.StartDate.Should().Be(today);
        subscription.TrialEndsAt.Should().Be(new DateOnly(2026, 11, 4));
        subscription.Plan.Should().BeNull();
        subscription.Price.Should().Be(0);
        subscription.Interval.Should().BeNull();
        subscription.MollieCustomerId.Should().BeNull();
        subscription.LastTrialReminderDay.Should().BeNull();
        subscription.IsActive.Should().BeTrue();
    }
}

[TestFixture]
public class EmailVerificationCodesTests
{
    [Test]
    public void Generate_ReturnsSixDigits()
    {
        for (int i = 0; i < 200; i++)
        {
            string code = EmailVerificationCodes.Generate();

            code.Should().MatchRegex("^[0-9]{6}$");
        }
    }

    [Test]
    public void Matches_CorrectCodeForSameUser_ReturnsTrue()
    {
        Guid userId = Guid.NewGuid();
        string hash = EmailVerificationCodes.Hash(userId, "042917");

        EmailVerificationCodes.Matches(hash, userId, "042917").Should().BeTrue();
    }

    [Test]
    public void Matches_WrongCode_ReturnsFalse()
    {
        Guid userId = Guid.NewGuid();
        string hash = EmailVerificationCodes.Hash(userId, "042917");

        EmailVerificationCodes.Matches(hash, userId, "042918").Should().BeFalse();
    }

    [Test]
    public void Matches_SameCodeForOtherUser_ReturnsFalse()
    {
        string hash = EmailVerificationCodes.Hash(Guid.NewGuid(), "042917");

        EmailVerificationCodes.Matches(hash, Guid.NewGuid(), "042917").Should().BeFalse();
    }

    [Test]
    public void Hash_DoesNotContainTheCode()
    {
        string hash = EmailVerificationCodes.Hash(Guid.NewGuid(), "042917");

        hash.Should().HaveLength(64);
        hash.Should().NotContain("042917");
    }
}
