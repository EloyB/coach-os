using CoachOS.Application.Mappings;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class TrainerAvailabilityServiceTests
{
    private Mock<ITrainerAvailabilityRepository> _repo = null!;
    private Mock<ITennisClubRepository> _clubRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private ApplicationMapper _mapper = null!;
    private TrainerAvailabilityService _sut = null!;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _trainerId = Guid.NewGuid();
    private readonly Guid _clubId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ITrainerAvailabilityRepository>();
        _clubRepo = new Mock<ITennisClubRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _mapper = new ApplicationMapper();
        _sut = new TrainerAvailabilityService(_repo.Object, _clubRepo.Object, _userLookup.Object, _mapper);
    }

    private CreateTrainerAvailabilityRequest ValidRequest() =>
        new(_trainerId, _clubId, 0, "17:00", "21:00");

    private void SetupHappyPath()
    {
        _clubRepo.Setup(r => r.ExistsAsync(_clubId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _userLookup.Setup(r => r.IsActiveTrainerAsync(_trainerId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repo.Setup(r => r.HasOverlapAsync(_trainerId, _orgId, 0, It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Test]
    public async Task CreateAsync_ValidRequest_AddsAndReturnsId()
    {
        SetupHappyPath();

        Result<Guid> result = await _sut.CreateAsync(_orgId, ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _repo.Verify(r => r.AddAsync(
            It.Is<TrainerAvailability>(a =>
                a.OrganizationId == _orgId
                && a.TrainerId == _trainerId
                && a.TennisClubId == _clubId
                && a.DayOfWeek == 0
                && a.StartTime == new TimeOnly(17, 0)
                && a.EndTime == new TimeOnly(21, 0)
                && a.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_NoClub_SkipsClubCheckAndSucceeds()
    {
        SetupHappyPath();
        CreateTrainerAvailabilityRequest request = ValidRequest() with { TennisClubId = null };

        Result<Guid> result = await _sut.CreateAsync(_orgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _clubRepo.Verify(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.AddAsync(
            It.Is<TrainerAvailability>(a => a.TennisClubId == null && a.TrainerId == _trainerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_ClubNotInOrganization_ReturnsNotFound()
    {
        SetupHappyPath();
        _clubRepo.Setup(r => r.ExistsAsync(_clubId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Result<Guid> result = await _sut.CreateAsync(_orgId, ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        _repo.Verify(r => r.AddAsync(It.IsAny<TrainerAvailability>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_NotAnActiveTrainer_ReturnsNotFound()
    {
        SetupHappyPath();
        _userLookup.Setup(r => r.IsActiveTrainerAsync(_trainerId, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Result<Guid> result = await _sut.CreateAsync(_orgId, ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        _repo.Verify(r => r.AddAsync(It.IsAny<TrainerAvailability>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_OverlappingAvailability_ReturnsConflict()
    {
        SetupHappyPath();
        _repo.Setup(r => r.HasOverlapAsync(_trainerId, _orgId, 0, It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<Guid> result = await _sut.CreateAsync(_orgId, ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        _repo.Verify(r => r.AddAsync(It.IsAny<TrainerAvailability>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        TrainerAvailability entity = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            TrainerId = _trainerId,
            TennisClubId = _clubId,
            DayOfWeek = 3,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(22, 0),
            IsActive = true,
            TennisClub = new TennisClub { Name = "TC Demo" },
        };
        _repo.Setup(r => r.GetByOrganizationAsync(_orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrainerAvailability> { entity });

        Result<List<TrainerAvailabilityDto>> result = await _sut.GetAllAsync(_orgId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].TennisClubName.Should().Be("TC Demo");
        result.Value![0].StartTime.Should().Be("18:00");
    }

    [Test]
    public async Task DeleteAsync_Existing_SoftDeletes()
    {
        TrainerAvailability entity = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = _orgId,
            TrainerId = _trainerId,
            TennisClubId = _clubId,
            IsActive = true,
        };
        _repo.Setup(r => r.GetByIdAsync(entity.Id, _orgId, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        Result result = await _sut.DeleteAsync(entity.Id, _orgId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        entity.IsActive.Should().BeFalse();
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_NotFound_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrainerAvailability?)null);

        Result result = await _sut.DeleteAsync(Guid.NewGuid(), _orgId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
