using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Entities;
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
        _sut = new CampService(_camps.Object, _enrollments.Object, _forms.Object, _clubs.Object, _users.Object);
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
        _camps.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
}
