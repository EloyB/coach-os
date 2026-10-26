using CoachOS.Application.Abstractions;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Payments;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class CampServiceTests
{
    private Mock<ICampRepository> _camps = null!;
    private Mock<ICampEnrollmentRepository> _enrollments = null!;
    private Mock<ICampEnrollmentFormRepository> _forms = null!;
    private Mock<ITennisClubRepository> _clubs = null!;
    private Mock<IUserLookupService> _users = null!;
    private Mock<IPaymentRepository> _payments = null!;
    private Mock<IPaymentService> _paymentService = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CampService _sut = null!;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _clubId = Guid.NewGuid();
    private readonly Guid _trainerId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _camps = new Mock<ICampRepository>();
        _enrollments = new Mock<ICampEnrollmentRepository>();
        _forms = new Mock<ICampEnrollmentFormRepository>();
        _clubs = new Mock<ITennisClubRepository>();
        _users = new Mock<IUserLookupService>();
        _payments = new Mock<IPaymentRepository>();
        _paymentService = new Mock<IPaymentService>();
        _unitOfWork = new Mock<IUnitOfWork>();
        // Standaard bestaat elk gevraagd kamp (zonder prijsopties); tests overschrijven dit waar nodig.
        _camps.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, Guid org, CancellationToken _) => new Camp { Id = id, OrganizationId = org });
        _enrollments.Setup(r => r.GetByCampWithResponsesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _sut = new CampService(_camps.Object, _enrollments.Object, _forms.Object, _clubs.Object,
            _users.Object, _payments.Object,
            _unitOfWork.Object, _paymentService.Object);
    }

    private CreateCampRequest Request() => new(
        "Paaskamp", null, _clubId, null, 120m, "2026-04-14", "2026-04-16",
        new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), 20,
        new List<CreateCampDayRequest>
        {
            new("2026-04-14", "09:00", "16:00", new List<CreateCampDayTrainerRequest> { new(_trainerId, "09:00", "12:00") }),
            new("2026-04-15", "09:00", "16:00", new List<CreateCampDayTrainerRequest>()),
        });

    private void Happy()
    {
        _clubs.Setup(r => r.ExistsAsync(_clubId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _users.Setup(r => r.IsActiveTrainerAsync(_trainerId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Test]
    public async Task CreateAsync_Valid_AddsCampWithDaysAndTrainers()
    {
        Happy();
        Result<Guid> result = await _sut.CreateAsync(_orgId, Request(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        _camps.Verify(r => r.AddAsync(
            It.Is<Camp>(c => c.OrganizationId == _orgId && c.Days.Count == 2
                && c.Days.First().TrainerAssignments.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_ClubNotInOrg_ReturnsNotFound()
    {
        Happy();
        _clubs.Setup(r => r.ExistsAsync(_clubId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Result<Guid> result = await _sut.CreateAsync(_orgId, Request(), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        _camps.Verify(r => r.AddAsync(It.IsAny<Camp>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_InactiveTrainer_ReturnsNotFound()
    {
        Happy();
        _users.Setup(r => r.IsActiveTrainerAsync(_trainerId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Result<Guid> result = await _sut.CreateAsync(_orgId, Request(), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task UpdateAsync_ReplacesDays_RemovesOldDaysAndSavesOnce()
    {
        Happy();
        Guid campId = Guid.NewGuid();

        // Bestaand kamp met 2 dagen, elk met een trainerassignment (tracked).
        Camp existing = new()
        {
            Id = campId,
            OrganizationId = _orgId,
            TennisClubId = _clubId,
            Name = "Oud kamp",
            StartDate = new DateOnly(2026, 4, 14),
            EndDate = new DateOnly(2026, 4, 16),
        };
        for (int i = 0; i < 2; i++)
        {
            CampDay day = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = _orgId,
                CampId = campId,
                Date = new DateOnly(2026, 4, 14 + i),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(16, 0),
            };
            day.TrainerAssignments.Add(new CampDayTrainer
            {
                Id = Guid.NewGuid(),
                OrganizationId = _orgId,
                TrainerId = _trainerId,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0),
            });
            existing.Days.Add(day);
        }

        _camps.Setup(r => r.GetByIdWithDetailsAsync(campId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        UpdateCampRequest request = new(
            "Nieuw kamp", null, _clubId, null, 150m, "2026-05-01", "2026-05-02",
            new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), 25, true,
            new List<CreateCampDayRequest>
            {
                new("2026-05-01", "10:00", "15:00", new List<CreateCampDayTrainerRequest> { new(_trainerId, "10:00", "13:00") }),
            });

        Result result = await _sut.UpdateAsync(campId, _orgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _camps.Verify(r => r.RemoveDays(It.Is<IEnumerable<CampDay>>(d => d.Count() == 2)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        existing.Days.Should().HaveCount(1);
        existing.Days.First().Date.Should().Be(new DateOnly(2026, 5, 1));
    }

    [Test]
    public async Task GetEnrollmentsAsync_MarksGroupLeaderAndExposesGroupId()
    {
        Guid campId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Guid leaderId = Guid.NewGuid();
        CampEnrollmentGroup group = new() { Id = groupId, Name = "Groep A", LeaderEnrollmentId = leaderId };
        CampEnrollment leader = new()
        {
            Id = leaderId, ParticipantName = "Sarah", CampEnrollmentGroupId = groupId, Group = group,
        };
        CampEnrollment member = new()
        {
            Id = Guid.NewGuid(), ParticipantName = "Tom", CampEnrollmentGroupId = groupId, Group = group,
        };
        CampEnrollment solo = new() { Id = Guid.NewGuid(), ParticipantName = "Emma" };

        _camps.Setup(r => r.ExistsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetByCampWithResponsesAsync(campId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([leader, member, solo]);
        _payments.Setup(r => r.GetLatestByCampEnrollmentIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Result<List<CampEnrollmentDto>> result = await _sut.GetEnrollmentsAsync(campId, _orgId);

        result.IsSuccess.Should().BeTrue();
        CampEnrollmentDto leaderDto = result.Value!.Single(d => d.Id == leaderId);
        leaderDto.IsGroupLeader.Should().BeTrue();
        leaderDto.EnrollmentGroupId.Should().Be(groupId);
        CampEnrollmentDto memberDto = result.Value!.Single(d => d.Id == member.Id);
        memberDto.IsGroupLeader.Should().BeFalse();
        memberDto.EnrollmentGroupId.Should().Be(groupId);
        CampEnrollmentDto soloDto = result.Value!.Single(d => d.Id == solo.Id);
        soloDto.IsGroupLeader.Should().BeFalse();
        soloDto.EnrollmentGroupId.Should().BeNull();
    }

    [Test]
    public async Task GetEnrollmentsAsync_IncludesLatestPaymentWithMollieId()
    {
        Guid campId = Guid.NewGuid();
        Guid paidId = Guid.NewGuid();
        Guid unpaidId = Guid.NewGuid();
        DateTime paidAt = new(2026, 9, 5, 12, 54, 24, DateTimeKind.Utc);

        _camps.Setup(r => r.ExistsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetByCampWithResponsesAsync(campId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { Id = paidId, OrganizationId = _orgId, CampId = campId, ParticipantName = "Betaald" },
                new() { Id = unpaidId, OrganizationId = _orgId, CampId = campId, ParticipantName = "Open" },
            ]);
        _payments.Setup(r => r.GetLatestByCampEnrollmentIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Payment>
            {
                [paidId] = new()
                {
                    CampEnrollmentId = paidId,
                    Amount = 240m,
                    Status = PaymentStatus.Paid,
                    Method = PaymentMethod.Online,
                    PaidAt = paidAt,
                    MolliePaymentId = "tr_camp123",
                },
            });

        Result<List<CampEnrollmentDto>> result = await _sut.GetEnrollmentsAsync(campId, _orgId);

        result.IsSuccess.Should().BeTrue();
        CampEnrollmentDto paid = result.Value!.Single(e => e.Id == paidId);
        paid.PaymentStatus.Should().Be("Paid");
        paid.PaymentMethod.Should().Be("Online");
        paid.PaymentAmount.Should().Be(240m);
        paid.PaidAt.Should().Be(paidAt);
        paid.MolliePaymentId.Should().Be("tr_camp123");

        CampEnrollmentDto unpaid = result.Value!.Single(e => e.Id == unpaidId);
        unpaid.PaymentStatus.Should().BeNull();
        unpaid.MolliePaymentId.Should().BeNull();
    }

    // ── Cancel ───────────────────────────────────────────────────────────────

    private static CampEnrollmentGroup BuildGroup(
        Guid groupId, Guid campId, Guid orgId, out CampEnrollment leader, out CampEnrollment member)
    {
        leader = new CampEnrollment
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, CampId = campId, CampEnrollmentGroupId = groupId,
            ParticipantName = "Sarah", Status = Domain.Enums.EnrollmentStatus.Confirmed,
        };
        member = new CampEnrollment
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, CampId = campId, CampEnrollmentGroupId = groupId,
            ParticipantName = "Tom", Status = Domain.Enums.EnrollmentStatus.Confirmed,
        };
        return new CampEnrollmentGroup
        {
            Id = groupId, OrganizationId = orgId, CampId = campId,
            LeaderEnrollmentId = leader.Id, Members = [leader, member],
        };
    }

    [Test]
    public async Task CancelEnrollmentAsync_Solo_SetsCancelled()
    {
        Guid campId = Guid.NewGuid();
        CampEnrollment e = new()
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, CampId = campId,
            Status = Domain.Enums.EnrollmentStatus.Confirmed,
        };
        _camps.Setup(r => r.ExistsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetByIdAsync(e.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        Result result = await _sut.CancelEnrollmentAsync(campId, e.Id, _orgId);

        result.IsSuccess.Should().BeTrue();
        e.Status.Should().Be(Domain.Enums.EnrollmentStatus.Cancelled);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CancelEnrollmentAsync_WrongCamp_ReturnsNotFound()
    {
        Guid routeCampId = Guid.NewGuid();
        CampEnrollment e = new()
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, CampId = Guid.NewGuid(),
            Status = Domain.Enums.EnrollmentStatus.Confirmed,
        };
        _camps.Setup(r => r.ExistsAsync(routeCampId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetByIdAsync(e.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        Result result = await _sut.CancelEnrollmentAsync(routeCampId, e.Id, _orgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
        e.Status.Should().Be(Domain.Enums.EnrollmentStatus.Confirmed);
    }

    [Test]
    public async Task CancelEnrollmentAsync_AlreadyCancelled_ReturnsValidation()
    {
        Guid campId = Guid.NewGuid();
        CampEnrollment e = new()
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, CampId = campId,
            Status = Domain.Enums.EnrollmentStatus.Cancelled,
        };
        _camps.Setup(r => r.ExistsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetByIdAsync(e.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        Result result = await _sut.CancelEnrollmentAsync(campId, e.Id, _orgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public async Task CancelEnrollmentAsync_LeaderWithActiveMembers_IsBlocked()
    {
        Guid campId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        CampEnrollmentGroup group = BuildGroup(groupId, campId, _orgId, out CampEnrollment leader, out _);
        _camps.Setup(r => r.ExistsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetByIdAsync(leader.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(leader);
        _enrollments.Setup(r => r.GetGroupWithMembersAsync(groupId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        Result result = await _sut.CancelEnrollmentAsync(campId, leader.Id, _orgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        leader.Status.Should().Be(Domain.Enums.EnrollmentStatus.Confirmed);
    }

    [Test]
    public async Task CancelEnrollmentAsync_MemberOfGroup_IsCancelled()
    {
        Guid campId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        CampEnrollmentGroup group = BuildGroup(groupId, campId, _orgId, out _, out CampEnrollment member);
        _camps.Setup(r => r.ExistsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetByIdAsync(member.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _enrollments.Setup(r => r.GetGroupWithMembersAsync(groupId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        Result result = await _sut.CancelEnrollmentAsync(campId, member.Id, _orgId);

        result.IsSuccess.Should().BeTrue();
        member.Status.Should().Be(Domain.Enums.EnrollmentStatus.Cancelled);
    }

    [Test]
    public async Task CancelGroupAsync_CancelsAllActiveMembersInOneSave()
    {
        Guid campId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        CampEnrollmentGroup group = BuildGroup(groupId, campId, _orgId, out CampEnrollment leader, out CampEnrollment member);
        _camps.Setup(r => r.ExistsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetGroupWithMembersAsync(groupId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        Result result = await _sut.CancelGroupAsync(campId, groupId, _orgId);

        result.IsSuccess.Should().BeTrue();
        leader.Status.Should().Be(Domain.Enums.EnrollmentStatus.Cancelled);
        member.Status.Should().Be(Domain.Enums.EnrollmentStatus.Cancelled);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CancelGroupAsync_WrongCamp_ReturnsNotFound()
    {
        Guid routeCampId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        CampEnrollmentGroup group = BuildGroup(groupId, Guid.NewGuid(), _orgId, out _, out _);
        _camps.Setup(r => r.ExistsAsync(routeCampId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetGroupWithMembersAsync(groupId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        Result result = await _sut.CancelGroupAsync(routeCampId, groupId, _orgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public async Task CancelGroupAsync_AllAlreadyCancelled_ReturnsValidation()
    {
        Guid campId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        CampEnrollmentGroup group = BuildGroup(groupId, campId, _orgId, out CampEnrollment leader, out CampEnrollment member);
        leader.Status = Domain.Enums.EnrollmentStatus.Cancelled;
        member.Status = Domain.Enums.EnrollmentStatus.Cancelled;
        _camps.Setup(r => r.ExistsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetGroupWithMembersAsync(groupId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        Result result = await _sut.CancelGroupAsync(campId, groupId, _orgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    // ── AddGroupMember ───────────────────────────────────────────────────────

    private Camp ArrangeCampWithGroup(
        out CampEnrollmentGroup group, out CampEnrollment leader, out CampEnrollment member,
        decimal price = 100m, int? maxParticipants = null)
    {
        Guid campId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        group = BuildGroup(groupId, campId, _orgId, out leader, out member);
        leader.Status = Domain.Enums.EnrollmentStatus.PendingPayment;
        member.Status = Domain.Enums.EnrollmentStatus.PendingPayment;
        Camp camp = new()
        {
            Id = campId, OrganizationId = _orgId, Name = "Paaskamp", Price = price,
            MaxParticipants = maxParticipants,
        };
        _camps.Setup(r => r.GetByIdWithDetailsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(camp);
        _enrollments.Setup(r => r.GetGroupWithMembersAsync(groupId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        return camp;
    }

    private static AddCampGroupMemberRequest MemberRequest() => new()
    {
        ParticipantName = "Lisa", ParticipantEmail = "lisa@test.com",
    };

    [Test]
    public async Task AddGroupMemberAsync_Success_InheritsLeaderStatusAndAddsToGroup()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out _, out _);
        CampEnrollment? added = null;
        _enrollments.Setup(r => r.AddAsync(It.IsAny<CampEnrollment>(), It.IsAny<CancellationToken>()))
            .Callback<CampEnrollment, CancellationToken>((e, _) => added = e)
            .Returns(Task.CompletedTask);

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId, MemberRequest());

        result.IsSuccess.Should().BeTrue();
        added!.CampEnrollmentGroupId.Should().Be(group.Id);
        added.Status.Should().Be(Domain.Enums.EnrollmentStatus.PendingPayment);
        added.ParticipantEmail.Should().Be("lisa@test.com");
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddGroupMemberAsync_GroupOfOtherCamp_ReturnsNotFound()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out _, out _);
        group.CampId = Guid.NewGuid();

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId, MemberRequest());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public async Task AddGroupMemberAsync_LeaderCancelled_ReturnsConflict()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out CampEnrollment leader, out _);
        leader.Status = Domain.Enums.EnrollmentStatus.Cancelled;

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId, MemberRequest());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
    }

    [Test]
    public async Task AddGroupMemberAsync_PaymentPaid_ReturnsConflict()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out CampEnrollment leader, out _);
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(leader.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment { Status = Domain.Enums.PaymentStatus.Paid, Method = Domain.Enums.PaymentMethod.Cash });

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId, MemberRequest());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        _enrollments.Verify(r => r.AddAsync(It.IsAny<CampEnrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AddGroupMemberAsync_OnlinePaymentPending_ReturnsConflict()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out CampEnrollment leader, out _);
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(leader.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment { Status = Domain.Enums.PaymentStatus.Pending, Method = Domain.Enums.PaymentMethod.Online });

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId, MemberRequest());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
    }

    [Test]
    public async Task AddGroupMemberAsync_CashPending_UpdatesAmountToActiveMembersPlusOne()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out CampEnrollment leader, out _, price: 100m);
        Payment cash = new()
        {
            Status = Domain.Enums.PaymentStatus.Pending, Method = Domain.Enums.PaymentMethod.Cash, Amount = 200m,
        };
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(leader.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cash);
        // Simuleert EF relationship fixup: het nieuwe lid zit na AddAsync al in group.Members.
        _enrollments.Setup(r => r.AddAsync(It.IsAny<CampEnrollment>(), It.IsAny<CancellationToken>()))
            .Callback<CampEnrollment, CancellationToken>((e, _) => group.Members.Add(e))
            .Returns(Task.CompletedTask);

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId, MemberRequest());

        result.IsSuccess.Should().BeTrue();
        cash.Amount.Should().Be(300m); // 2 actieve leden + 1 nieuw, niet dubbel geteld
    }

    [Test]
    public async Task AddGroupMemberAsync_CampFull_ReturnsConflictAndRollsBack()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out _, out _, maxParticipants: 5);
        _enrollments.Setup(r => r.CountActiveByCampAsync(camp.Id, It.IsAny<CancellationToken>())).ReturnsAsync(5);

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId, MemberRequest());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddGroupMemberAsync_DuplicateEmail_ReturnsConflict()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out _, out _);
        _enrollments.Setup(r => r.IsDuplicateAsync(camp.Id, "lisa@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId, MemberRequest());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
    }

    [Test]
    public async Task AddGroupMemberAsync_MissingRequiredFormField_ReturnsValidation()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out _, out _);
        _forms.Setup(f => f.GetByCampIdReadOnlyAsync(camp.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampEnrollmentForm
            {
                Fields = [new CampFormField { Id = Guid.NewGuid(), Label = "Shirtmaat", IsRequired = true }],
            });

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId, MemberRequest());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public async Task CancelEnrollmentAsync_MemberOfGroup_UpdatesPendingCashAmount()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out CampEnrollment leader, out CampEnrollment member, price: 100m);
        _camps.Setup(r => r.ExistsAsync(camp.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetByIdAsync(member.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        Payment cash = new()
        {
            Status = Domain.Enums.PaymentStatus.Pending, Method = Domain.Enums.PaymentMethod.Cash, Amount = 200m,
        };
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(leader.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cash);

        Result result = await _sut.CancelEnrollmentAsync(camp.Id, member.Id, _orgId);

        result.IsSuccess.Should().BeTrue();
        cash.Amount.Should().Be(100m); // enkel de leider blijft over
    }

    // ── UpdateEnrollment ─────────────────────────────────────────────────────

    private (Guid CampId, CampEnrollment Enrollment) ArrangeEditable(
        Domain.Enums.EnrollmentStatus status = Domain.Enums.EnrollmentStatus.PendingPayment)
    {
        Guid campId = Guid.NewGuid();
        CampEnrollment e = new()
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, CampId = campId,
            ParticipantName = "Emma", ParticipantEmail = "emma@test.com", Status = status,
        };
        _camps.Setup(r => r.ExistsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetByIdWithResponsesAsync(e.Id, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(e);
        return (campId, e);
    }

    private static UpdateCampEnrollmentRequest UpdateRequest(
        params (Guid FieldId, string Value)[] responses) => new()
    {
        ParticipantName = "  Emma Peeters ",
        ParticipantEmail = " emma.nieuw@test.com ",
        ParticipantPhone = "0471 11 11 11",
        Responses = responses.Select(r => new CampFormResponseValueDto
        {
            CampFormFieldId = r.FieldId, Value = r.Value,
        }).ToList(),
    };

    [Test]
    public async Task UpdateEnrollmentAsync_UpdatesBasicsAndKeepsStatus()
    {
        (Guid campId, CampEnrollment e) = ArrangeEditable();

        Result result = await _sut.UpdateEnrollmentAsync(campId, e.Id, _orgId, UpdateRequest());

        result.IsSuccess.Should().BeTrue();
        e.ParticipantName.Should().Be("Emma Peeters");
        e.ParticipantEmail.Should().Be("emma.nieuw@test.com");
        e.ParticipantPhone.Should().Be("0471 11 11 11");
        e.Status.Should().Be(Domain.Enums.EnrollmentStatus.PendingPayment);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test, Ignore("Wordt in Task 5 herschreven naar een betaald pakket: een tarief is nu een korting")]
    public async Task UpdateEnrollmentAsync_ConfirmedWithoutPayment_PaidOption_ReturnsConflict()
    {
        (Guid campId, CampEnrollment e) = ArrangeEditable(Domain.Enums.EnrollmentStatus.Confirmed);
        CampPrice paid = new() { Id = Guid.NewGuid(), Label = "Leden", Discount = 0m };
        _camps.Setup(r => r.GetByIdWithDetailsAsync(campId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Camp { Id = campId, OrganizationId = _orgId, Price = 0m, Prices = { paid } });

        Result result = await _sut.UpdateEnrollmentAsync(
            campId, e.Id, _orgId, UpdateRequest() with { CampPriceId = paid.Id });

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        e.CampPriceId.Should().BeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test, Ignore("Wordt in Task 5 herschreven naar een betaald pakket: een tarief is nu een korting")]
    public async Task AddGroupMemberAsync_ConfirmedGroupWithoutPayment_PaidOption_ReturnsConflict()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out CampEnrollment leader, out _, price: 0m);
        leader.Status = Domain.Enums.EnrollmentStatus.Confirmed;
        CampPrice paid = new() { Id = Guid.NewGuid(), Label = "Leden", Discount = 0m };
        camp.Prices.Add(paid);

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId,
            new AddCampGroupMemberRequest
            {
                ParticipantName = "Lisa", ParticipantEmail = "lisa@test.com", CampPriceId = paid.Id,
            });

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        _enrollments.Verify(r => r.AddAsync(It.IsAny<CampEnrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateEnrollmentAsync_WrongCamp_ReturnsNotFound()
    {
        (Guid campId, CampEnrollment e) = ArrangeEditable();
        e.CampId = Guid.NewGuid();

        Result result = await _sut.UpdateEnrollmentAsync(campId, e.Id, _orgId, UpdateRequest());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public async Task UpdateEnrollmentAsync_Cancelled_ReturnsConflict()
    {
        (Guid campId, CampEnrollment e) = ArrangeEditable(Domain.Enums.EnrollmentStatus.Cancelled);

        Result result = await _sut.UpdateEnrollmentAsync(campId, e.Id, _orgId, UpdateRequest());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        e.ParticipantEmail.Should().Be("emma@test.com");
    }

    [Test]
    public async Task UpdateEnrollmentAsync_EmailUsedByOtherEnrollment_ReturnsConflict()
    {
        (Guid campId, CampEnrollment e) = ArrangeEditable();
        _enrollments.Setup(r => r.IsDuplicateExceptAsync(
                campId, "emma.nieuw@test.com", e.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result result = await _sut.UpdateEnrollmentAsync(campId, e.Id, _orgId, UpdateRequest());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        e.ParticipantEmail.Should().Be("emma@test.com");
    }

    [Test]
    public async Task UpdateEnrollmentAsync_RequiredFieldCleared_ReturnsValidation()
    {
        (Guid campId, CampEnrollment e) = ArrangeEditable();
        Guid fieldId = Guid.NewGuid();
        _forms.Setup(f => f.GetByCampIdReadOnlyAsync(campId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampEnrollmentForm
            {
                Fields = [new CampFormField { Id = fieldId, Label = "Shirtmaat", IsRequired = true }],
            });

        Result result = await _sut.UpdateEnrollmentAsync(campId, e.Id, _orgId, UpdateRequest((fieldId, "")));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public async Task UpdateEnrollmentAsync_UpdatesAddsAndRemovesResponses()
    {
        (Guid campId, CampEnrollment e) = ArrangeEditable();
        Guid sizeId = Guid.NewGuid();
        Guid allergyId = Guid.NewGuid();
        Guid noteId = Guid.NewGuid();
        _forms.Setup(f => f.GetByCampIdReadOnlyAsync(campId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampEnrollmentForm
            {
                Fields =
                [
                    new CampFormField { Id = sizeId, Label = "Shirtmaat", IsRequired = true },
                    new CampFormField { Id = allergyId, Label = "Allergieën", IsRequired = false },
                    new CampFormField { Id = noteId, Label = "Opmerking", IsRequired = false },
                ],
            });
        CampFormResponse size = new() { CampEnrollmentId = e.Id, CampFormFieldId = sizeId, Value = "128" };
        CampFormResponse note = new() { CampEnrollmentId = e.Id, CampFormFieldId = noteId, Value = "oud" };
        e.FormResponses = [size, note];
        CampFormResponse? added = null;
        _enrollments.Setup(r => r.AddFormResponseAsync(It.IsAny<CampFormResponse>(), It.IsAny<CancellationToken>()))
            .Callback<CampFormResponse, CancellationToken>((r, _) => added = r)
            .Returns(Task.CompletedTask);

        Result result = await _sut.UpdateEnrollmentAsync(campId, e.Id, _orgId,
            UpdateRequest((sizeId, "140"), (allergyId, "Noten"), (noteId, "")));

        result.IsSuccess.Should().BeTrue();
        size.Value.Should().Be("140");                       // bijgewerkt
        added!.CampFormFieldId.Should().Be(allergyId);       // toegevoegd
        added.Value.Should().Be("Noten");
        _enrollments.Verify(r => r.RemoveFormResponse(note), Times.Once); // gewist
        _enrollments.Verify(r => r.RemoveFormResponse(size), Times.Never);
    }

    [Test]
    public async Task AddGroupMemberAsync_NoFormButResponseGiven_ReturnsValidationAndStoresNothing()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out _, out _);
        AddCampGroupMemberRequest request = new()
        {
            ParticipantName = "Lisa", ParticipantEmail = "lisa@test.com",
            Responses = [new CampFormResponseValueDto { CampFormFieldId = Guid.NewGuid(), Value = "x" }],
        };

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _enrollments.Verify(r => r.AddAsync(It.IsAny<CampEnrollment>(), It.IsAny<CancellationToken>()), Times.Never);
        _enrollments.Verify(r => r.AddFormResponseAsync(It.IsAny<CampFormResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AddGroupMemberAsync_ResponseForFieldOfOtherCamp_ReturnsValidation()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out _, out _);
        _forms.Setup(f => f.GetByCampIdReadOnlyAsync(camp.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampEnrollmentForm
            {
                Fields = [new CampFormField { Id = Guid.NewGuid(), Label = "Shirtmaat", IsRequired = false }],
            });
        AddCampGroupMemberRequest request = new()
        {
            ParticipantName = "Lisa", ParticipantEmail = "lisa@test.com",
            Responses = [new CampFormResponseValueDto { CampFormFieldId = Guid.NewGuid(), Value = "x" }],
        };

        Result<Guid> result = await _sut.AddGroupMemberAsync(camp.Id, group.Id, _orgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public async Task UpdateEnrollmentAsync_NoFormButResponseGiven_ReturnsValidation()
    {
        (Guid campId, CampEnrollment e) = ArrangeEditable();

        Result result = await _sut.UpdateEnrollmentAsync(campId, e.Id, _orgId,
            UpdateRequest((Guid.NewGuid(), "x")));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    // ── Prijsopties ──────────────────────────────────────────────────────────

    private UpdateCampRequest UpdateWithPrices(List<SaveCampPriceRequest> prices) => new(
        "Kamp", null, _clubId, null, 100m, "2026-05-01", "2026-05-01",
        new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), 25, true,
        new List<CreateCampDayRequest> { new("2026-05-01", "10:00", "15:00", new List<CreateCampDayTrainerRequest>()) },
        prices);

    [Test]
    public async Task CreateAsync_WithPrices_StoresThemInOrder()
    {
        Happy();
        CreateCampRequest request = Request() with
        {
            Prices = [new(null, "Leden", "Voor leden", 90m), new(null, "Niet-leden", null, 120m)],
        };

        Result<Guid> result = await _sut.CreateAsync(_orgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _camps.Verify(r => r.AddAsync(
            It.Is<Camp>(c => c.Prices.Count == 2
                && c.Prices.Single(p => p.Label == "Leden").SortOrder == 0
                && c.Prices.Single(p => p.Label == "Niet-leden").SortOrder == 1
                && c.Prices.All(p => p.OrganizationId == _orgId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_Prices_UpdatesAddsAndRemoves()
    {
        Happy();
        Guid campId = Guid.NewGuid();
        CampPrice keep = new() { Id = Guid.NewGuid(), Label = "Oud", Discount = 80m, SortOrder = 0 };
        CampPrice drop = new() { Id = Guid.NewGuid(), Label = "Weg", Discount = 70m, SortOrder = 1 };
        Camp existing = new()
        {
            Id = campId, OrganizationId = _orgId, TennisClubId = _clubId,
            StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 5, 1),
            Prices = { keep, drop },
        };
        _camps.Setup(r => r.GetByIdWithDetailsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        Result result = await _sut.UpdateAsync(campId, _orgId, UpdateWithPrices(
            [new(null, "Nieuw", "Uitleg", 60m), new(keep.Id, "  Leden ", null, 85m)]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.Prices.Should().HaveCount(2);
        keep.Label.Should().Be("Leden");
        keep.Discount.Should().Be(85m);
        keep.SortOrder.Should().Be(1);
        existing.Prices.Single(p => p.Label == "Nieuw").SortOrder.Should().Be(0);
        _camps.Verify(r => r.RemovePrices(It.Is<IEnumerable<CampPrice>>(x => x.Single().Id == drop.Id)), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_MainPriceChanged_RecalculatesPendingCashPayment()
    {
        Happy();
        Guid campId = Guid.NewGuid();
        Camp existing = new()
        {
            Id = campId, OrganizationId = _orgId, TennisClubId = _clubId, Price = 80m,
            StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 5, 1),
        };
        CampEnrollment solo = new() { Id = Guid.NewGuid(), CampId = campId, Status = Domain.Enums.EnrollmentStatus.Confirmed };
        Payment pending = new() { Amount = 80m, Status = Domain.Enums.PaymentStatus.Pending, Method = Domain.Enums.PaymentMethod.Cash };
        _camps.Setup(r => r.GetByIdWithDetailsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _enrollments.Setup(r => r.GetByCampWithResponsesAsync(campId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([solo]);
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(solo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        Result result = await _sut.UpdateAsync(campId, _orgId, UpdateWithPrices([]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        pending.Amount.Should().Be(100m);
    }

    [Test]
    public async Task UpdateAsync_RemovingPriceInUse_ReturnsConflict()
    {
        Happy();
        Guid campId = Guid.NewGuid();
        CampPrice inUse = new() { Id = Guid.NewGuid(), Label = "Leden", Discount = 80m };
        Camp existing = new()
        {
            Id = campId, OrganizationId = _orgId, TennisClubId = _clubId,
            StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 5, 1),
            Prices = { inUse },
        };
        _camps.Setup(r => r.GetByIdWithDetailsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _enrollments.Setup(r => r.AnyUsingPricesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result result = await _sut.UpdateAsync(campId, _orgId, UpdateWithPrices([]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_NullPrices_LeavesExistingPricesUntouched()
    {
        Happy();
        Guid campId = Guid.NewGuid();
        Camp existing = new()
        {
            Id = campId, OrganizationId = _orgId, TennisClubId = _clubId,
            StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 5, 1),
            Prices = { new CampPrice { Id = Guid.NewGuid(), Label = "Leden", Discount = 80m } },
        };
        _camps.Setup(r => r.GetByIdWithDetailsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        Result result = await _sut.UpdateAsync(campId, _orgId, UpdateWithPrices([]) with { Prices = null }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.Prices.Should().HaveCount(1);
    }

    // ── Dagdelen en pakketten ─────────────────────────────────────────────────

    private static List<CreateCampDayPartRequest> Parts() => new()
    {
        new("Ochtend", "09:00", "12:00"),
        new("Namiddag", "13:00", "16:00"),
    };

    private UpdateCampRequest UpdateWithPackages(List<SaveCampPackageRequest> packages, string dayDate = "2026-05-01") => new(
        "Kamp", null, _clubId, null, 100m, "2026-05-01", "2026-05-02",
        new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc), 25, true,
        new List<CreateCampDayRequest>
        {
            new(dayDate, "10:00", "15:00", new List<CreateCampDayTrainerRequest>(), Parts()),
        },
        null, packages);

    private Camp ExistingCamp(Guid campId, params CampPackage[] packages)
    {
        Camp camp = new()
        {
            Id = campId, OrganizationId = _orgId, TennisClubId = _clubId, Price = 100m,
            StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 5, 2),
        };
        foreach (CampPackage p in packages) camp.Packages.Add(p);
        _camps.Setup(r => r.GetByIdWithDetailsAsync(campId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(camp);
        return camp;
    }

    [Test]
    public async Task CreateAsync_WithPartsAndPackages_PersistsThemAndDerivesDayTimes()
    {
        Happy();
        CreateCampRequest request = Request() with
        {
            Days = new List<CreateCampDayRequest>
            {
                new("2026-04-14", "10:00", "11:00", new List<CreateCampDayTrainerRequest>(), Parts()),
            },
            Packages = [new(null, "Ochtenden", "Enkel 's ochtends", 60m, [new("2026-04-14", 0)])],
        };

        Result<Guid> result = await _sut.CreateAsync(_orgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _camps.Verify(r => r.AddAsync(
            It.Is<Camp>(c =>
                c.Days.Single().Parts.Count == 2
                && c.Days.Single().Parts.OrderBy(p => p.Order).First().Name == "Ochtend"
                && c.Days.Single().Parts.All(p => p.OrganizationId == _orgId)
                && c.Days.Single().StartTime == new TimeOnly(9, 0)
                && c.Days.Single().EndTime == new TimeOnly(16, 0)
                && c.Packages.Single().Name == "Ochtenden"
                && c.Packages.Single().Price == 60m
                && c.Packages.Single().OrganizationId == _orgId
                && c.Packages.Single().Slots.Single().PartOrder == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_PackageSlotWithoutMatchingPart_ReturnsValidationError()
    {
        Happy();
        CreateCampRequest request = Request() with
        {
            Days = new List<CreateCampDayRequest>
            {
                new("2026-04-14", "09:00", "16:00", new List<CreateCampDayTrainerRequest>(), Parts()),
            },
            Packages = [new(null, "Woensdag", null, 60m, [new("2026-04-15", 0)])],
        };

        Result<Guid> result = await _sut.CreateAsync(_orgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _camps.Verify(r => r.AddAsync(It.IsAny<Camp>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_PackageSlotPointsToRemovedPart_ReturnsValidationError()
    {
        Happy();
        Guid campId = Guid.NewGuid();
        CampPackage existing = new()
        {
            Id = Guid.NewGuid(), Name = "Dag 2", Price = 50m,
            Slots = { new CampPackageSlot { Date = new DateOnly(2026, 5, 2), PartOrder = 0 } },
        };
        ExistingCamp(campId, existing);

        // De nieuwe dagen bevatten enkel 2026-05-01: het slot van 2026-05-02 bestaat niet meer.
        Result result = await _sut.UpdateAsync(campId, _orgId, UpdateWithPackages(
            [new(existing.Id, "Dag 2", null, 50m, [new("2026-05-02", 0)])]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_NullPackagesButDaysDropAUsedSlot_ReturnsValidationError()
    {
        Happy();
        Guid campId = Guid.NewGuid();
        CampPackage existing = new()
        {
            Id = Guid.NewGuid(), Name = "Dag 2", Price = 50m,
            Slots = { new CampPackageSlot { Date = new DateOnly(2026, 5, 2), PartOrder = 0 } },
        };
        ExistingCamp(campId, existing);

        // Pakketten niet meegestuurd (null = ongewijzigd), maar de nieuwe dagen laten 2026-05-02 vallen.
        Result result = await _sut.UpdateAsync(
            campId, _orgId, UpdateWithPackages([]) with { Packages = null }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public async Task UpdateAsync_RemovingPackageInUse_ReturnsConflict()
    {
        Happy();
        Guid campId = Guid.NewGuid();
        CampPackage inUse = new() { Id = Guid.NewGuid(), Name = "Ochtenden", Price = 60m };
        ExistingCamp(campId, inUse);
        _enrollments.Setup(r => r.AnyUsingPackagesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result result = await _sut.UpdateAsync(campId, _orgId, UpdateWithPackages([]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_Packages_UpdatesAddsAndRemoves()
    {
        Happy();
        Guid campId = Guid.NewGuid();
        CampPackage keep = new()
        {
            Id = Guid.NewGuid(), Name = "Oud", Price = 50m, SortOrder = 0,
            Slots = { new CampPackageSlot { Date = new DateOnly(2026, 5, 1), PartOrder = 0 } },
        };
        CampPackage drop = new() { Id = Guid.NewGuid(), Name = "Weg", Price = 70m, SortOrder = 1 };
        Camp existing = ExistingCamp(campId, keep, drop);

        Result result = await _sut.UpdateAsync(campId, _orgId, UpdateWithPackages(
        [
            new(null, "Nieuw", null, 90m, [new("2026-05-01", 0), new("2026-05-01", 1)]),
            new(keep.Id, " Ochtenden ", "Uitleg", 65m, [new("2026-05-01", 1)]),
        ]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.Packages.Should().HaveCount(2);
        keep.Name.Should().Be("Ochtenden");
        keep.Price.Should().Be(65m);
        keep.SortOrder.Should().Be(1);
        keep.Slots.Should().ContainSingle().Which.PartOrder.Should().Be(1);
        existing.Packages.Single(p => p.Name == "Nieuw").Slots.Should().HaveCount(2);
        _camps.Verify(r => r.RemovePackages(It.Is<IEnumerable<CampPackage>>(x => x.Single().Id == drop.Id)), Times.Once);
        _camps.Verify(r => r.RemovePackageSlots(It.IsAny<IEnumerable<CampPackageSlot>>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_ChangingPackagePrice_RecalculatesPendingCashPayment()
    {
        Happy();
        Guid campId = Guid.NewGuid();
        CampPackage package = new()
        {
            Id = Guid.NewGuid(), Name = "Ochtenden", Price = 50m,
            Slots = { new CampPackageSlot { Date = new DateOnly(2026, 5, 1), PartOrder = 0 } },
        };
        ExistingCamp(campId, package);
        CampEnrollment solo = new()
        {
            Id = Guid.NewGuid(), CampId = campId, CampPackageId = package.Id,
            Status = Domain.Enums.EnrollmentStatus.Confirmed,
        };
        Payment pending = new() { Amount = 50m, Status = Domain.Enums.PaymentStatus.Pending, Method = Domain.Enums.PaymentMethod.Cash };
        _enrollments.Setup(r => r.GetByCampWithResponsesAsync(campId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([solo]);
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(solo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        Result result = await _sut.UpdateAsync(campId, _orgId, UpdateWithPackages(
            [new(package.Id, "Ochtenden", null, 65m, [new("2026-05-01", 0)])]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        pending.Amount.Should().Be(65m);
    }

    private (Camp Camp, CampPrice Option, CampEnrollment Enrollment) ArrangeSoloWithOption()
    {
        Camp camp = new() { Id = Guid.NewGuid(), OrganizationId = _orgId, Name = "Kamp", Price = 100m };
        CampPrice option = new() { Id = Guid.NewGuid(), CampId = camp.Id, Label = "Leden", Discount = 20m };
        camp.Prices.Add(option);
        CampEnrollment e = new()
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, CampId = camp.Id,
            ParticipantName = "Emma", ParticipantEmail = "emma@test.com",
            Status = Domain.Enums.EnrollmentStatus.PendingPayment,
        };
        _camps.Setup(r => r.GetByIdWithDetailsAsync(camp.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(camp);
        _enrollments.Setup(r => r.GetByIdWithResponsesAsync(e.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        return (camp, option, e);
    }

    [Test]
    public async Task UpdateEnrollmentAsync_ChoosingPrice_SetsPriceAndUpdatesPendingCashAmount()
    {
        (Camp camp, CampPrice option, CampEnrollment e) = ArrangeSoloWithOption();
        Payment cash = new()
        {
            Status = Domain.Enums.PaymentStatus.Pending, Method = Domain.Enums.PaymentMethod.Cash, Amount = 100m,
        };
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(e.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cash);

        UpdateCampEnrollmentRequest request = UpdateRequest() with { CampPriceId = option.Id };
        Result result = await _sut.UpdateEnrollmentAsync(camp.Id, e.Id, _orgId, request);

        result.IsSuccess.Should().BeTrue();
        e.CampPriceId.Should().Be(option.Id);
        cash.Amount.Should().Be(80m);
    }

    [Test]
    public async Task UpdateEnrollmentAsync_BackToMainPrice_RestoresMainAmount()
    {
        (Camp camp, CampPrice option, CampEnrollment e) = ArrangeSoloWithOption();
        e.CampPriceId = option.Id;
        Payment cash = new()
        {
            Status = Domain.Enums.PaymentStatus.Pending, Method = Domain.Enums.PaymentMethod.Cash, Amount = 80m,
        };
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(e.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cash);

        Result result = await _sut.UpdateEnrollmentAsync(camp.Id, e.Id, _orgId, UpdateRequest());

        result.IsSuccess.Should().BeTrue();
        e.CampPriceId.Should().BeNull();
        cash.Amount.Should().Be(100m);
    }

    [Test]
    public async Task UpdateEnrollmentAsync_UnknownPrice_ReturnsNotFound()
    {
        (Camp camp, _, CampEnrollment e) = ArrangeSoloWithOption();

        Result result = await _sut.UpdateEnrollmentAsync(
            camp.Id, e.Id, _orgId, UpdateRequest() with { CampPriceId = Guid.NewGuid() });

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
        e.CampPriceId.Should().BeNull();
    }

    [Test]
    public async Task UpdateEnrollmentAsync_ChangingPriceAfterPayment_ReturnsConflict()
    {
        (Camp camp, CampPrice option, CampEnrollment e) = ArrangeSoloWithOption();
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(e.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment { Status = Domain.Enums.PaymentStatus.Paid, Method = Domain.Enums.PaymentMethod.Cash, Amount = 100m });

        Result result = await _sut.UpdateEnrollmentAsync(
            camp.Id, e.Id, _orgId, UpdateRequest() with { CampPriceId = option.Id });

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        e.CampPriceId.Should().BeNull();
    }

    [Test]
    public async Task UpdateEnrollmentAsync_GroupMemberPrice_UpdatesLeadersPendingCashSum()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out CampEnrollment leader, out CampEnrollment member, price: 100m);
        CampPrice option = new() { Id = Guid.NewGuid(), CampId = camp.Id, Label = "Tweede kind", Discount = 30m };
        camp.Prices.Add(option);
        _enrollments.Setup(r => r.GetByIdWithResponsesAsync(member.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        Payment cash = new()
        {
            Status = Domain.Enums.PaymentStatus.Pending, Method = Domain.Enums.PaymentMethod.Cash, Amount = 200m,
        };
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(leader.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cash);

        Result result = await _sut.UpdateEnrollmentAsync(
            camp.Id, member.Id, _orgId, UpdateRequest() with { CampPriceId = option.Id });

        result.IsSuccess.Should().BeTrue();
        cash.Amount.Should().Be(170m); // leider 100 + lid 70
    }

    [Test]
    public async Task AddGroupMemberAsync_WithPrice_UsesThatPriceInGroupAmount()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out CampEnrollment leader, out _, price: 100m);
        CampPrice option = new() { Id = Guid.NewGuid(), CampId = camp.Id, Label = "Tweede kind", Discount = 30m };
        camp.Prices.Add(option);
        Payment cash = new()
        {
            Status = Domain.Enums.PaymentStatus.Pending, Method = Domain.Enums.PaymentMethod.Cash, Amount = 200m,
        };
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(leader.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cash);

        Result<Guid> result = await _sut.AddGroupMemberAsync(
            camp.Id, group.Id, _orgId, MemberRequest() with { CampPriceId = option.Id });

        result.IsSuccess.Should().BeTrue();
        cash.Amount.Should().Be(270m); // 2 bestaande leden x 100 + nieuw lid 70
    }

    [Test]
    public async Task AddGroupMemberAsync_UnknownPrice_ReturnsNotFound()
    {
        Camp camp = ArrangeCampWithGroup(out CampEnrollmentGroup group, out _, out _);

        Result<Guid> result = await _sut.AddGroupMemberAsync(
            camp.Id, group.Id, _orgId, MemberRequest() with { CampPriceId = Guid.NewGuid() });

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }
}
