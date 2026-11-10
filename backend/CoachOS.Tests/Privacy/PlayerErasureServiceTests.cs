using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Models.Privacy;
using AwesomeAssertions;
using Moq;
using NUnit.Framework;
using static CoachOS.Tests.Privacy.PlayerErasureTestData;

namespace CoachOS.Tests.Privacy;

[TestFixture]
public class PlayerErasureServiceTests
{
    private const PrivacyErasureKind Lesson = PrivacyErasureKind.Enrollment;
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private Mock<IPlayerErasureRepository> _repo = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private PlayerErasureService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IPlayerErasureRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _repo.Setup(r => r.IsActiveAdminAsync(OrgId, ActorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repo.Setup(r => r.LockForErasureAsync(OrgId, It.IsAny<PrivacyErasureKind>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErasureLockOutcome.Acquired);
        _repo.Setup(r => r.DeleteAsync(OrgId, It.IsAny<PlayerErasurePlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErasureDeleteOutcome.Deleted);
        PlayerErasureExecutor executor = new(_repo.Object, _unitOfWork.Object, Clock());
        _service = new PlayerErasureService(_repo.Object, executor);
    }

    private void GivenFacts(PlayerErasureFacts facts) =>
        _repo.Setup(r => r.GetFactsAsync(OrgId, facts.Target.Kind, facts.Target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(facts);

    private static ErasePlayerRequest Confirm(string name = "Synthetic Player", Guid? leader = null, DateTime? updatedAt = null) =>
        new(name, leader, updatedAt);

    private void VerifyNothingErased()
    {
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<PlayerErasurePlan>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.AddReceiptAsync(It.IsAny<PrivacyErasureReceipt>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PreviewAsync_NotActiveAdmin_ReturnsForbidden()
    {
        Result<PlayerErasurePreviewDto> result = await _service.PreviewAsync(OrgId, Guid.NewGuid(), Lesson, Guid.NewGuid());

        result.Errors.Single().Code.Should().Be(ErrorCodes.Forbidden);
    }

    [Test]
    public async Task PreviewAsync_UnknownEnrollment_ReturnsNotFound()
    {
        Result<PlayerErasurePreviewDto> result = await _service.PreviewAsync(OrgId, ActorId, Lesson, Guid.NewGuid());

        result.Errors.Single().Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public async Task PreviewAsync_BlockedPlayer_ReturnsReasonsWithoutLockingOrTransaction()
    {
        PlayerErasureFacts facts = Facts(hasPayments: true);
        GivenFacts(facts);

        Result<PlayerErasurePreviewDto> result = await _service.PreviewAsync(OrgId, ActorId, Lesson, facts.Target.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CanErase.Should().BeFalse();
        result.Value.BlockingReasons.Should().Equal(PlayerErasureBlockReasons.FinancialRecords);
        result.Value.RetentionPeriodPassed.Should().BeTrue();
        result.Value.FormAnswerCount.Should().Be(2);
        _repo.Verify(r => r.LockForErasureAsync(It.IsAny<Guid>(), It.IsAny<PrivacyErasureKind>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task EraseAsync_NotActiveAdmin_ReturnsForbiddenWithoutTransaction()
    {
        Result<PlayerErasureResultDto> result = await _service.EraseAsync(OrgId, Guid.NewGuid(), Lesson, Guid.NewGuid(), Confirm());

        result.Errors.Single().Code.Should().Be(ErrorCodes.Forbidden);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task EraseAsync_EligiblePlayerAndExactName_DeletesWritesReceiptAndCommits()
    {
        PlayerErasureFacts facts = Facts();
        GivenFacts(facts);

        Result<PlayerErasureResultDto> result = await _service.EraseAsync(OrgId, ActorId, Lesson, facts.Target.Id, Confirm(updatedAt: UpdatedAt));

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(OrgId, It.Is<PlayerErasurePlan>(p => p.EnrollmentId == facts.Target.Id &&
            p.GroupEffect == ErasureGroupEffect.None && p.NewGroupLeaderId == null), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.AddReceiptAsync(It.Is<PrivacyErasureReceipt>(x => x.OrganizationId == OrgId &&
            x.SubjectId == facts.Target.Id && x.Kind == Lesson), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task EraseAsync_LockTimeout_ReturnsSpecificConflictAndRollsBack()
    {
        Guid id = Guid.NewGuid();
        _repo.Setup(r => r.LockForErasureAsync(OrgId, Lesson, id, It.IsAny<CancellationToken>())).ReturnsAsync(ErasureLockOutcome.LockTimeout);

        Result<PlayerErasureResultDto> result = await _service.EraseAsync(OrgId, ActorId, Lesson, id, Confirm());

        result.Errors.Single().Should().Be(PlayerErasureErrors.LockTimeout);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNothingErased();
    }

    [Test]
    public async Task EraseAsync_ForeignKeyFailure_ReturnsSpecificConflictWithoutReceipt()
    {
        PlayerErasureFacts facts = Facts();
        GivenFacts(facts);
        _repo.Setup(r => r.DeleteAsync(OrgId, It.IsAny<PlayerErasurePlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErasureDeleteOutcome.DependencyConflict);

        Result<PlayerErasureResultDto> result = await _service.EraseAsync(OrgId, ActorId, Lesson, facts.Target.Id, Confirm());

        result.Errors.Single().Should().Be(PlayerErasureErrors.DependencyConflict);
        _repo.Verify(r => r.AddReceiptAsync(It.IsAny<PrivacyErasureReceipt>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task EraseAsync_WrongName_ReturnsValidationAndErasesNothing()
    {
        PlayerErasureFacts facts = Facts();
        GivenFacts(facts);

        Result<PlayerErasureResultDto> result = await _service.EraseAsync(OrgId, ActorId, Lesson, facts.Target.Id, Confirm("synthetic player"));

        result.Errors.Single().Should().Be(PlayerErasureErrors.NameMismatch);
        VerifyNothingErased();
    }

    [Test]
    public async Task EraseAsync_RecheckInsideTransactionBlocks_ReturnsConflict()
    {
        PlayerErasureFacts facts = Facts(openPayments: true);
        GivenFacts(facts);

        Result<PlayerErasureResultDto> result = await _service.EraseAsync(OrgId, ActorId, Lesson, facts.Target.Id, Confirm());

        result.Errors.Single().Should().Be(PlayerErasureErrors.Blocked);
        VerifyNothingErased();
    }

    [Test]
    public async Task EraseAsync_ChangedSincePreview_ReturnsConflict()
    {
        PlayerErasureFacts facts = Facts();
        GivenFacts(facts);

        Result<PlayerErasureResultDto> result = await _service.EraseAsync(OrgId, ActorId, Lesson, facts.Target.Id,
            Confirm(updatedAt: UpdatedAt.AddSeconds(-1)));

        result.Errors.Single().Should().Be(PlayerErasureErrors.ChangedSincePreview);
        VerifyNothingErased();
    }

    [Test]
    public async Task EraseAsync_LeaderWithoutValidNewLeader_ReturnsValidation()
    {
        Guid target = Guid.NewGuid();
        PlayerErasureFacts facts = Facts(target, group: Group(target, target, Guid.NewGuid()));
        GivenFacts(facts);

        Result<PlayerErasureResultDto> missing = await _service.EraseAsync(OrgId, ActorId, Lesson, target, Confirm());
        Result<PlayerErasureResultDto> foreign = await _service.EraseAsync(OrgId, ActorId, Lesson, target, Confirm(leader: Guid.NewGuid()));
        Result<PlayerErasureResultDto> self = await _service.EraseAsync(OrgId, ActorId, Lesson, target, Confirm(leader: target));

        missing.Errors.Single().Should().Be(PlayerErasureErrors.NewGroupLeaderRequired);
        foreign.Errors.Single().Should().Be(PlayerErasureErrors.NewGroupLeaderRequired);
        self.Errors.Single().Should().Be(PlayerErasureErrors.NewGroupLeaderRequired);
        VerifyNothingErased();
    }

    [Test]
    public async Task EraseAsync_LeaderWithChosenGroupMember_TransfersLeadership()
    {
        Guid target = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        PlayerErasureFacts facts = Facts(target, group: Group(target, target, other));
        GivenFacts(facts);

        Result<PlayerErasureResultDto> result = await _service.EraseAsync(OrgId, ActorId, Lesson, target, Confirm(leader: other));

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(OrgId, It.Is<PlayerErasurePlan>(p =>
            p.GroupEffect == ErasureGroupEffect.TransferLeader && p.NewGroupLeaderId == other &&
            p.GroupId == facts.Group!.GroupId), It.IsAny<CancellationToken>()), Times.Once);
    }
}
