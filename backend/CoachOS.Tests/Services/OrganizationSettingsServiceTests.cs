using CoachOS.Application.Mappings;
using CoachOS.Application.OrganizationSettings;
using CoachOS.Application.OrganizationSettings.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class OrganizationSettingsServiceTests
{
    private Mock<IOrganizationSettingsRepository> _repo = null!;
    private ApplicationMapper _mapper = null!;
    private OrganizationSettingsService _sut = null!;

    private static readonly Guid OrgId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IOrganizationSettingsRepository>();
        _mapper = new ApplicationMapper();
        _sut = new OrganizationSettingsService(_repo.Object, _mapper);
    }

    [Test]
    public async Task GetAsync_ExistingSettings_ReturnsDto()
    {
        OrganizationSettings existing = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            AdminsActAsTrainers = false,
        };
        _repo.Setup(r => r.GetByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _sut.GetAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AdminsActAsTrainers.Should().BeFalse();
        _repo.Verify(r => r.AddAsync(It.IsAny<OrganizationSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAsync_MissingSettings_LazyCreatesWithDefaultTrue()
    {
        _repo.Setup(r => r.GetByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationSettings?)null);

        var result = await _sut.GetAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AdminsActAsTrainers.Should().BeTrue();
        _repo.Verify(r => r.AddAsync(
            It.Is<OrganizationSettings>(s => s.OrganizationId == OrgId && s.AdminsActAsTrainers),
            It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_PersistsNewValueAndReturnsDto()
    {
        OrganizationSettings existing = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            AdminsActAsTrainers = true,
        };
        _repo.Setup(r => r.GetByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        UpdateOrganizationSettingsRequest request = new(AdminsActAsTrainers: false);

        var result = await _sut.UpdateAsync(OrgId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AdminsActAsTrainers.Should().BeFalse();
        existing.AdminsActAsTrainers.Should().BeFalse();
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_MissingSettings_CreatesAndApplies()
    {
        _repo.Setup(r => r.GetByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationSettings?)null);

        UpdateOrganizationSettingsRequest request = new(AdminsActAsTrainers: false);

        var result = await _sut.UpdateAsync(OrgId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AdminsActAsTrainers.Should().BeFalse();
        _repo.Verify(r => r.AddAsync(It.IsAny<OrganizationSettings>(), It.IsAny<CancellationToken>()), Times.Once);
        // Eén SaveChanges voor de create, één voor de update.
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
