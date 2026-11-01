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
public class CampServiceDayPartsTests
{
    private Mock<ICampRepository> _camps = null!;
    private Mock<ICampEnrollmentRepository> _enrollments = null!;
    private Mock<ITennisClubRepository> _clubs = null!;
    private Mock<IUserLookupService> _users = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CampService _sut = null!;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _clubId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _camps = new Mock<ICampRepository>();
        _enrollments = new Mock<ICampEnrollmentRepository>();
        _clubs = new Mock<ITennisClubRepository>();
        _users = new Mock<IUserLookupService>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _clubs.Setup(r => r.ExistsAsync(_clubId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _sut = new CampService(_camps.Object, _enrollments.Object, new Mock<ICampEnrollmentFormRepository>().Object,
            _clubs.Object, _users.Object, new Mock<IPaymentRepository>().Object,
            _unitOfWork.Object, new Mock<IPaymentService>().Object);
    }

    private static CreateCampDayRequest DayRequest(Guid? id, string date, bool morning = true, bool afternoon = true)
        => new(id, date,
            morning ? "09:15" : null, morning ? "12:00" : null,
            afternoon ? "13:15" : null, afternoon ? "16:00" : null,
            []);

    private UpdateCampRequest Update(bool allowHalfDays, params CreateCampDayRequest[] days) => new(
        "Kamp", null, _clubId, null, 32.5m, 20m, allowHalfDays, "2026-12-21", "2026-12-24",
        new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), 12, true, days.ToList());

    private Camp ExistingCamp(out CampDay monday, out CampDay tuesday)
    {
        Camp camp = new()
        {
            Id = Guid.NewGuid(), OrganizationId = _orgId, TennisClubId = _clubId, Name = "Kamp",
            DayPrice = 32.5m, HalfDayPrice = 20m, AllowHalfDays = true,
        };
        monday = NewDay(camp, new DateOnly(2026, 12, 21));
        tuesday = NewDay(camp, new DateOnly(2026, 12, 22));
        camp.Days.Add(monday);
        camp.Days.Add(tuesday);
        _camps.Setup(r => r.GetByIdWithDetailsAsync(camp.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(camp);
        return camp;
    }

    private CampDay NewDay(Camp camp, DateOnly date) => new()
    {
        Id = Guid.NewGuid(), CampId = camp.Id, OrganizationId = _orgId, Date = date,
        MorningStart = new TimeOnly(9, 15), MorningEnd = new TimeOnly(12, 0),
        AfternoonStart = new TimeOnly(13, 15), AfternoonEnd = new TimeOnly(16, 0),
    };

    [Test]
    public async Task CreateAsync_StoresPricesToggleAndDayParts()
    {
        CreateCampRequest request = new(
            "Kerstkamp", null, _clubId, null, 32.5m, 20m, true, "2026-12-21", "2026-12-22",
            new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), 12,
            [DayRequest(null, "2026-12-21"), DayRequest(null, "2026-12-22", afternoon: false)]);

        Result<Guid> result = await _sut.CreateAsync(_orgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _camps.Verify(r => r.AddAsync(
            It.Is<Camp>(c => c.DayPrice == 32.5m && c.HalfDayPrice == 20m && c.AllowHalfDays && c.MaxParticipants == 12
                && c.Days.Count == 2
                && c.Days.Single(d => d.Date == new DateOnly(2026, 12, 22)).AfternoonStart == null
                && c.Days.Single(d => d.Date == new DateOnly(2026, 12, 21)).AfternoonEnd == new TimeOnly(16, 0)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_ReturnsPricesToggleAndParts()
    {
        Camp camp = ExistingCamp(out CampDay monday, out _);
        monday.AfternoonStart = null;
        monday.AfternoonEnd = null;
        _enrollments.Setup(r => r.CountActiveByCampAsync(camp.Id, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        Result<CampDetailDto> result = await _sut.GetByIdAsync(camp.Id, _orgId, CancellationToken.None);

        result.Value!.DayPrice.Should().Be(32.5m);
        result.Value.HalfDayPrice.Should().Be(20m);
        result.Value.AllowHalfDays.Should().BeTrue();
        CampDayDto dto = result.Value.Days.Single(d => d.Id == monday.Id);
        dto.MorningStart.Should().Be("09:15");
        dto.AfternoonStart.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_RemovingDayWithEnrollments_ReturnsConflict()
    {
        Camp camp = ExistingCamp(out CampDay monday, out CampDay tuesday);
        _camps.Setup(r => r.DayHasEnrollmentsAsync(tuesday.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Result result = await _sut.UpdateAsync(camp.Id, _orgId,
            Update(true, DayRequest(monday.Id, "2026-12-21")), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_RemovingPartThatIsChosen_ReturnsConflict()
    {
        Camp camp = ExistingCamp(out CampDay monday, out CampDay tuesday);
        _camps.Setup(r => r.AnyEnrollmentUsesPartAsync(monday.Id, CampDayPart.Afternoon, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result result = await _sut.UpdateAsync(camp.Id, _orgId,
            Update(true, DayRequest(monday.Id, "2026-12-21", afternoon: false), DayRequest(tuesday.Id, "2026-12-22")),
            CancellationToken.None);

        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task UpdateAsync_DayWithoutEnrollments_CanBeRemovedAndPartsChanged()
    {
        Camp camp = ExistingCamp(out CampDay monday, out CampDay tuesday);

        Result result = await _sut.UpdateAsync(camp.Id, _orgId,
            Update(true, DayRequest(monday.Id, "2026-12-21", afternoon: false)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _camps.Verify(r => r.RemoveDays(It.Is<IEnumerable<CampDay>>(d => d.Single().Id == tuesday.Id)), Times.Once);
        camp.Days.Should().ContainSingle();
        camp.Days.Single().Id.Should().Be(monday.Id);
        camp.Days.Single().AfternoonStart.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_AddsNewDayWithoutId()
    {
        Camp camp = ExistingCamp(out CampDay monday, out CampDay tuesday);

        Result result = await _sut.UpdateAsync(camp.Id, _orgId,
            Update(true, DayRequest(monday.Id, "2026-12-21"), DayRequest(tuesday.Id, "2026-12-22"),
                DayRequest(null, "2026-12-23")), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        camp.Days.Should().HaveCount(3);
    }

    [Test]
    public async Task UpdateAsync_PriceChange_LeavesExistingEnrollmentAmountsUntouched()
    {
        Camp camp = ExistingCamp(out CampDay monday, out CampDay tuesday);
        CampEnrollment enrollment = new() { Id = Guid.NewGuid(), CampId = camp.Id, TotalAmount = 65m };
        camp.Enrollments.Add(enrollment);
        UpdateCampRequest request = Update(true, DayRequest(monday.Id, "2026-12-21"), DayRequest(tuesday.Id, "2026-12-22"))
            with { DayPrice = 40m, HalfDayPrice = 25m };

        Result result = await _sut.UpdateAsync(camp.Id, _orgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        camp.DayPrice.Should().Be(40m);
        enrollment.TotalAmount.Should().Be(65m);
    }

    [Test]
    public async Task UpdateAsync_DisablingHalfDaysWithHalfDayEnrollments_ReturnsConflict()
    {
        Camp camp = ExistingCamp(out CampDay monday, out CampDay tuesday);
        _camps.Setup(r => r.AnyHalfDayEnrollmentAsync(camp.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Result result = await _sut.UpdateAsync(camp.Id, _orgId,
            Update(false, DayRequest(monday.Id, "2026-12-21"), DayRequest(tuesday.Id, "2026-12-22")),
            CancellationToken.None);

        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
    }
}
