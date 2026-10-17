using CoachOS.Application.Dashboard;
using CoachOS.Application.Abstractions;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Dashboard;

[TestFixture]
public class DashboardServiceMyLessonsTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();

    private Mock<ILessonRepository> _lessons = null!;
    private Mock<IEnrollmentRepository> _enrollments = null!;
    private DashboardService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _lessons = new Mock<ILessonRepository>();
        _enrollments = new Mock<IEnrollmentRepository>();
        _sut = new DashboardService(
            Mock.Of<ILessonSerieRepository>(),
            _lessons.Object,
            _enrollments.Object,
            Mock.Of<ITennisClubRepository>(),
            Mock.Of<IUserLookupService>(),
            Mock.Of<IAssignmentConfirmationTokenRepository>(),
            Mock.Of<IScheduleAssignmentRepository>(),
            Mock.Of<ILessonInvitationRepository>(),
            Mock.Of<ICampRepository>(),
            TimeProvider.System);
    }

    private static readonly DateOnly From = new(2026, 10, 1);
    private static readonly DateOnly To = new(2026, 10, 31);

    [Test]
    public async Task GetMyLessons_SeriesLesson_MapsSeriesClubAndEnrolledCount()
    {
        Guid seriesId = Guid.NewGuid();
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            TrainerId = TrainerId,
            LessonSerieId = seriesId,
            Date = new DateOnly(2026, 10, 15),
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(19, 0),
            CourtName = "Baan 1",
            LessonSerie = new LessonSerie
            {
                Id = seriesId,
                Name = "Zomerlessen",
                TennisClub = new TennisClub { Name = "TC De Aces" },
            },
        };
        _lessons.Setup(r => r.GetByTrainerAndDateRangeAsync(TrainerId, OrgId, From, To, It.IsAny<CancellationToken>()))
            .ReturnsAsync([lesson]);
        _enrollments.Setup(r => r.CountActiveBySeriesIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(seriesId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [seriesId] = 6 });

        Result<List<CoachOS.Application.Dashboard.DTOs.MyLessonDto>> result =
            await _sut.GetMyLessonsAsync(OrgId, TrainerId, From, To);

        result.IsSuccess.Should().BeTrue();
        CoachOS.Application.Dashboard.DTOs.MyLessonDto dto = result.Value!.Single();
        dto.SeriesId.Should().Be(seriesId);
        dto.SeriesName.Should().Be("Zomerlessen");
        dto.Date.Should().Be("2026-10-15");
        dto.StartTime.Should().Be("18:00");
        dto.EndTime.Should().Be("19:00");
        dto.CourtName.Should().Be("Baan 1");
        dto.TennisClubName.Should().Be("TC De Aces");
        dto.EnrolledCount.Should().Be(6);
    }

    [Test]
    public async Task GetMyLessons_StandaloneLesson_HasNoSeriesAndUsesOwnClub()
    {
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            TrainerId = TrainerId,
            LessonSerieId = null,
            Date = new DateOnly(2026, 10, 16),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            TennisClub = new TennisClub { Name = "Padel Center" },
        };
        _lessons.Setup(r => r.GetByTrainerAndDateRangeAsync(TrainerId, OrgId, From, To, It.IsAny<CancellationToken>()))
            .ReturnsAsync([lesson]);

        Result<List<CoachOS.Application.Dashboard.DTOs.MyLessonDto>> result =
            await _sut.GetMyLessonsAsync(OrgId, TrainerId, From, To);

        CoachOS.Application.Dashboard.DTOs.MyLessonDto dto = result.Value!.Single();
        dto.SeriesId.Should().BeNull();
        dto.SeriesName.Should().BeNull();
        dto.TennisClubName.Should().Be("Padel Center");
        dto.EnrolledCount.Should().BeNull();
        _enrollments.Verify(r => r.CountActiveBySeriesIdsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetMyLessons_AsksRepositoryForOwnTrainerAndOrganizationOnly()
    {
        _lessons.Setup(r => r.GetByTrainerAndDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Result<List<CoachOS.Application.Dashboard.DTOs.MyLessonDto>> result =
            await _sut.GetMyLessonsAsync(OrgId, TrainerId, From, To);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        _lessons.Verify(r => r.GetByTrainerAndDateRangeAsync(
            TrainerId, OrgId, From, To, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetMyLessons_EndBeforeStart_ReturnsValidationError()
    {
        Result<List<CoachOS.Application.Dashboard.DTOs.MyLessonDto>> result =
            await _sut.GetMyLessonsAsync(OrgId, TrainerId, To, From);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _lessons.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GetMyLessons_RangeLargerThanTwoMonths_ReturnsValidationError()
    {
        Result<List<CoachOS.Application.Dashboard.DTOs.MyLessonDto>> result =
            await _sut.GetMyLessonsAsync(OrgId, TrainerId, From, From.AddDays(120));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _lessons.VerifyNoOtherCalls();
    }
}
