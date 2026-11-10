using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

[TestFixture]
public class OrganizationOffboardingServiceTests
{
    // 10/11/2026 10:00 UTC = 11:00 in Brussel.
    private static readonly DateOnly Today = new(2026, 11, 10);
    private const string OrgName = "TC Brederode";

    private Mock<IOrganizationRepository> _organizations = null!;
    private Mock<ISubscriptionRepository> _subscriptions = null!;
    private Mock<IOrganizationOffboardingRepository> _offboarding = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private OrganizationOffboardingService _sut = null!;
    private Organization _org = null!;

    [SetUp]
    public void SetUp()
    {
        _organizations = new Mock<IOrganizationRepository>();
        _subscriptions = new Mock<ISubscriptionRepository>();
        _offboarding = new Mock<IOrganizationOffboardingRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _org = new Organization { Id = Guid.NewGuid(), Name = OrgName, Email = "info@brederode.be" };

        _organizations.Setup(r => r.GetByIdAsync(_org.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(_org);
        _offboarding.Setup(r => r.CountDataAsync(_org.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Counts());

        _sut = new OrganizationOffboardingService(
            _organizations.Object, _subscriptions.Object, _offboarding.Object, _unitOfWork.Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 10, 10, 0, 0, TimeSpan.Zero)),
            NullLogger<OrganizationOffboardingService>.Instance);
    }

    private static OrganizationDataCounts Counts(int subscriptionPayments = 0)
        => new(1, 2, 10, 20, 1, 5, 3, 4, 2, subscriptionPayments);

    private void GivenTrialEnded(int daysAgo)
    {
        Subscription subscription = new()
        {
            Id = Guid.NewGuid(), OrganizationId = _org.Id, Status = SubscriptionStatus.Trialing,
            StartDate = Today.AddDays(-daysAgo - 30), TrialEndsAt = Today.AddDays(-daysAgo)
        };
        _subscriptions.Setup(r => r.GetByOrganizationReadOnlyAsync(_org.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
    }

    private void GivenActivePaying()
    {
        Subscription subscription = new()
        {
            Id = Guid.NewGuid(), OrganizationId = _org.Id, Status = SubscriptionStatus.Active,
            Plan = SubscriptionPlan.Set, StartDate = Today.AddDays(-400), CurrentPeriodEnd = Today.AddDays(30)
        };
        _subscriptions.Setup(r => r.GetByOrganizationReadOnlyAsync(_org.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
    }

    [Test]
    public async Task GetPreviewAsync_EndedMoreThan90DaysAgo_IsEligibleWithCounts()
    {
        GivenTrialEnded(daysAgo: 120);

        Result<OrganizationOffboardingPreviewDto> result = await _sut.GetPreviewAsync(_org.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Eligible.Should().BeTrue();
        result.Value.Blockers.Should().BeEmpty();
        result.Value.EndedOn.Should().Be(Today.AddDays(-120));
        result.Value.DeletableFrom.Should().Be(Today.AddDays(-30));
        result.Value.Counts.Enrollments.Should().Be(20);
        result.Value.Counts.AccountsToDelete.Should().Be(2);
        _offboarding.Verify(r => r.LockOrganizationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetPreviewAsync_ExactlyNinetyDays_IsEligible()
    {
        GivenTrialEnded(daysAgo: 90);

        Result<OrganizationOffboardingPreviewDto> result = await _sut.GetPreviewAsync(_org.Id);

        result.Value!.Eligible.Should().BeTrue();
    }

    [Test]
    public async Task GetPreviewAsync_EndedLessThan90DaysAgo_BlockedByRetention()
    {
        GivenTrialEnded(daysAgo: 89);

        Result<OrganizationOffboardingPreviewDto> result = await _sut.GetPreviewAsync(_org.Id);

        result.Value!.Eligible.Should().BeFalse();
        result.Value.Blockers.Should().ContainSingle(b => b.Code == OffboardingBlockers.RetentionPeriod && b.Date == Today.AddDays(1));
    }

    [Test]
    public async Task GetPreviewAsync_ActivePayingSubscription_BlockedAsRunning()
    {
        GivenActivePaying();

        Result<OrganizationOffboardingPreviewDto> result = await _sut.GetPreviewAsync(_org.Id);

        result.Value!.Eligible.Should().BeFalse();
        result.Value.EndedOn.Should().BeNull();
        result.Value.Blockers.Should().ContainSingle(b => b.Code == OffboardingBlockers.SubscriptionRunning);
    }

    [Test]
    public async Task GetPreviewAsync_NoSubscription_TreatedAsLegacyAndBlocked()
    {
        Result<OrganizationOffboardingPreviewDto> result = await _sut.GetPreviewAsync(_org.Id);

        result.Value!.SubscriptionStatus.Should().Be("Legacy");
        result.Value.Blockers.Should().ContainSingle(b => b.Code == OffboardingBlockers.SubscriptionRunning);
    }

    [Test]
    public async Task GetPreviewAsync_FinancialRecords_Blocked()
    {
        GivenTrialEnded(daysAgo: 200);
        _offboarding.Setup(r => r.CountDataAsync(_org.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Counts(subscriptionPayments: 1));

        Result<OrganizationOffboardingPreviewDto> result = await _sut.GetPreviewAsync(_org.Id);

        result.Value!.Eligible.Should().BeFalse();
        result.Value.Blockers.Should().ContainSingle(b => b.Code == OffboardingBlockers.FinancialRecords &&
            b.Message.Contains("wettelijke bewaarplicht"));
    }

    [Test]
    public async Task GetPreviewAsync_UnknownOrganization_NotFound()
    {
        Result<OrganizationOffboardingPreviewDto> result = await _sut.GetPreviewAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public async Task DeleteAsync_Eligible_LocksBeforeReadingThenDeletesAndCommits()
    {
        GivenTrialEnded(daysAgo: 120);
        List<string> calls = [];
        _unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Callback(() => calls.Add("begin"));
        _offboarding.Setup(r => r.LockOrganizationAsync(_org.Id, It.IsAny<CancellationToken>())).Callback(() => calls.Add("lock"));
        _organizations.Setup(r => r.GetByIdAsync(_org.Id, false, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("read")).ReturnsAsync(_org);
        _offboarding.Setup(r => r.DeleteOrganizationAsync(_org.Id, It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("delete")).ReturnsAsync(2);
        _unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Callback(() => calls.Add("commit"));

        Result<OrganizationDeletedDto> result = await _sut.DeleteAsync(_org.Id, new DeleteOrganizationRequest(OrgName));

        result.IsSuccess.Should().BeTrue();
        result.Value!.DeletedAccounts.Should().Be(2);
        calls.Should().Equal("begin", "lock", "read", "delete", "commit");
    }

    [Test]
    public async Task DeleteAsync_NameMismatch_RollsBackWithoutDeleting()
    {
        GivenTrialEnded(daysAgo: 120);

        Result<OrganizationDeletedDto> result = await _sut.DeleteAsync(_org.Id, new DeleteOrganizationRequest("tc brederode"));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _offboarding.Verify(r => r.DeleteOrganizationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_Blocked_ReturnsConflictWithoutDeleting()
    {
        GivenTrialEnded(daysAgo: 30);

        Result<OrganizationDeletedDto> result = await _sut.DeleteAsync(_org.Id, new DeleteOrganizationRequest(OrgName));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        _offboarding.Verify(r => r.DeleteOrganizationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_FinancialRecords_ReturnsConflictWithoutDeleting()
    {
        GivenTrialEnded(daysAgo: 300);
        _offboarding.Setup(r => r.CountDataAsync(_org.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Counts(subscriptionPayments: 3));

        Result<OrganizationDeletedDto> result = await _sut.DeleteAsync(_org.Id, new DeleteOrganizationRequest(OrgName));

        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict && e.Message.Contains("handmatig afhandelen"));
        _offboarding.Verify(r => r.DeleteOrganizationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ActivePaying_ReturnsConflict()
    {
        GivenActivePaying();

        Result<OrganizationDeletedDto> result = await _sut.DeleteAsync(_org.Id, new DeleteOrganizationRequest(OrgName));

        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        _offboarding.Verify(r => r.DeleteOrganizationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_UnknownOrganization_NotFound()
    {
        Result<OrganizationDeletedDto> result = await _sut.DeleteAsync(Guid.NewGuid(), new DeleteOrganizationRequest(OrgName));

        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_RepositoryThrows_RollsBackAndRethrows()
    {
        GivenTrialEnded(daysAgo: 120);
        _offboarding.Setup(r => r.DeleteOrganizationAsync(_org.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fk"));

        Func<Task> act = () => _sut.DeleteAsync(_org.Id, new DeleteOrganizationRequest(OrgName));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
