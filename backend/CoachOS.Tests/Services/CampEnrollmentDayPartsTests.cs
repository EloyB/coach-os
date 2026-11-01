using CoachOS.Application.Abstractions;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.MollieConnect;
using CoachOS.Application.MollieConnect.DTOs;
using CoachOS.Application.Payments;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class CampEnrollmentDayPartsTests
{
    private Mock<ICampRepository> _camps = null!;
    private Mock<ICampEnrollmentRepository> _enrollments = null!;
    private Mock<ICampEnrollmentFormRepository> _forms = null!;
    private Mock<IPaymentService> _payments = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IMollieConnectService> _mollie = null!;
    private CampEnrollmentService _sut = null!;

    private readonly Guid _orgId = Guid.NewGuid();
    private Camp _camp = null!;
    private CampDay _monday = null!, _tuesday = null!, _wednesday = null!;

    [SetUp]
    public void SetUp()
    {
        _camps = new Mock<ICampRepository>();
        _enrollments = new Mock<ICampEnrollmentRepository>();
        _forms = new Mock<ICampEnrollmentFormRepository>();
        _payments = new Mock<IPaymentService>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _mollie = new Mock<IMollieConnectService>();
        _sut = new CampEnrollmentService(_camps.Object, _enrollments.Object, _forms.Object, _payments.Object,
            new Mock<IPaymentRepository>().Object, _unitOfWork.Object, _mollie.Object,
            new Mock<IEmailService>().Object, NullLogger<CampEnrollmentService>.Instance);

        _camp = new Camp
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, Name = "Kerstkamp",
            DayPrice = 32.5m, HalfDayPrice = 20m, AllowHalfDays = true, MaxParticipants = 10,
            StartDate = new DateOnly(2026, 12, 21), EndDate = new DateOnly(2026, 12, 23),
            RegistrationDeadline = DateTime.UtcNow.AddDays(10), IsActive = true,
        };
        _monday = Day(new DateOnly(2026, 12, 21));
        _tuesday = Day(new DateOnly(2026, 12, 22));
        _wednesday = Day(new DateOnly(2026, 12, 23));
        foreach (CampDay d in new[] { _monday, _tuesday, _wednesday }) _camp.Days.Add(d);

        _camps.Setup(r => r.GetByIdPublicAsync(_camp.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_camp);
        _forms.Setup(r => r.GetByCampIdReadOnlyAsync(_camp.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CampEnrollmentForm?)null);
        _enrollments.Setup(r => r.IsDuplicateAsync(_camp.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        SetCounts(new Dictionary<(Guid, CampDayPart), int>());
    }

    private static CampDaySelectionDto Sel(Guid dayId, bool morning, bool afternoon) => new(dayId, morning, afternoon);

    private CampDay Day(DateOnly date) => new()
    {
        Id = Guid.NewGuid(), CampId = _camp.Id, OrganizationId = _orgId, Date = date,
        MorningStart = new TimeOnly(9, 15), MorningEnd = new TimeOnly(12, 0),
        AfternoonStart = new TimeOnly(13, 15), AfternoonEnd = new TimeOnly(16, 0),
    };

    private void SetCounts(Dictionary<(Guid, CampDayPart), int> counts)
        => _enrollments.Setup(r => r.CountActivePerDayPartAsync(_camp.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counts.ToDictionary(kv => (kv.Key.Item1, kv.Key.Item2), kv => kv.Value));

    private SubmitCampEnrollmentRequest Request(params CampDaySelectionDto[] days) => new()
    {
        ParticipantName = "Emma", ParticipantEmail = "emma@example.com", EnrollmentType = "solo", Days = days.ToList(),
    };

    private List<CampEnrollment> CaptureAdded()
    {
        List<CampEnrollment> added = [];
        _enrollments.Setup(r => r.AddAsync(It.IsAny<CampEnrollment>(), It.IsAny<CancellationToken>()))
            .Callback<CampEnrollment, CancellationToken>((e, _) => added.Add(e)).Returns(Task.CompletedTask);
        return added;
    }

    [Test]
    public async Task Submit_MixedDays_StoresDaysAndTotalAmount()
    {
        List<CampEnrollment> added = CaptureAdded();

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_camp.Id,
            Request(Sel(_monday.Id, true, true), Sel(_tuesday.Id, true, true), Sel(_wednesday.Id, true, false)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RequiresPayment.Should().BeTrue();
        CampEnrollment saved = added.Single();
        saved.TotalAmount.Should().Be(85m);
        saved.Status.Should().Be(EnrollmentStatus.PendingPayment);
        saved.Days.Should().HaveCount(3);
        saved.Days.Single(d => d.CampDayId == _wednesday.Id).Afternoon.Should().BeFalse();
        saved.Days.Should().OnlyContain(d => d.OrganizationId == _orgId);
    }

    [Test]
    public async Task Submit_FreeCamp_ConfirmsImmediately()
    {
        _camp.DayPrice = 0m;
        _camp.HalfDayPrice = 0m;
        List<CampEnrollment> added = CaptureAdded();

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_camp.Id,
            Request(Sel(_monday.Id, true, true)), CancellationToken.None);

        result.Value!.RequiresPayment.Should().BeFalse();
        added.Single().Status.Should().Be(EnrollmentStatus.Confirmed);
        added.Single().TotalAmount.Should().Be(0m);
    }

    [Test]
    public async Task Submit_FullDayWhileMorningIsFull_ReturnsConflictNamingDayAndPart()
    {
        _camp.MaxParticipants = 1;
        SetCounts(new() { [(_monday.Id, CampDayPart.Morning)] = 1 });
        List<CampEnrollment> added = CaptureAdded();

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_camp.Id,
            Request(Sel(_tuesday.Id, true, true), Sel(_monday.Id, true, true)), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict
            && e.Message.Contains("21/12/2026") && e.Message.Contains("voormiddag"));
        added.Should().BeEmpty();
    }

    [Test]
    public async Task Submit_AfternoonOnlyWhileMorningIsFull_Succeeds()
    {
        _camp.MaxParticipants = 1;
        SetCounts(new() { [(_monday.Id, CampDayPart.Morning)] = 1 });
        CaptureAdded();

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_camp.Id,
            Request(Sel(_monday.Id, false, true)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Submit_GroupLargerThanRemainingPlaces_ReturnsConflict()
    {
        _camp.MaxParticipants = 3;
        SetCounts(new() { [(_monday.Id, CampDayPart.Morning)] = 1 });
        SubmitCampEnrollmentRequest request = Request(Sel(_monday.Id, true, false)) with
        {
            EnrollmentType = "group",
            GroupMembers =
            [
                new() { ParticipantName = "A", ParticipantEmail = "a@example.com" },
                new() { ParticipantName = "B", ParticipantEmail = "b@example.com" },
            ],
        };

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_camp.Id, request, CancellationToken.None);

        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task Submit_Group_EveryMemberGetsTheSameDaysAndOwnTotal()
    {
        List<CampEnrollment> added = CaptureAdded();
        SubmitCampEnrollmentRequest request = Request(Sel(_monday.Id, true, true), Sel(_tuesday.Id, true, false)) with
        {
            EnrollmentType = "group",
            GroupMembers =
            [
                new() { ParticipantName = "A", ParticipantEmail = "a@example.com" },
                new() { ParticipantName = "B", ParticipantEmail = "b@example.com" },
            ],
        };

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_camp.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added.Should().HaveCount(3);
        added.Should().OnlyContain(e => e.TotalAmount == 52.5m && e.Days.Count == 2);
    }

    [TestCase("other-camp-day")]
    [TestCase("no-days")]
    [TestCase("missing-part")]
    public async Task Submit_InvalidSelection_ReturnsValidationAndStoresNothing(string scenario)
    {
        List<CampEnrollment> added = CaptureAdded();
        _monday.AfternoonStart = null;
        _monday.AfternoonEnd = null;
        SubmitCampEnrollmentRequest request = scenario switch
        {
            "other-camp-day" => Request(Sel(Guid.NewGuid(), true, true)),
            "no-days" => Request(),
            _ => Request(Sel(_monday.Id, false, true)),
        };

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_camp.Id, request, CancellationToken.None);

        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
        added.Should().BeEmpty();
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<System.Data.IsolationLevel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetPublicCamp_MarksFullParts()
    {
        _camp.MaxParticipants = 2;
        SetCounts(new() { [(_monday.Id, CampDayPart.Morning)] = 2, [(_monday.Id, CampDayPart.Afternoon)] = 1 });
        _enrollments.Setup(r => r.CountActiveByCampAsync(_camp.Id, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        Result<PublicCampDto> result = await _sut.GetPublicCampAsync(_camp.Id, CancellationToken.None);

        PublicCampDayDto monday = result.Value!.Days.Single(d => d.Id == _monday.Id);
        monday.MorningFull.Should().BeTrue();
        monday.AfternoonFull.Should().BeFalse();
        result.Value.Days.Single(d => d.Id == _tuesday.Id).MorningFull.Should().BeFalse();
        result.Value.DayPrice.Should().Be(32.5m);
    }

    [Test]
    public async Task GetPaymentOptions_ReturnsTotalOfTheGroupWithoutCancelledMembers()
    {
        Guid groupId = Guid.NewGuid();
        CampEnrollmentGroup group = new() { Id = groupId };
        CampEnrollment leader = new() { Id = Guid.NewGuid(), CampId = _camp.Id, CampEnrollmentGroupId = groupId, Group = group, TotalAmount = 52.5m, Status = EnrollmentStatus.PendingPayment };
        CampEnrollment member = new() { Id = Guid.NewGuid(), CampId = _camp.Id, CampEnrollmentGroupId = groupId, Group = group, TotalAmount = 52.5m, Status = EnrollmentStatus.PendingPayment };
        CampEnrollment cancelled = new() { Id = Guid.NewGuid(), CampId = _camp.Id, CampEnrollmentGroupId = groupId, Group = group, TotalAmount = 52.5m, Status = EnrollmentStatus.Cancelled };
        group.Members = [leader, member, cancelled];
        _enrollments.Setup(r => r.GetByIdWithGroupAsync(leader.Id, It.IsAny<CancellationToken>())).ReturnsAsync(leader);
        _mollie.Setup(m => m.GetStatusAsync(_orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MollieConnectionStatusDto>.Ok(new MollieConnectionStatusDto(false, null, null)));

        Result<CampPaymentOptionsDto> result = await _sut.GetPaymentOptionsAsync(_camp.Id, leader.Id, CancellationToken.None);

        result.Value!.Price.Should().Be(105m);
    }

    [Test]
    public async Task GetPaymentOptions_EnrollmentOfAnotherCamp_ReturnsNotFound()
    {
        CampEnrollment other = new() { Id = Guid.NewGuid(), CampId = Guid.NewGuid() };
        _enrollments.Setup(r => r.GetByIdWithGroupAsync(other.Id, It.IsAny<CancellationToken>())).ReturnsAsync(other);

        Result<CampPaymentOptionsDto> result = await _sut.GetPaymentOptionsAsync(_camp.Id, other.Id, CancellationToken.None);

        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
    }
}
