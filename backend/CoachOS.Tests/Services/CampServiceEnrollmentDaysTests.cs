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
public class CampServiceEnrollmentDaysTests
{
    private Mock<ICampRepository> _camps = null!;
    private Mock<ICampEnrollmentRepository> _enrollments = null!;
    private Mock<ICampEnrollmentFormRepository> _forms = null!;
    private Mock<IPaymentRepository> _payments = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CampService _sut = null!;

    private readonly Guid _orgId = Guid.NewGuid();
    private Camp _camp = null!;
    private CampDay _monday = null!, _tuesday = null!;

    [SetUp]
    public void SetUp()
    {
        _camps = new Mock<ICampRepository>();
        _enrollments = new Mock<ICampEnrollmentRepository>();
        _forms = new Mock<ICampEnrollmentFormRepository>();
        _payments = new Mock<IPaymentRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _sut = new CampService(_camps.Object, _enrollments.Object, _forms.Object,
            new Mock<ITennisClubRepository>().Object, new Mock<IUserLookupService>().Object, _payments.Object,
            _unitOfWork.Object, new Mock<IPaymentService>().Object);

        _camp = new Camp
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, Name = "Kamp",
            DayPrice = 30m, HalfDayPrice = 20m, AllowHalfDays = true, MaxParticipants = 5,
        };
        _monday = Day(new DateOnly(2026, 12, 21));
        _tuesday = Day(new DateOnly(2026, 12, 22));
        _camp.Days.Add(_monday);
        _camp.Days.Add(_tuesday);
        _camps.Setup(r => r.GetByIdWithDetailsAsync(_camp.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(_camp);
        _forms.Setup(r => r.GetByCampIdReadOnlyAsync(_camp.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CampEnrollmentForm?)null);
        SetCounts(new Dictionary<(Guid, CampDayPart), int>());
    }

    private CampDay Day(DateOnly date) => new()
    {
        Id = Guid.NewGuid(), CampId = _camp.Id, OrganizationId = _orgId, Date = date,
        MorningStart = new TimeOnly(9, 15), MorningEnd = new TimeOnly(12, 0),
        AfternoonStart = new TimeOnly(13, 15), AfternoonEnd = new TimeOnly(16, 0),
    };

    private void SetCounts(Dictionary<(Guid, CampDayPart), int> counts)
        => _enrollments.Setup(r => r.CountActivePerDayPartAsync(_camp.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counts.ToDictionary(kv => (kv.Key.Item1, kv.Key.Item2), kv => kv.Value));

    private CampEnrollment Enrolled(string email, EnrollmentStatus status, decimal total, params CampEnrollmentDay[] days)
    {
        CampEnrollment e = new()
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, CampId = _camp.Id, ParticipantName = "Emma",
            ParticipantEmail = email, Status = status, TotalAmount = total,
        };
        foreach (CampEnrollmentDay d in days) e.Days.Add(d);
        _enrollments.Setup(r => r.GetByIdWithResponsesAsync(e.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        _enrollments.Setup(r => r.IsDuplicateExceptAsync(_camp.Id, email, e.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        return e;
    }

    private static CampDaySelectionDto Sel(Guid dayId, bool morning, bool afternoon) => new(dayId, morning, afternoon);

    private static CampEnrollmentDay Chosen(CampDay day, bool morning, bool afternoon)
        => new() { Id = Guid.NewGuid(), CampDayId = day.Id, Morning = morning, Afternoon = afternoon };

    private UpdateCampEnrollmentRequest UpdateTo(string email, params CampDaySelectionDto[] days) => new()
    {
        ParticipantName = "Emma", ParticipantEmail = email, Days = days.ToList(),
    };

    // ── UpdateEnrollmentAsync ────────────────────────────────────────────────

    [Test]
    public async Task UpdateEnrollment_ChangingDays_RecalculatesTotalAndPendingCashPayment()
    {
        CampEnrollment enrollment = Enrolled("emma@example.com", EnrollmentStatus.PendingPayment, 30m, Chosen(_monday, true, true));
        Payment cash = new() { Amount = 30m, Status = PaymentStatus.Pending, Method = PaymentMethod.Cash };
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(enrollment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cash);

        Result result = await _sut.UpdateEnrollmentAsync(_camp.Id, enrollment.Id, _orgId,
            UpdateTo("emma@example.com", Sel(_monday.Id, true, false), Sel(_tuesday.Id, true, true)));

        result.IsSuccess.Should().BeTrue();
        enrollment.TotalAmount.Should().Be(50m);
        cash.Amount.Should().Be(50m);
        enrollment.Days.Should().HaveCount(2);
        _enrollments.Verify(r => r.RemoveEnrollmentDays(It.IsAny<IEnumerable<CampEnrollmentDay>>()), Times.Once);
    }

    [Test]
    public async Task UpdateEnrollment_PaidPayment_ReturnsConflictAndKeepsDays()
    {
        CampEnrollment enrollment = Enrolled("emma@example.com", EnrollmentStatus.Confirmed, 30m, Chosen(_monday, true, true));
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(enrollment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment { Amount = 30m, Status = PaymentStatus.Paid, Method = PaymentMethod.Cash });

        Result result = await _sut.UpdateEnrollmentAsync(_camp.Id, enrollment.Id, _orgId,
            UpdateTo("emma@example.com", Sel(_monday.Id, true, false)));

        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        enrollment.TotalAmount.Should().Be(30m);
        enrollment.Days.Single().Afternoon.Should().BeTrue();
    }

    [Test]
    public async Task UpdateEnrollment_PartThatIsFullForOthers_ReturnsConflict()
    {
        _camp.MaxParticipants = 1;
        CampEnrollment enrollment = Enrolled("emma@example.com", EnrollmentStatus.PendingPayment, 20m, Chosen(_monday, true, false));
        SetCounts(new() { [(_monday.Id, CampDayPart.Morning)] = 1, [(_tuesday.Id, CampDayPart.Afternoon)] = 1 });

        Result result = await _sut.UpdateEnrollmentAsync(_camp.Id, enrollment.Id, _orgId,
            UpdateTo("emma@example.com", Sel(_monday.Id, true, false), Sel(_tuesday.Id, false, true)));

        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict && e.Message.Contains("namiddag"));
    }

    [Test]
    public async Task UpdateEnrollment_OwnPartsDoNotCountAgainstCapacity()
    {
        _camp.MaxParticipants = 1;
        CampEnrollment enrollment = Enrolled("emma@example.com", EnrollmentStatus.PendingPayment, 20m, Chosen(_monday, true, false));
        SetCounts(new() { [(_monday.Id, CampDayPart.Morning)] = 1 });

        Result result = await _sut.UpdateEnrollmentAsync(_camp.Id, enrollment.Id, _orgId,
            UpdateTo("emma@example.com", Sel(_monday.Id, true, false)));

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UpdateEnrollment_InvalidSelection_ReturnsValidation()
    {
        CampEnrollment enrollment = Enrolled("emma@example.com", EnrollmentStatus.PendingPayment, 30m, Chosen(_monday, true, true));

        Result result = await _sut.UpdateEnrollmentAsync(_camp.Id, enrollment.Id, _orgId,
            UpdateTo("emma@example.com", Sel(Guid.NewGuid(), true, true)));

        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
    }

    // ── AddGroupMemberAsync ──────────────────────────────────────────────────

    [Test]
    public async Task AddGroupMember_CopiesLeaderDaysAndUpdatesPendingCashPayment()
    {
        CampEnrollment leader = Enrolled("leader@example.com", EnrollmentStatus.PendingPayment, 50m,
            Chosen(_monday, true, true), Chosen(_tuesday, true, false));
        CampEnrollmentGroup group = new() { Id = Guid.NewGuid(), CampId = _camp.Id, OrganizationId = _orgId, LeaderEnrollmentId = leader.Id, Members = [leader] };
        _enrollments.Setup(r => r.GetGroupWithMembersAsync(group.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        Payment cash = new() { Amount = 50m, Status = PaymentStatus.Pending, Method = PaymentMethod.Cash };
        _payments.Setup(p => p.GetLatestTrackedByCampEnrollmentIdAsync(leader.Id, It.IsAny<CancellationToken>())).ReturnsAsync(cash);
        _enrollments.Setup(r => r.IsDuplicateAsync(_camp.Id, "lid@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        CampEnrollment? added = null;
        _enrollments.Setup(r => r.AddAsync(It.IsAny<CampEnrollment>(), It.IsAny<CancellationToken>()))
            .Callback<CampEnrollment, CancellationToken>((e, _) => added = e).Returns(Task.CompletedTask);

        Result<Guid> result = await _sut.AddGroupMemberAsync(_camp.Id, group.Id, _orgId,
            new AddCampGroupMemberRequest { ParticipantName = "Lid", ParticipantEmail = "lid@example.com" });

        result.IsSuccess.Should().BeTrue();
        added!.TotalAmount.Should().Be(50m);
        added.Days.Should().HaveCount(2);
        added.Days.Single(d => d.CampDayId == _tuesday.Id).Afternoon.Should().BeFalse();
        cash.Amount.Should().Be(100m);
    }

    [Test]
    public async Task AddGroupMember_WhenLeadersPartIsFull_ReturnsConflict()
    {
        _camp.MaxParticipants = 1;
        CampEnrollment leader = Enrolled("leader@example.com", EnrollmentStatus.PendingPayment, 20m, Chosen(_monday, true, false));
        CampEnrollmentGroup group = new() { Id = Guid.NewGuid(), CampId = _camp.Id, OrganizationId = _orgId, LeaderEnrollmentId = leader.Id, Members = [leader] };
        _enrollments.Setup(r => r.GetGroupWithMembersAsync(group.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        SetCounts(new() { [(_monday.Id, CampDayPart.Morning)] = 1 });

        Result<Guid> result = await _sut.AddGroupMemberAsync(_camp.Id, group.Id, _orgId,
            new AddCampGroupMemberRequest { ParticipantName = "Lid", ParticipantEmail = "lid@example.com" });

        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
    }

    // ── GetEnrollmentsAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetEnrollments_ExposesChosenDaysAndTotal()
    {
        CampEnrollment enrollment = Enrolled("emma@example.com", EnrollmentStatus.Confirmed, 50m,
            Chosen(_monday, true, true), Chosen(_tuesday, false, true));
        _camps.Setup(r => r.ExistsAsync(_camp.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _enrollments.Setup(r => r.GetByCampWithResponsesAsync(_camp.Id, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([enrollment]);
        _payments.Setup(r => r.GetLatestByCampEnrollmentIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Result<List<CampEnrollmentDto>> result = await _sut.GetEnrollmentsAsync(_camp.Id, _orgId);

        CampEnrollmentDto dto = result.Value!.Single();
        dto.TotalAmount.Should().Be(50m);
        dto.Days.Should().BeEquivalentTo(new[]
        {
            new CampDaySelectionDto(_monday.Id, true, true), new CampDaySelectionDto(_tuesday.Id, false, true),
        });
    }
}
