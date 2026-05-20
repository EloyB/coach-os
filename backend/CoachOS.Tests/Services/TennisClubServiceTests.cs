using CoachOS.Application.Mappings;
using CoachOS.Application.TennisClubs;
using CoachOS.Application.TennisClubs.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class TennisClubServiceTests
{
    private Mock<ITennisClubRepository> _tennisClubRepo = null!;
    private Mock<ILessonSerieRepository> _lessonSeriesRepo = null!;
    private ApplicationMapper _mapper = null!;
    private TennisClubService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _tennisClubRepo = new Mock<ITennisClubRepository>();
        _lessonSeriesRepo = new Mock<ILessonSerieRepository>();
        _mapper = new ApplicationMapper();
        _service = new TennisClubService(_tennisClubRepo.Object, _lessonSeriesRepo.Object, _mapper);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TennisClub BuildClub(string name = "TC Ons Dorp", string address = "Sportlaan 1") =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            Name = name,
            Address = address,
        };

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_ReturnsDtos()
    {
        var club1 = BuildClub("TC Ons Dorp", "Sportlaan 1");
        var club2 = BuildClub("TC De Smasher", "Blauwstraat 12");

        _tennisClubRepo
            .Setup(r => r.GetByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TennisClub> { club1, club2 });

        var result = await _service.GetAllAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Name.Should().Be("TC Ons Dorp");
        result.Value[0].Address.Should().Be("Sportlaan 1");
        result.Value[1].Name.Should().Be("TC De Smasher");
    }

    [Test]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNone()
    {
        _tennisClubRepo
            .Setup(r => r.GetByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TennisClub>());

        var result = await _service.GetAllAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_ReturnsId()
    {
        CreateTennisClubRequest request = new()
        {
            Name = "TC Nieuwe Club",
            Address = "Nieuwstraat 5",
        };

        _tennisClubRepo
            .Setup(r => r.NameExistsAsync(request.Name, OrgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _tennisClubRepo
            .Setup(r => r.AddAsync(It.IsAny<TennisClub>(), It.IsAny<CancellationToken>()))
            .Callback<TennisClub, CancellationToken>((c, _) => c.Id = Guid.NewGuid())
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(OrgId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _tennisClubRepo.Verify(
            r => r.AddAsync(
                It.Is<TennisClub>(c => c.Name == "TC Nieuwe Club" && c.OrganizationId == OrgId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreateAsync_ReturnsConflict_WhenNameAlreadyExists()
    {
        CreateTennisClubRequest request = new()
        {
            Name = "TC De Aces",
            Address = "Nieuwstraat 5",
        };

        _tennisClubRepo
            .Setup(r => r.NameExistsAsync(request.Name, OrgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(OrgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
        _tennisClubRepo.Verify(r => r.AddAsync(It.IsAny<TennisClub>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_Succeeds_WhenNotInUse()
    {
        var club = BuildClub();

        _tennisClubRepo
            .Setup(r => r.GetByIdAsync(club.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(club);

        _lessonSeriesRepo
            .Setup(r => r.AnyByTennisClubAsync(club.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.DeleteAsync(club.Id, OrgId);

        result.IsSuccess.Should().BeTrue();
        _tennisClubRepo.Verify(r => r.DeleteAsync(club, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_ReturnsNotFound_WhenMissing()
    {
        var missingId = Guid.NewGuid();

        _tennisClubRepo
            .Setup(r => r.GetByIdAsync(missingId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TennisClub?)null);

        var result = await _service.DeleteAsync(missingId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
        _tennisClubRepo.Verify(r => r.DeleteAsync(It.IsAny<TennisClub>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAsync_ReturnsConflict_WhenInUse()
    {
        var club = BuildClub();

        _tennisClubRepo
            .Setup(r => r.GetByIdAsync(club.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(club);

        _lessonSeriesRepo
            .Setup(r => r.AnyByTennisClubAsync(club.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.DeleteAsync(club.Id, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
        _tennisClubRepo.Verify(r => r.DeleteAsync(It.IsAny<TennisClub>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
