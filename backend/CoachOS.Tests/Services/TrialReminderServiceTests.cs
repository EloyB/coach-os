using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Application.Subscriptions;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class TrialReminderServiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 15);
    private static readonly FixedTimeProvider Clock = new(new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero));

    private Mock<ISubscriptionRepository> _repo = null!;
    private Mock<IEmailService> _email = null!;
    private TrialReminderService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ISubscriptionRepository>();
        _email = new Mock<IEmailService>();

        // Default: elke claim lukt.
        _repo.Setup(r => r.TrySetLastTrialReminderDayAsync(
                It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _sut = new TrialReminderService(
            _repo.Object,
            _email.Object,
            Options.Create(new AppOptions { FrontendBaseUrl = "https://app.coach-os.be" }),
            Clock,
            NullLogger<TrialReminderService>.Instance);
    }

    private static Subscription Trial(int daysRemaining, int? lastReminderDay = null) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        Status = SubscriptionStatus.Trialing,
        TrialEndsAt = Today.AddDays(daysRemaining),
        LastTrialReminderDay = lastReminderDay,
        Organization = new Organization { Name = "TC Brederode", Email = "info@brederode.be" }
    };

    private void ArrangeTrials(params Subscription[] trials) =>
        _repo.Setup(r => r.GetAllTrialingReadOnlyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trials);

    private void VerifyReminderSent(Subscription trial, int daysRemaining, Times times) =>
        _email.Verify(e => e.SendTrialReminderAsync(
            trial.Organization.Email, trial.Organization.Name, daysRemaining, trial.TrialEndsAt!.Value,
            "https://app.coach-os.be", It.IsAny<CancellationToken>()), times);

    [Test]
    public async Task SendDueRemindersAsync_TrialNotYetAtDay20_SendsNothing()
    {
        Subscription trial = Trial(daysRemaining: 11);
        ArrangeTrials(trial);

        Result<int> result = await _sut.SendDueRemindersAsync();

        result.Value.Should().Be(0);
        _email.VerifyNoOtherCalls();
        _repo.Verify(r => r.TrySetLastTrialReminderDayAsync(
            It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(10, 20)]
    [TestCase(3, 27)]
    [TestCase(0, 30)]
    public async Task SendDueRemindersAsync_AtMilestone_SendsReminderAndMarksIt(int daysRemaining, int expectedReminderDay)
    {
        Subscription trial = Trial(daysRemaining);
        ArrangeTrials(trial);

        Result<int> result = await _sut.SendDueRemindersAsync();

        result.Value.Should().Be(1);
        VerifyReminderSent(trial, daysRemaining, Times.Once());
        _repo.Verify(r => r.TrySetLastTrialReminderDayAsync(
            trial.Id, null, expectedReminderDay, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendDueRemindersAsync_ReminderAlreadySent_SendsNothing()
    {
        ArrangeTrials(Trial(daysRemaining: 8, lastReminderDay: 20));

        Result<int> result = await _sut.SendDueRemindersAsync();

        result.Value.Should().Be(0);
        _email.VerifyNoOtherCalls();
    }

    [Test]
    public async Task SendDueRemindersAsync_JobWasDownPastSeveralMilestones_SendsOnlyTheLatest()
    {
        Subscription trial = Trial(daysRemaining: 2);
        ArrangeTrials(trial);

        Result<int> result = await _sut.SendDueRemindersAsync();

        result.Value.Should().Be(1);
        _repo.Verify(r => r.TrySetLastTrialReminderDayAsync(
            trial.Id, null, 27, It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(e => e.SendTrialReminderAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateOnly>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendDueRemindersAsync_ExpiredTrial_SendsFinalReminderWithZeroDays()
    {
        Subscription trial = Trial(daysRemaining: -4, lastReminderDay: 27);
        ArrangeTrials(trial);

        Result<int> result = await _sut.SendDueRemindersAsync();

        result.Value.Should().Be(1);
        VerifyReminderSent(trial, 0, Times.Once());
    }

    [Test]
    public async Task SendDueRemindersAsync_ClaimLostToOtherInstance_DoesNotSend()
    {
        Subscription trial = Trial(daysRemaining: 10);
        ArrangeTrials(trial);
        _repo.Setup(r => r.TrySetLastTrialReminderDayAsync(
                trial.Id, null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Result<int> result = await _sut.SendDueRemindersAsync();

        result.Value.Should().Be(0);
        _email.VerifyNoOtherCalls();
    }

    [Test]
    public async Task SendDueRemindersAsync_EmailFails_RevertsClaimAndContinuesWithNextTrial()
    {
        Subscription failing = Trial(daysRemaining: 10);
        Subscription healthy = Trial(daysRemaining: 3, lastReminderDay: 20);
        ArrangeTrials(failing, healthy);
        _email.Setup(e => e.SendTrialReminderAsync(
                It.IsAny<string>(), It.IsAny<string>(), 10, It.IsAny<DateOnly>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        Result<int> result = await _sut.SendDueRemindersAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        // Claim (null → 20) wordt teruggedraaid (20 → null) zodat de volgende run opnieuw probeert.
        _repo.Verify(r => r.TrySetLastTrialReminderDayAsync(
            failing.Id, 20, null, It.IsAny<CancellationToken>()), Times.Once);
        VerifyReminderSent(healthy, 3, Times.Once());
    }

    [Test]
    public async Task SendDueRemindersAsync_ShutdownDuringSend_RevertsClaimAndRethrows()
    {
        Subscription trial = Trial(daysRemaining: 10);
        ArrangeTrials(trial);
        _email.Setup(e => e.SendTrialReminderAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateOnly>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        Func<Task> act = () => _sut.SendDueRemindersAsync();

        await act.Should().ThrowAsync<OperationCanceledException>();
        _repo.Verify(r => r.TrySetLastTrialReminderDayAsync(
            trial.Id, 20, null, CancellationToken.None), Times.Once);
    }

    // 23:30 UTC = 01:30 Brussels (CEST); 05:30 UTC = 07:30; 16:30 UTC = 18:30.
    [TestCase(23, 30)]
    [TestCase(5, 30)]
    [TestCase(16, 30)]
    public async Task SendDueRemindersAsync_OutsideDaytimeInBrussels_SendsNothing(int utcHour, int utcMinute)
    {
        TrialReminderService sut = new(
            _repo.Object,
            _email.Object,
            Options.Create(new AppOptions()),
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 15, utcHour, utcMinute, 0, TimeSpan.Zero)),
            NullLogger<TrialReminderService>.Instance);

        Result<int> result = await sut.SendDueRemindersAsync();

        result.Value.Should().Be(0);
        _repo.Verify(r => r.GetAllTrialingReadOnlyAsync(It.IsAny<CancellationToken>()), Times.Never);
        _email.VerifyNoOtherCalls();
    }
}
