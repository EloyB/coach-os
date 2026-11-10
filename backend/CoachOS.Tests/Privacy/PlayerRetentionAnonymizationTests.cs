using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Models.Privacy;
using CoachOS.Infrastructure.Privacy;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using static CoachOS.Tests.Privacy.PlayerErasureTestData;

namespace CoachOS.Tests.Privacy;

[TestFixture]
public class PlayerRetentionAnonymizationTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private static RetentionDecision Decide(PlayerErasureFacts facts)
        => PlayerErasureRules.DecideRetention(facts, PlayerErasureRules.Evaluate(facts, Today), Today);

    [Test]
    public void DecideRetention_RecentParticipation_IsNotDueEvenWithPayments()
    {
        Decide(Facts(lastParticipation: Today.AddMonths(-23), hasPayments: true)).Should().Be(RetentionDecision.NotDue);
        Decide(Facts(lastParticipation: Today.AddMonths(-23))).Should().Be(RetentionDecision.NotDue);
    }

    [Test]
    public void DecideRetention_UnknownParticipation_IsNotDue()
    {
        Decide(Facts(noParticipation: true, hasPayments: true)).Should().Be(RetentionDecision.NotDue);
    }

    [Test]
    public void DecideRetention_ExpiredWithoutPayments_Erases()
    {
        Decide(Facts(lastParticipation: Today.AddMonths(-25))).Should().Be(RetentionDecision.Erase);
    }

    [Test]
    public void DecideRetention_ExpiredWithOnlyOwnCompletedPayments_Anonymizes()
    {
        Decide(Facts(lastParticipation: Today.AddMonths(-25), hasPayments: true)).Should().Be(RetentionDecision.Anonymize);
    }

    [Test]
    public void DecideRetention_OpenPayment_StaysBlocked()
    {
        Decide(Facts(lastParticipation: Today.AddMonths(-25), hasPayments: true, openPayments: true))
            .Should().Be(RetentionDecision.Blocked);
        Decide(Facts(lastParticipation: Today.AddMonths(-25), hasPayments: true, status: EnrollmentStatus.PendingPayment))
            .Should().Be(RetentionDecision.Blocked);
    }

    [Test]
    public void DecideRetention_PaymentsPlusAnotherBlocker_StaysBlocked()
    {
        Decide(Facts(lastParticipation: Today.AddMonths(-25), hasPayments: true, pendingMail: true))
            .Should().Be(RetentionDecision.Blocked);
        Decide(Facts(lastParticipation: Today.AddMonths(-25), hasPayments: true, chainIncomplete: true))
            .Should().Be(RetentionDecision.Blocked);
    }

    [Test]
    public void DecideRetention_GroupMemberWithPayments_StaysBlocked()
    {
        Guid id = Guid.NewGuid();
        Guid leader = Guid.NewGuid();
        ErasureGroupFacts group = Group(leader, leader, id) with { HasPayments = true };
        Decide(Facts(id, Today.AddMonths(-25), hasPayments: true, group: group)).Should().Be(RetentionDecision.Blocked);
    }

    [Test]
    public void DecideRetention_GroupLeaderWithOtherMembers_NeedsAHumanAndStaysBlocked()
    {
        Guid id = Guid.NewGuid();
        Decide(Facts(id, Today.AddMonths(-25), group: Group(id, id, Guid.NewGuid()))).Should().Be(RetentionDecision.Blocked);
    }

    [Test]
    public async Task RunAsync_PaidExpiredPlayer_IsAnonymizedWithReceiptAndNeverDeleted()
    {
        PlayerErasureFacts paid = Facts(lastParticipation: Today.AddMonths(-30), hasPayments: true);
        PlayerErasureFacts blocked = Facts(lastParticipation: Today.AddMonths(-30), hasPayments: true, openPayments: true);
        Mock<IPlayerErasureRepository> repo = Repository(paid, blocked);
        List<PrivacyErasureReceipt> receipts = [];
        repo.Setup(r => r.AddReceiptAsync(It.IsAny<PrivacyErasureReceipt>(), It.IsAny<CancellationToken>()))
            .Callback<PrivacyErasureReceipt, CancellationToken>((receipt, _) => receipts.Add(receipt))
            .Returns(Task.CompletedTask);
        PlayerRetentionService service = Service(repo);

        Result<PlayerRetentionReport> dry = await service.RunAsync(OrgId, dryRun: true);
        dry.Value.Should().Be(new PlayerRetentionReport(2, 0, 1, 1, 0, 0));
        repo.Verify(r => r.AnonymizeAsync(It.IsAny<Guid>(), It.IsAny<PlayerErasurePlan>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);

        Result<PlayerRetentionReport> applied = await service.RunAsync(OrgId, dryRun: false);

        applied.Value.Should().Be(new PlayerRetentionReport(2, 0, 1, 1, 0, 1));
        repo.Verify(r => r.AnonymizeAsync(OrgId, It.Is<PlayerErasurePlan>(p => p.EnrollmentId == paid.Target.Id),
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<PlayerErasurePlan>(), It.IsAny<CancellationToken>()), Times.Never);
        receipts.Should().ContainSingle().Which.Should().Match<PrivacyErasureReceipt>(r =>
            r.SubjectId == paid.Target.Id && r.Method == PrivacyErasureMethod.Anonymized && r.OrganizationId == OrgId);
    }

    [Test]
    public async Task RunAsync_PlayerChangedSinceCounting_IsRecheckedInsideTheLock()
    {
        PlayerErasureFacts before = Facts(lastParticipation: Today.AddMonths(-30), hasPayments: true);
        Mock<IPlayerErasureRepository> repo = Repository(before);
        PlayerErasureFacts after = before with { HasOpenPaymentsForPlayer = true };
        repo.SetupSequence(r => r.GetFactsAsync(OrgId, PrivacyErasureKind.Enrollment, before.Target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(before)
            .ReturnsAsync(after);

        Result<PlayerRetentionReport> result = await Service(repo).RunAsync(OrgId, dryRun: false);

        result.Value!.Anonymized.Should().Be(0);
        repo.Verify(r => r.AnonymizeAsync(It.IsAny<Guid>(), It.IsAny<PlayerErasurePlan>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Worker_WithoutOrganizationFilter_RunsEveryOrganization()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Mock<IPlayerRetentionService> retention = new();
        retention.Setup(s => s.GetOrganizationIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([first, second]);
        retention.Setup(s => s.RunAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PlayerRetentionReport>.Ok(new PlayerRetentionReport(0, 0, 0, 0, 0, 0)));
        Mock<IPrivacyCleanupService> cleanup = new();
        cleanup.Setup(s => s.RunAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PrivacyCleanupReport>.Ok(new PrivacyCleanupReport(0, 0, 0)));

        await RunWorkerOnceAsync(new PlayerRetentionOptions { Enabled = true }, retention, cleanup);

        retention.Verify(s => s.RunAsync(first, true, It.IsAny<CancellationToken>()), Times.Once);
        retention.Verify(s => s.RunAsync(second, true, It.IsAny<CancellationToken>()), Times.Once);
        cleanup.Verify(s => s.RunAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Worker_WithOrganizationFilter_OnlyRunsThoseAndDoesNotListAll()
    {
        Guid only = Guid.NewGuid();
        Mock<IPlayerRetentionService> retention = new();
        retention.Setup(s => s.RunAsync(only, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PlayerRetentionReport>.Ok(new PlayerRetentionReport(0, 0, 0, 0, 0, 0)));

        await RunWorkerOnceAsync(
            new PlayerRetentionOptions { Enabled = true, DryRun = false, OrganizationIds = [only, only], CleanupEnabled = false },
            retention, new Mock<IPrivacyCleanupService>(MockBehavior.Strict));

        retention.Verify(s => s.RunAsync(only, false, It.IsAny<CancellationToken>()), Times.Once);
        retention.Verify(s => s.GetOrganizationIdsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Worker_RetentionDisabled_StillCountsCleanupButTouchesNoPlayer()
    {
        Mock<IPlayerRetentionService> retention = new(MockBehavior.Strict);
        Mock<IPrivacyCleanupService> cleanup = new();
        cleanup.Setup(s => s.RunAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PrivacyCleanupReport>.Ok(new PrivacyCleanupReport(1, 2, 3)));

        await RunWorkerOnceAsync(new PlayerRetentionOptions(), retention, cleanup);

        cleanup.Verify(s => s.RunAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task Cleanup_UsesDocumentedCutoffsAndOnlyDeletesWhenNotDryRun(bool dryRun, bool apply)
    {
        DateTime now = Clock().GetUtcNow().UtcDateTime;
        Mock<IPrivacyCleanupRepository> repo = new();
        repo.Setup(r => r.CleanupCompletedMailAsync(It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(4);
        repo.Setup(r => r.CleanupExpiredMagicLinksAsync(It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(5);
        repo.Setup(r => r.CleanupExpiredConfirmationTokensAsync(It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(6);
        PrivacyCleanupService service = new(repo.Object, new Mock<IUnitOfWork>().Object, Clock());

        Result<PrivacyCleanupReport> result = await service.RunAsync(dryRun);

        result.Value.Should().Be(new PrivacyCleanupReport(4, 5, 6));
        repo.Verify(r => r.CleanupCompletedMailAsync(now.AddDays(-30), apply, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.CleanupExpiredMagicLinksAsync(now, apply, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.CleanupExpiredConfirmationTokensAsync(now.AddDays(-30), apply, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IPlayerErasureRepository> Repository(params PlayerErasureFacts[] facts)
    {
        Mock<IPlayerErasureRepository> repo = new();
        List<ErasureCandidate> candidates = [];
        foreach (PlayerErasureFacts item in facts)
        {
            candidates.Add(new ErasureCandidate(item.Target.Id, PrivacyErasureKind.Enrollment));
            repo.Setup(r => r.GetFactsAsync(OrgId, PrivacyErasureKind.Enrollment, item.Target.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(item);
        }
        repo.Setup(r => r.GetCandidatesAsync(OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(candidates);
        repo.Setup(r => r.LockForErasureAsync(OrgId, It.IsAny<PrivacyErasureKind>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErasureLockOutcome.Acquired);
        repo.Setup(r => r.AnonymizeAsync(OrgId, It.IsAny<PlayerErasurePlan>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErasureDeleteOutcome.Deleted);
        return repo;
    }

    private static PlayerRetentionService Service(Mock<IPlayerErasureRepository> repo)
        => new(repo.Object, new PlayerErasureExecutor(repo.Object, new Mock<IUnitOfWork>().Object, Clock()));

    private static async Task RunWorkerOnceAsync(
        PlayerRetentionOptions options, Mock<IPlayerRetentionService> retention, Mock<IPrivacyCleanupService> cleanup)
    {
        ServiceCollection services = new();
        services.AddSingleton(retention.Object);
        services.AddSingleton(cleanup.Object);
        await using ServiceProvider provider = services.BuildServiceProvider();
        using PlayerRetentionWorker worker = new(provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options), NullLogger<PlayerRetentionWorker>.Instance);

        // ExecuteAsync loopt op een achtergrondthread: wachten tot de eerste ronde klaar is
        // (de worker logt dan en wacht een dag), pas daarna stoppen.
        await worker.StartAsync(CancellationToken.None);
        await WaitUntilIdleAsync(retention, cleanup);
        await worker.StopAsync(CancellationToken.None);
    }

    /// <summary>De ronde is klaar zodra het aantal aanroepen op beide mocks een tijdje stabiel blijft.</summary>
    private static async Task WaitUntilIdleAsync(params Mock[] mocks)
    {
        int Calls() => mocks.Sum(m => m.Invocations.Count);
        int previous = -1;
        for (int i = 0; i < 100; i++)
        {
            await Task.Delay(20);
            int current = Calls();
            if (current > 0 && current == previous) return;
            previous = current;
        }
    }
}
