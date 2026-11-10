using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Application.Privacy.Validators;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Models.Privacy;
using CoachOS.Infrastructure.Privacy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using static CoachOS.Tests.Privacy.PlayerErasureTestData;

namespace CoachOS.Tests.Privacy;

[TestFixture]
public class PlayerRetentionServiceTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private Mock<IPlayerErasureRepository> _repo = null!;
    private PlayerRetentionService _service = null!;
    private PlayerErasureFacts _due = null!;
    private PlayerErasureFacts _recent = null!;
    private PlayerErasureFacts _leader = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IPlayerErasureRepository>();
        Guid leaderId = Guid.NewGuid();
        _due = Facts(lastParticipation: Today.AddMonths(-25));
        _recent = Facts(lastParticipation: Today.AddMonths(-6));
        _leader = Facts(leaderId, Today.AddYears(-3), group: Group(leaderId, leaderId, Guid.NewGuid()));
        List<ErasureCandidate> candidates = [];
        foreach (PlayerErasureFacts facts in new[] { _due, _recent, _leader })
        {
            candidates.Add(new ErasureCandidate(facts.Target.Id, PrivacyErasureKind.Enrollment));
            _repo.Setup(r => r.GetFactsAsync(OrgId, PrivacyErasureKind.Enrollment, facts.Target.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(facts);
        }
        _repo.Setup(r => r.GetCandidatesAsync(OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(candidates);
        _repo.Setup(r => r.LockForErasureAsync(OrgId, It.IsAny<PrivacyErasureKind>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErasureLockOutcome.Acquired);
        _repo.Setup(r => r.DeleteAsync(OrgId, It.IsAny<PlayerErasurePlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErasureDeleteOutcome.Deleted);
        PlayerErasureExecutor executor = new(_repo.Object, new Mock<IUnitOfWork>().Object, Clock());
        _service = new PlayerRetentionService(_repo.Object, executor);
    }

    [Test]
    public async Task RunAsync_DryRun_CountsOnlyDueWithoutDeleting()
    {
        Result<PlayerRetentionReport> result = await _service.RunAsync(OrgId, dryRun: true);

        result.Value.Should().Be(new PlayerRetentionReport(3, 1, 0));
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<PlayerErasurePlan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RunAsync_Apply_ErasesOnlyDuePlayersAndNeverPicksAGroupLeader()
    {
        Result<PlayerRetentionReport> result = await _service.RunAsync(OrgId, dryRun: false);

        result.Value.Should().Be(new PlayerRetentionReport(3, 1, 1));
        _repo.Verify(r => r.DeleteAsync(OrgId, It.Is<PlayerErasurePlan>(p => p.EnrollmentId == _due.Target.Id), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<PlayerErasurePlan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Worker_Disabled_DoesNotResolveAnyScope()
    {
        Mock<IServiceScopeFactory> scopes = new(MockBehavior.Strict);
        using PlayerRetentionWorker worker = new(scopes.Object, Options.Create(new PlayerRetentionOptions()),
            NullLogger<PlayerRetentionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        scopes.VerifyNoOtherCalls();
    }

    [Test]
    public void Options_DefaultToDisabledAndDryRun()
    {
        PlayerRetentionOptions options = new();
        options.Enabled.Should().BeFalse();
        options.DryRun.Should().BeTrue();
        options.OrganizationIds.Should().BeEmpty();
    }

    [TestCase("", false)]
    [TestCase("Synthetic Player", true)]
    public void Validator_RequiresConfirmationName(string name, bool valid)
    {
        new ErasePlayerRequestValidator().Validate(new ErasePlayerRequest(name)).IsValid.Should().Be(valid);
    }

    [Test]
    public void Validator_RejectsEmptyNewLeaderAndOverlongName()
    {
        ErasePlayerRequestValidator validator = new();
        validator.Validate(new ErasePlayerRequest("x", Guid.Empty)).IsValid.Should().BeFalse();
        validator.Validate(new ErasePlayerRequest(new string('a', 251))).IsValid.Should().BeFalse();
    }
}
