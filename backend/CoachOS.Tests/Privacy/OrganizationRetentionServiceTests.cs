using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Tests.TestHelpers;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

[TestFixture]
public class OrganizationRetentionServiceTests
{
    // 10/11/2026 10:00 UTC = 11:00 in Brussel (binnen het venster 8u–18u).
    private static readonly DateOnly Today = new(2026, 11, 10);
    private static readonly DateTimeOffset DayTime = new(2026, 11, 10, 10, 0, 0, TimeSpan.Zero);
    private const string AppUrl = "https://app.coach-os.be";
    private const string SuperAdmin = "admin@coach-os.be";

    private Mock<ISubscriptionRepository> _subscriptions = null!;
    private Mock<IOrganizationOffboardingRepository> _offboardingData = null!;
    private Mock<IOrganizationOffboardingService> _offboarding = null!;
    private Mock<IEmailService> _email = null!;
    private Mock<IEmailOrganizationContext> _mailOrganization = null!;
    private Mock<IOrganizationDeletionNoticeStatus> _noticeStatus = null!;

    [SetUp]
    public void SetUp()
    {
        _subscriptions = new Mock<ISubscriptionRepository>();
        _offboardingData = new Mock<IOrganizationOffboardingRepository>();
        _offboarding = new Mock<IOrganizationOffboardingService>();
        _email = new Mock<IEmailService>();
        _mailOrganization = new Mock<IEmailOrganizationContext>();
        _noticeStatus = new Mock<IOrganizationDeletionNoticeStatus>();

        // Default: elke claim lukt, geen enkele club heeft facturen en de laatste mail is verstuurd.
        _noticeStatus.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailDeliveryStatus.Sent);
        _subscriptions.Setup(r => r.TrySetDeletionScheduleAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly?>(), It.IsAny<OrganizationDeletionStage?>(),
                It.IsAny<DateOnly?>(), It.IsAny<OrganizationDeletionStage?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _offboardingData.Setup(r => r.CountDataAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Counts(subscriptionPayments: 0));
        _offboarding.Setup(s => s.DeleteExpiredAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, DateOnly _, CancellationToken _) =>
                Result<OrganizationDeletedDto>.Ok(new OrganizationDeletedDto(id, 1, DayTime.UtcDateTime)));
    }

    private OrganizationRetentionService Sut(
        DateTimeOffset? now = null, string superAdminEmail = SuperAdmin, bool enforceTrialExpiry = true) => new(
        _subscriptions.Object, _offboardingData.Object, _offboarding.Object, _noticeStatus.Object,
        _email.Object, _mailOrganization.Object,
        Options.Create(new AppOptions { FrontendBaseUrl = AppUrl }),
        Options.Create(new SuperAdminOptions { Email = superAdminEmail }),
        Options.Create(new BillingOptions { EnforceTrialExpiry = enforceTrialExpiry }),
        new FixedTimeProvider(now ?? DayTime),
        NullLogger<OrganizationRetentionService>.Instance);

    private static OrganizationDataCounts Counts(int subscriptionPayments)
        => new(1, 1, 1, 1, 0, 0, 0, 1, 1, subscriptionPayments);

    /// <summary>Proef zonder plan die <paramref name="endedDaysAgo"/> dagen geleden afliep.</summary>
    private static Subscription EndedTrial(
        int endedDaysAgo, DateOnly? scheduledOn = null, OrganizationDeletionStage? stage = null) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        Status = SubscriptionStatus.Trialing,
        StartDate = Today.AddDays(-endedDaysAgo - 30),
        TrialEndsAt = Today.AddDays(-endedDaysAgo),
        ScheduledDeletionOn = scheduledOn,
        DeletionStage = stage,
        DeletionStageChangedAt = stage is null ? null : DayTime.UtcDateTime.AddDays(-7),
        Organization = new Organization { Name = "TC Brederode", Email = "info@brederode.be" }
    };

    private void Arrange(params Subscription[] subscriptions)
        => _subscriptions.Setup(r => r.ListForRetentionReadOnlyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

    private void VerifyNotice(DateOnly deletionDate, bool finalNotice, Times times)
        => _email.Verify(e => e.SendOrganizationDeletionNoticeAsync(
            "info@brederode.be", "TC Brederode", deletionDate, finalNotice,
            It.Is<string>(u => u.StartsWith($"{AppUrl}/dashboard/settings?org=")), It.IsAny<CancellationToken>()), times);

    private void VerifyNoMail()
    {
        _email.Verify(e => e.SendOrganizationDeletionNoticeAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<bool>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(e => e.SendBillingSuperAdminNoticeAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyNoClaim()
        => _subscriptions.Verify(r => r.TrySetDeletionScheduleAsync(
            It.IsAny<Guid>(), It.IsAny<DateOnly?>(), It.IsAny<OrganizationDeletionStage?>(),
            It.IsAny<DateOnly?>(), It.IsAny<OrganizationDeletionStage?>(), It.IsAny<CancellationToken>()), Times.Never);

    private void VerifyNoDelete()
        => _offboarding.Verify(s => s.DeleteExpiredAsync(
            It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);

    // --- Eerste mail ---

    [Test]
    public async Task RunAsync_MoreThan30DaysBeforeDeletable_DoesNothing()
    {
        // Proef 59 dagen geleden afgelopen: verwijderbaar over 31 dagen.
        Arrange(EndedTrial(endedDaysAgo: 59));

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value.Should().Be(OrganizationRetentionReport.Empty);
        VerifyNoClaim();
        VerifyNoMail();
    }

    [Test]
    public async Task RunAsync_30DaysBeforeDeletable_SchedulesAndSendsFirstNotice()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 60);
        Arrange(trial);

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        DateOnly deletionOn = Today.AddDays(30);
        result.Value!.FirstNotices.Should().Be(1);
        _subscriptions.Verify(r => r.TrySetDeletionScheduleAsync(
            trial.Id, null, null, deletionOn, OrganizationDeletionStage.FirstNotice, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNotice(deletionOn, finalNotice: false, Times.Once());
        VerifyNoDelete();
    }

    [Test]
    public async Task RunAsync_LongPastDeletable_StillGivesClub30DaysNotice()
    {
        // Job stond lang uit: de bewaartermijn is al verstreken, maar de club krijgt toch 30 dagen.
        Arrange(EndedTrial(endedDaysAgo: 400));

        await Sut().RunAsync(dryRun: false);

        VerifyNotice(Today.AddDays(30), finalNotice: false, Times.Once());
        VerifyNoDelete();
    }

    [Test]
    public async Task RunAsync_ClaimLostToOtherInstance_SendsNothing()
    {
        Arrange(EndedTrial(endedDaysAgo: 70));
        _subscriptions.Setup(r => r.TrySetDeletionScheduleAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly?>(), It.IsAny<OrganizationDeletionStage?>(),
                It.IsAny<DateOnly?>(), It.IsAny<OrganizationDeletionStage?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.FirstNotices.Should().Be(0);
        VerifyNoMail();
    }

    [Test]
    public async Task RunAsync_MailFails_RevertsClaimAndCountsFailure()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 70);
        Arrange(trial);
        DateOnly deletionOn = Today.AddDays(30);
        _email.Setup(e => e.SendOrganizationDeletionNoticeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp"));

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.Failed.Should().Be(1);
        _subscriptions.Verify(r => r.TrySetDeletionScheduleAsync(
            trial.Id, deletionOn, OrganizationDeletionStage.FirstNotice, null, null, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task RunAsync_OneOrganizationFails_OthersStillProcessed()
    {
        Subscription broken = EndedTrial(endedDaysAgo: 70);
        Subscription fine = EndedTrial(endedDaysAgo: 70);
        Arrange(broken, fine);
        _offboardingData.Setup(r => r.CountDataAsync(broken.OrganizationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db"));

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.Failed.Should().Be(1);
        result.Value.FirstNotices.Should().Be(1);
    }

    // --- Laatste mail ---

    [Test]
    public async Task RunAsync_FirstNoticeSentAndMoreThan7DaysLeft_WaitsForFinalNotice()
    {
        Arrange(EndedTrial(endedDaysAgo: 120, Today.AddDays(8), OrganizationDeletionStage.FirstNotice));

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value.Should().Be(OrganizationRetentionReport.Empty);
        VerifyNoMail();
    }

    [Test]
    public async Task RunAsync_7DaysBeforeScheduled_SendsFinalNoticeForSameDate()
    {
        DateOnly scheduledOn = Today.AddDays(7);
        Subscription trial = EndedTrial(endedDaysAgo: 120, scheduledOn, OrganizationDeletionStage.FirstNotice);
        Arrange(trial);

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.FinalNotices.Should().Be(1);
        _subscriptions.Verify(r => r.TrySetDeletionScheduleAsync(
            trial.Id, scheduledOn, OrganizationDeletionStage.FirstNotice,
            scheduledOn, OrganizationDeletionStage.FinalNotice, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNotice(scheduledOn, finalNotice: true, Times.Once());
        VerifyNoDelete();
    }

    [Test]
    public async Task RunAsync_FinalNoticeMissed_PostponesDateBy7DaysInsteadOfDeleting()
    {
        // Job lag stil: de datum is voorbij zonder laatste mail. Nooit verwijderen zonder die mail.
        Arrange(EndedTrial(endedDaysAgo: 150, Today.AddDays(-2), OrganizationDeletionStage.FirstNotice));

        await Sut().RunAsync(dryRun: false);

        VerifyNotice(Today.AddDays(7), finalNotice: true, Times.Once());
        VerifyNoDelete();
    }

    // --- Verwijderen ---

    [Test]
    public async Task RunAsync_FinalNoticeSentAndDateReached_DeletesViaOffboarding()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 130, Today, OrganizationDeletionStage.FinalNotice);
        Arrange(trial);

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.Deleted.Should().Be(1);
        _offboarding.Verify(s => s.DeleteExpiredAsync(trial.OrganizationId, Today, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoMail();
    }

    [Test]
    public async Task RunAsync_FinalNoticeSentButDateNotReached_DoesNotDelete()
    {
        Arrange(EndedTrial(endedDaysAgo: 130, Today.AddDays(1), OrganizationDeletionStage.FinalNotice));

        await Sut().RunAsync(dryRun: false);

        VerifyNoDelete();
        VerifyNoMail();
    }

    [Test]
    public async Task RunAsync_OffboardingRefuses_CountsFailure()
    {
        Arrange(EndedTrial(endedDaysAgo: 130, Today, OrganizationDeletionStage.FinalNotice));
        _offboarding.Setup(s => s.DeleteExpiredAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OrganizationDeletedDto>.Fail(new Error(ErrorCodes.Conflict, "blocked")));

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.Deleted.Should().Be(0);
        result.Value.Failed.Should().Be(1);
    }

    // --- Club gaat verder ---

    [Test]
    public async Task RunAsync_ClubChoseAPlan_CancelsSchedule()
    {
        Subscription active = new()
        {
            Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Status = SubscriptionStatus.Active,
            Plan = SubscriptionPlan.Set, CurrentPeriodEnd = Today.AddDays(300),
            ScheduledDeletionOn = Today.AddDays(5), DeletionStage = OrganizationDeletionStage.FinalNotice,
            Organization = new Organization { Name = "TC Brederode", Email = "info@brederode.be" }
        };
        Arrange(active);

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.Cancelled.Should().Be(1);
        _subscriptions.Verify(r => r.TrySetDeletionScheduleAsync(
            active.Id, Today.AddDays(5), OrganizationDeletionStage.FinalNotice, null, null, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoDelete();
        VerifyNoMail();
    }

    [Test]
    public async Task RunAsync_TrialExtendedBeyondSchedule_CancelsSchedule()
    {
        // Superadmin verlengde de proef: verwijderbaar pas na de geplande datum.
        Subscription trial = EndedTrial(endedDaysAgo: 120, Today.AddDays(3), OrganizationDeletionStage.FinalNotice);
        trial.TrialEndsAt = Today.AddDays(10);
        Arrange(trial);

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.Cancelled.Should().Be(1);
        VerifyNoDelete();
    }

    // --- Betalende clubs ---

    [Test]
    public async Task RunAsync_PayingClubPastRetention_NotifiesSuperAdminOnceWithoutClubMail()
    {
        Subscription cancelled = new()
        {
            Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Status = SubscriptionStatus.Cancelled,
            Plan = SubscriptionPlan.Set, CurrentPeriodEnd = Today.AddDays(-91), CancelAt = Today.AddDays(-91),
            Organization = new Organization { Name = "TC Betaald", Email = "info@betaald.be" }
        };
        Arrange(cancelled);
        _offboardingData.Setup(r => r.CountDataAsync(cancelled.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Counts(subscriptionPayments: 4));

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.HandedToSuperAdmin.Should().Be(1);
        _subscriptions.Verify(r => r.TrySetDeletionScheduleAsync(
            cancelled.Id, null, null, Today.AddDays(-1), OrganizationDeletionStage.HandedToSuperAdmin,
            It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(e => e.SendBillingSuperAdminNoticeAsync(
            SuperAdmin, It.IsAny<string>(), It.Is<string>(b => b.Contains("TC Betaald")),
            $"{AppUrl}/super-admin", It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(e => e.SendOrganizationDeletionNoticeAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<bool>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoDelete();
    }

    [Test]
    public async Task RunAsync_PayingClubStillInRetention_DoesNothingYet()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 70);
        Arrange(trial);
        _offboardingData.Setup(r => r.CountDataAsync(trial.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Counts(subscriptionPayments: 1));

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value.Should().Be(OrganizationRetentionReport.Empty);
        VerifyNoClaim();
        VerifyNoMail();
    }

    [Test]
    public async Task RunAsync_AlreadyHandedToSuperAdmin_DoesNothing()
    {
        Arrange(EndedTrial(endedDaysAgo: 200, Today.AddDays(-110), OrganizationDeletionStage.HandedToSuperAdmin));

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value.Should().Be(OrganizationRetentionReport.Empty);
        VerifyNoClaim();
        VerifyNoMail();
        VerifyNoDelete();
    }

    [Test]
    public async Task RunAsync_PayingClubWithoutSuperAdminAddress_NotClaimedSoItRetriesLater()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 100);
        Arrange(trial);
        _offboardingData.Setup(r => r.CountDataAsync(trial.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Counts(subscriptionPayments: 1));

        Result<OrganizationRetentionReport> result = await Sut(superAdminEmail: "").RunAsync(dryRun: false);

        result.Value.Should().Be(OrganizationRetentionReport.Empty);
        VerifyNoClaim();
        VerifyNoMail();
    }

    [TestCase(OrganizationDeletionStage.FirstNotice, 7)]
    [TestCase(OrganizationDeletionStage.FinalNotice, 0)]
    public async Task RunAsync_PaymentStartedAfterNotice_HandsToSuperAdminInsteadOfContinuing(
        OrganizationDeletionStage stage, int daysUntilScheduled)
    {
        // Bv. een afgebroken checkout na de eerste mail: de offboarding zou weigeren.
        DateOnly scheduledOn = Today.AddDays(daysUntilScheduled);
        Subscription trial = EndedTrial(endedDaysAgo: 130, scheduledOn, stage);
        Arrange(trial);
        _offboardingData.Setup(r => r.CountDataAsync(trial.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Counts(subscriptionPayments: 1));

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.HandedToSuperAdmin.Should().Be(1);
        result.Value.Failed.Should().Be(0);
        _subscriptions.Verify(r => r.TrySetDeletionScheduleAsync(
            trial.Id, scheduledOn, stage, scheduledOn, OrganizationDeletionStage.HandedToSuperAdmin,
            It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(e => e.SendBillingSuperAdminNoticeAsync(
            SuperAdmin, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(e => e.SendOrganizationDeletionNoticeAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<bool>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoDelete();
    }

    [Test]
    public async Task RunAsync_ClubMail_IsTaggedWithOrganizationThenCleared()
    {
        // Zo ruimt de offboarding de verwijdermails mee op, en krijgt een latere
        // superadmin-melding in dezelfde run geen verkeerde organisatie.
        Subscription trial = EndedTrial(endedDaysAgo: 60);
        Arrange(trial);
        List<string> calls = [];
        _mailOrganization.Setup(m => m.Use(trial.OrganizationId)).Callback(() => calls.Add("use"));
        _mailOrganization.Setup(m => m.Clear()).Callback(() => calls.Add("clear"));
        _email.Setup(e => e.SendOrganizationDeletionNoticeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("mail")).Returns(Task.CompletedTask);

        await Sut().RunAsync(dryRun: false);

        calls.Should().Equal("use", "mail", "clear");
    }

    [Test]
    public async Task RunAsync_RevertFailsAfterMailFailure_CountsFailureWithoutThrowing()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 70);
        Arrange(trial);
        _email.Setup(e => e.SendOrganizationDeletionNoticeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("outbox"));
        _subscriptions.Setup(r => r.TrySetDeletionScheduleAsync(
                trial.Id, It.IsAny<DateOnly?>(), OrganizationDeletionStage.FirstNotice, null, null, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("db"));

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.Failed.Should().Be(1);
        _mailOrganization.Verify(m => m.Clear(), Times.Once);
    }

    // --- Dry-run en venster ---

    [Test]
    public async Task RunAsync_DryRun_CountsEverythingWithoutWritesMailsOrDeletes()
    {
        Subscription first = EndedTrial(endedDaysAgo: 60);
        Subscription final = EndedTrial(endedDaysAgo: 120, Today.AddDays(7), OrganizationDeletionStage.FirstNotice);
        Subscription due = EndedTrial(endedDaysAgo: 130, Today, OrganizationDeletionStage.FinalNotice);
        Arrange(first, final, due);

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: true);

        result.Value.Should().Be(new OrganizationRetentionReport(1, 1, 1, 0, 0, 0, 0));
        VerifyNoClaim();
        VerifyNoMail();
        VerifyNoDelete();
    }

    [TestCase(5)]   // 06:00 Brussel
    [TestCase(17)]  // 18:00 Brussel
    public async Task RunAsync_OutsideDaytime_DoesNothing(int utcHour)
    {
        Arrange(EndedTrial(endedDaysAgo: 130, Today, OrganizationDeletionStage.FinalNotice));

        Result<OrganizationRetentionReport> result =
            await Sut(new DateTimeOffset(2026, 11, 10, utcHour, 0, 0, TimeSpan.Zero)).RunAsync(dryRun: false);

        result.Value.Should().Be(OrganizationRetentionReport.Empty);
        _subscriptions.Verify(r => r.ListForRetentionReadOnlyAsync(It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoDelete();
    }

    // --- Proefvervaldag niet afgedwongen (Billing:EnforceTrialExpiry uit) ---

    [Test]
    public async Task RunAsync_TrialExpiryNotEnforced_SkipsExpiredTrialWithoutPlanning()
    {
        Arrange(EndedTrial(endedDaysAgo: 200), EndedTrial(endedDaysAgo: 300));

        Result<OrganizationRetentionReport> result = await Sut(enforceTrialExpiry: false).RunAsync(dryRun: false);

        result.Value!.Skipped.Should().Be(2);
        result.Value.FirstNotices.Should().Be(0);
        VerifyNoClaim();
        VerifyNoMail();
        VerifyNoDelete();
    }

    [Test]
    public async Task RunAsync_TrialExpiryNotEnforced_CancelsExistingScheduleInsteadOfDeleting()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 130, Today, OrganizationDeletionStage.FinalNotice);
        Arrange(trial);

        Result<OrganizationRetentionReport> result = await Sut(enforceTrialExpiry: false).RunAsync(dryRun: false);

        result.Value!.Cancelled.Should().Be(1);
        _subscriptions.Verify(r => r.TrySetDeletionScheduleAsync(
            trial.Id, Today, OrganizationDeletionStage.FinalNotice, null, null, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoDelete();
    }

    [Test]
    public async Task RunAsync_TrialExpiryNotEnforced_LockedClubStillProcessed()
    {
        // Vergrendeld (proef stopgezet na opzegging) is altijd enkel-lezen: dat einde telt wel.
        Subscription locked = EndedTrial(endedDaysAgo: 60);
        locked.Status = SubscriptionStatus.Locked;
        Arrange(locked);

        Result<OrganizationRetentionReport> result = await Sut(enforceTrialExpiry: false).RunAsync(dryRun: false);

        result.Value!.FirstNotices.Should().Be(1);
    }

    // --- Verwijderen pas na verstuurde laatste mail ---

    [Test]
    public async Task RunAsync_FinalNoticeSent_ChecksMailsQueuedSinceTheLastClaim()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 130, Today, OrganizationDeletionStage.FinalNotice);
        Arrange(trial);

        await Sut().RunAsync(dryRun: false);

        _noticeStatus.Verify(s => s.GetAsync(
            trial.OrganizationId, trial.DeletionStageChangedAt!.Value, It.IsAny<CancellationToken>()), Times.Once);
        _offboarding.Verify(s => s.DeleteExpiredAsync(trial.OrganizationId, Today, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RunAsync_FinalNoticeWithoutClaimTime_TreatedAsMissingAndSentAgain()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 130, Today, OrganizationDeletionStage.FinalNotice);
        trial.DeletionStageChangedAt = null;
        Arrange(trial);

        await Sut().RunAsync(dryRun: false);

        VerifyNoDelete();
        VerifyNotice(Today.AddDays(7), finalNotice: true, Times.Once());
    }

    [Test]
    public async Task RunAsync_FinalNoticeStillPending_WaitsWithoutDeleting()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 130, Today, OrganizationDeletionStage.FinalNotice);
        Arrange(trial);
        _noticeStatus.Setup(s => s.GetAsync(trial.OrganizationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailDeliveryStatus.Pending);

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value.Should().Be(OrganizationRetentionReport.Empty);
        VerifyNoDelete();
        VerifyNoMail();
    }

    [TestCase(EmailDeliveryStatus.Failed)]
    [TestCase(EmailDeliveryStatus.Missing)]
    public async Task RunAsync_FinalNoticeNotDelivered_PostponesAndSendsAgain(EmailDeliveryStatus status)
    {
        Subscription trial = EndedTrial(endedDaysAgo: 130, Today, OrganizationDeletionStage.FinalNotice);
        Arrange(trial);
        _noticeStatus.Setup(s => s.GetAsync(trial.OrganizationId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.FinalNotices.Should().Be(1);
        _subscriptions.Verify(r => r.TrySetDeletionScheduleAsync(
            trial.Id, Today, OrganizationDeletionStage.FinalNotice,
            Today.AddDays(7), OrganizationDeletionStage.FinalNotice, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNotice(Today.AddDays(7), finalNotice: true, Times.Once());
        VerifyNoDelete();
    }

    // --- Overdracht aan de superadmin na een verwijdermail ---

    [Test]
    public async Task RunAsync_HandedOverAfterNotice_TellsClubDeletionIsCancelled()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 130, Today.AddDays(7), OrganizationDeletionStage.FirstNotice);
        Arrange(trial);
        _offboardingData.Setup(r => r.CountDataAsync(trial.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Counts(subscriptionPayments: 1));

        await Sut().RunAsync(dryRun: false);

        _email.Verify(e => e.SendOrganizationDeletionCancelledAsync(
            "info@brederode.be", "TC Brederode", It.IsAny<CancellationToken>()), Times.Once);
        _mailOrganization.Verify(m => m.Use(trial.OrganizationId), Times.Once);
        _email.Verify(e => e.SendBillingSuperAdminNoticeAsync(
            SuperAdmin, It.IsAny<string>(), It.Is<string>(b => b.Contains("kreeg al een verwijdermail")),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RunAsync_HandedOverWithoutEarlierNotice_SendsNoClubMail()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 100);
        Arrange(trial);
        _offboardingData.Setup(r => r.CountDataAsync(trial.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Counts(subscriptionPayments: 1));

        await Sut().RunAsync(dryRun: false);

        _email.Verify(e => e.SendOrganizationDeletionCancelledAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RunAsync_CancelledMailFails_HandOverStaysAndNoFailure()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 130, Today, OrganizationDeletionStage.FinalNotice);
        Arrange(trial);
        _offboardingData.Setup(r => r.CountDataAsync(trial.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Counts(subscriptionPayments: 1));
        _email.Setup(e => e.SendOrganizationDeletionCancelledAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("outbox"));

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.HandedToSuperAdmin.Should().Be(1);
        result.Value.Failed.Should().Be(0);
        _subscriptions.Verify(r => r.TrySetDeletionScheduleAsync(
            trial.Id, Today, OrganizationDeletionStage.HandedToSuperAdmin, It.IsAny<DateOnly?>(),
            It.IsAny<OrganizationDeletionStage?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- Superadmin-tekst ---

    [Test]
    public async Task RunAsync_SuperAdminNotice_UsesPastTenseForEndedRetention()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 100);
        Arrange(trial);
        _offboardingData.Setup(r => r.CountDataAsync(trial.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Counts(subscriptionPayments: 1));

        await Sut().RunAsync(dryRun: false);

        string expected = $"liep af op {Today.AddDays(-10):dd/MM/yyyy}";
        _email.Verify(e => e.SendBillingSuperAdminNoticeAsync(
            SuperAdmin, It.IsAny<string>(), It.Is<string>(b => b.Contains(expected) && !b.Contains("verstreken")),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RunAsync_SuperAdminNotice_UsesFutureTenseWhenRetentionStillRuns()
    {
        // Betaling na de eerste mail, vóór het einde van de bewaartermijn.
        Subscription trial = EndedTrial(endedDaysAgo: 85, Today.AddDays(7), OrganizationDeletionStage.FirstNotice);
        Arrange(trial);
        _offboardingData.Setup(r => r.CountDataAsync(trial.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Counts(subscriptionPayments: 1));

        await Sut().RunAsync(dryRun: false);

        string expected = $"loopt af op {Today.AddDays(5):dd/MM/yyyy}";
        _email.Verify(e => e.SendBillingSuperAdminNoticeAsync(
            SuperAdmin, It.IsAny<string>(), It.Is<string>(b => b.Contains(expected)),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Geen mailadres ---

    [Test]
    public async Task RunAsync_OrganizationWithoutEmail_SkippedWithoutClaimOrFailure()
    {
        Subscription first = EndedTrial(endedDaysAgo: 60);
        first.Organization.Email = "";
        Subscription final = EndedTrial(endedDaysAgo: 130, Today.AddDays(7), OrganizationDeletionStage.FirstNotice);
        final.Organization.Email = " ";
        Arrange(first, final);

        Result<OrganizationRetentionReport> result = await Sut().RunAsync(dryRun: false);

        result.Value!.Skipped.Should().Be(2);
        result.Value.Failed.Should().Be(0);
        VerifyNoClaim();
        VerifyNoMail();
    }

    // --- Exportlink ---

    [Test]
    public async Task RunAsync_ExportLink_OpensTheOrganizationOfTheMail()
    {
        Subscription trial = EndedTrial(endedDaysAgo: 60);
        Arrange(trial);

        await Sut().RunAsync(dryRun: false);

        _email.Verify(e => e.SendOrganizationDeletionNoticeAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<bool>(),
            $"{AppUrl}/dashboard/settings?org={trial.OrganizationId}", It.IsAny<CancellationToken>()), Times.Once);
    }
}
