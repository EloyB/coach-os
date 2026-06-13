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
        _camps.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        existing.Days.Should().HaveCount(1);
        existing.Days.First().Date.Should().Be(new DateOnly(2026, 5, 1));
    }
}
