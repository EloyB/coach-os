using CoachOS.Application.Onboarding;
using CoachOS.Application.Onboarding.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class OnboardingServiceTests
{
    private Mock<IOrganizationSettingsRepository> _settingsRepo = null!;
    private Mock<IMollieConnectionRepository> _mollieRepo = null!;
    private Mock<ITennisClubRepository> _clubRepo = null!;
    private Mock<ILessonSerieRepository> _seriesRepo = null!;
    private OnboardingService _sut = null!;

    private static readonly Guid OrgId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _settingsRepo = new Mock<IOrganizationSettingsRepository>();
        _mollieRepo = new Mock<IMollieConnectionRepository>();
        _clubRepo = new Mock<ITennisClubRepository>();
        _seriesRepo = new Mock<ILessonSerieRepository>();

        // Defaults: alles uit. Tests die een stap "compleet" willen overschrijven dit.
        _mollieRepo.Setup(r => r.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MollieConnection?)null);
        _clubRepo.Setup(r => r.AnyByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _seriesRepo.Setup(r => r.AnyByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sut = new OnboardingService(
            _settingsRepo.Object,
            _mollieRepo.Object,
            _clubRepo.Object,
            _seriesRepo.Object);
    }

    private void ArrangeSettings(OrganizationSettings settings)
    {
        _settingsRepo.Setup(r => r.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        _settingsRepo.Setup(r => r.GetByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
    }

    [Test]
    public async Task GetStateAsync_OrgWithStartedAtNull_ReturnsShouldShowFalse()
    {
        ArrangeSettings(new OrganizationSettings
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            OnboardingStartedAt = null,
        });

        var result = await _sut.GetStateAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ShouldShow.Should().BeFalse();
    }

    [Test]
    public async Task GetStateAsync_OrgWithDismissedAt_ReturnsShouldShowFalse()
    {
        ArrangeSettings(new OrganizationSettings
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            OnboardingStartedAt = DateTime.UtcNow.AddDays(-1),
            OnboardingDismissedAt = DateTime.UtcNow,
        });

        var result = await _sut.GetStateAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ShouldShow.Should().BeFalse();
    }

    [Test]
    public async Task GetStateAsync_StartedAndNotDismissed_ReturnsShouldShowTrue()
    {
        ArrangeSettings(new OrganizationSettings
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            OnboardingStartedAt = DateTime.UtcNow,
            OnboardingDismissedAt = null,
        });

        var result = await _sut.GetStateAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ShouldShow.Should().BeTrue();
        result.Value.AllCompleted.Should().BeFalse();
        result.Value.Steps.Should().HaveCount(4);
        result.Value.Steps.Select(s => s.Key).Should().BeEquivalentTo(new[] { "mollie", "club", "trainerMode", "series" });
        result.Value.Steps.Should().OnlyContain(s => !s.Completed);
    }

    [Test]
    public async Task GetStateAsync_AllFourStepsComplete_AllCompletedTrue()
    {
        ArrangeSettings(new OrganizationSettings
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            OnboardingStartedAt = DateTime.UtcNow,
            TrainerModeChosenAt = DateTime.UtcNow,
        });
        _mollieRepo.Setup(r => r.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MollieConnection { Id = Guid.NewGuid(), OrganizationId = OrgId });
        _clubRepo.Setup(r => r.AnyByOrganizationAsync(OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _seriesRepo.Setup(r => r.AnyByOrganizationAsync(OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.GetStateAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AllCompleted.Should().BeTrue();
        // ShouldShow blijft true tot expliciet gedismissed; FE rendert de celebration variant.
        result.Value.ShouldShow.Should().BeTrue();
        result.Value.Steps.Should().OnlyContain(s => s.Completed);
    }

    [Test]
    public async Task GetStateAsync_NoSettingsRow_ReturnsShouldShowFalseAndEmptySteps()
    {
        _settingsRepo.Setup(r => r.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationSettings?)null);

        var result = await _sut.GetStateAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ShouldShow.Should().BeFalse();
        result.Value.Steps.Should().HaveCount(4);
    }

    [Test]
    public async Task SetTrainerModeAsync_AdminCoaches_SetsFlagTrueAndStampsChosenAt()
    {
        var settings = new OrganizationSettings
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            OnboardingStartedAt = DateTime.UtcNow,
            AdminsActAsTrainers = false,
        };
        ArrangeSettings(settings);

        var result = await _sut.SetTrainerModeAsync(OrgId, new SetTrainerModeRequest(AdminActsAsTrainer: true));

        result.IsSuccess.Should().BeTrue();
        settings.AdminsActAsTrainers.Should().BeTrue();
        settings.TrainerModeChosenAt.Should().NotBeNull();
        _settingsRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetTrainerModeAsync_AdminNotCoaching_SetsFlagFalseAndStampsChosenAt()
    {
        var settings = new OrganizationSettings
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            OnboardingStartedAt = DateTime.UtcNow,
            AdminsActAsTrainers = true,
        };
        ArrangeSettings(settings);

        var result = await _sut.SetTrainerModeAsync(OrgId, new SetTrainerModeRequest(AdminActsAsTrainer: false));

        result.IsSuccess.Should().BeTrue();
        settings.AdminsActAsTrainers.Should().BeFalse();
        settings.TrainerModeChosenAt.Should().NotBeNull();
    }

    [Test]
    public async Task SetTrainerModeAsync_NoSettingsRow_ReturnsFailure()
    {
        _settingsRepo.Setup(r => r.GetByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationSettings?)null);

        var result = await _sut.SetTrainerModeAsync(OrgId, new SetTrainerModeRequest(true));

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task DismissAsync_StampsTimestampOnce_IdempotentOnSecondCall()
    {
        var settings = new OrganizationSettings
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            OnboardingStartedAt = DateTime.UtcNow,
        };
        ArrangeSettings(settings);

        var result1 = await _sut.DismissAsync(OrgId);
        DateTime? firstStamp = settings.OnboardingDismissedAt;

        var result2 = await _sut.DismissAsync(OrgId);

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        firstStamp.Should().NotBeNull();
        settings.OnboardingDismissedAt.Should().Be(firstStamp);
        // Eerste call schrijft, tweede call is no-op → exact 1 SaveChanges.
        _settingsRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
