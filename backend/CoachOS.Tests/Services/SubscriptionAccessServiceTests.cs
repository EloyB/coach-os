using CoachOS.Application.Configuration;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class SubscriptionAccessServiceTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    // 15 oktober 2026, 12:00 Brussels (CEST) → "vandaag" = 15/10/2026.
    private static readonly DateOnly Today = new(2026, 10, 15);
    private static readonly FixedTimeProvider Clock = new(new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero));

    private Mock<ISubscriptionRepository> _repo = null!;

    [SetUp]
    public void SetUp() => _repo = new Mock<ISubscriptionRepository>();

    private SubscriptionAccessService CreateSut(bool enforceTrialExpiry) => new(
        _repo.Object,
        Options.Create(new BillingOptions { EnforceTrialExpiry = enforceTrialExpiry }),
        Clock);

    private void ArrangeSubscription(Subscription? subscription) =>
        _repo.Setup(r => r.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

    private static Subscription Trial(DateOnly trialEndsAt) => new()
    {
        OrganizationId = OrgId,
        Status = SubscriptionStatus.Trialing,
        StartDate = trialEndsAt.AddDays(-Subscription.TrialDays),
        TrialEndsAt = trialEndsAt
    };

    [Test]
    public async Task GetStatusAsync_NoSubscription_ReturnsLegacyWithFullAccess()
    {
        ArrangeSubscription(null);

        Result<SubscriptionStatusDto> result = await CreateSut(enforceTrialExpiry: true).GetStatusAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Legacy");
        result.Value.AccessLevel.Should().Be("Full");
        result.Value.TrialDaysRemaining.Should().BeNull();
        result.Value.IsTrialExpired.Should().BeFalse();
    }

    [Test]
    public async Task GetStatusAsync_LegacySubscription_ReturnsFullAccessEvenWhenEnforcing()
    {
        ArrangeSubscription(new Subscription { OrganizationId = OrgId, Status = SubscriptionStatus.Legacy });

        Result<SubscriptionStatusDto> result = await CreateSut(enforceTrialExpiry: true).GetStatusAsync(OrgId);

        result.Value!.Status.Should().Be("Legacy");
        result.Value.AccessLevel.Should().Be("Full");
        result.Value.TrialEndsAt.Should().BeNull();
    }

    [Test]
    public async Task GetStatusAsync_ActiveSubscription_ReturnsFullAccess()
    {
        ArrangeSubscription(new Subscription { OrganizationId = OrgId, Status = SubscriptionStatus.Active });

        Result<SubscriptionStatusDto> result = await CreateSut(enforceTrialExpiry: true).GetStatusAsync(OrgId);

        result.Value!.Status.Should().Be("Active");
        result.Value.AccessLevel.Should().Be("Full");
    }

    [Test]
    public async Task GetStatusAsync_RunningTrial_ReturnsDaysRemaining()
    {
        ArrangeSubscription(Trial(Today.AddDays(12)));

        Result<SubscriptionStatusDto> result = await CreateSut(enforceTrialExpiry: true).GetStatusAsync(OrgId);

        result.Value!.Status.Should().Be("Trialing");
        result.Value.AccessLevel.Should().Be("Full");
        result.Value.TrialEndsAt.Should().Be(Today.AddDays(12));
        result.Value.TrialDaysRemaining.Should().Be(12);
        result.Value.IsTrialExpired.Should().BeFalse();
    }

    [Test]
    public async Task GetStatusAsync_TrialEndsToday_IsStillFullAccess()
    {
        ArrangeSubscription(Trial(Today));

        Result<SubscriptionStatusDto> result = await CreateSut(enforceTrialExpiry: true).GetStatusAsync(OrgId);

        result.Value!.AccessLevel.Should().Be("Full");
        result.Value.TrialDaysRemaining.Should().Be(0);
        result.Value.IsTrialExpired.Should().BeFalse();
    }

    [Test]
    public async Task GetStatusAsync_ExpiredTrialWithoutEnforcement_KeepsFullAccess()
    {
        ArrangeSubscription(Trial(Today.AddDays(-5)));

        Result<SubscriptionStatusDto> result = await CreateSut(enforceTrialExpiry: false).GetStatusAsync(OrgId);

        result.Value!.AccessLevel.Should().Be("Full");
        result.Value.IsTrialExpired.Should().BeTrue();
        result.Value.TrialDaysRemaining.Should().Be(0);
    }

    [Test]
    public async Task GetStatusAsync_ExpiredTrialWithEnforcement_ReturnsReadOnly()
    {
        ArrangeSubscription(Trial(Today.AddDays(-1)));

        Result<SubscriptionStatusDto> result = await CreateSut(enforceTrialExpiry: true).GetStatusAsync(OrgId);

        result.Value!.AccessLevel.Should().Be("ReadOnly");
        result.Value.IsTrialExpired.Should().BeTrue();
    }

    [Test]
    public async Task GetStatusAsync_Locked_IsReadOnlyEvenWhenEnforcementOff()
    {
        ArrangeSubscription(new Subscription { OrganizationId = OrgId, Status = SubscriptionStatus.Locked, TrialEndsAt = Today.AddDays(-1) });

        Result<SubscriptionStatusDto> result = await CreateSut(enforceTrialExpiry: false).GetStatusAsync(OrgId);

        result.Value!.Status.Should().Be("Locked");
        result.Value.AccessLevel.Should().Be("ReadOnly");
    }

    [Test]
    public async Task GetStatusAsync_Cancelled_IsFullWithCancelAt()
    {
        ArrangeSubscription(new Subscription
        {
            OrganizationId = OrgId,
            Status = SubscriptionStatus.Cancelled,
            CurrentPeriodEnd = Today.AddDays(20),
            CancelAt = Today.AddDays(20),
        });

        Result<SubscriptionStatusDto> result = await CreateSut(enforceTrialExpiry: true).GetStatusAsync(OrgId);

        result.Value!.Status.Should().Be("Cancelled");
        result.Value.AccessLevel.Should().Be("Full");
        result.Value.CancelAt.Should().Be(Today.AddDays(20));
        result.Value.CurrentPeriodEnd.Should().Be(Today.AddDays(20));
    }

    [Test]
    public async Task GetStatusAsync_Active_ReturnsCurrentPeriodEnd()
    {
        ArrangeSubscription(new Subscription { OrganizationId = OrgId, Status = SubscriptionStatus.Active, CurrentPeriodEnd = Today.AddDays(5) });

        Result<SubscriptionStatusDto> result = await CreateSut(enforceTrialExpiry: true).GetStatusAsync(OrgId);

        result.Value!.AccessLevel.Should().Be("Full");
        result.Value.CurrentPeriodEnd.Should().Be(Today.AddDays(5));
        result.Value.CancelAt.Should().BeNull();
    }
}
