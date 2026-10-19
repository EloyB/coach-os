using CoachOS.Application.Abstractions;
using CoachOS.Application.Dashboard;
using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Dashboard;

/// <summary>Admin-dashboard: alle officieel ingeplande lessen van de organisatie, ongeacht trainer of club.</summary>
[TestFixture]
public class OrganizationLessonsRepositoryTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid OtherOrgId = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 10, 21);

    private sealed class NoTenantContext : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    private ApplicationDbContext _db = null!;
    private LessonRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options, new NoTenantContext());
        _sut = new LessonRepository(_db, TimeProvider.System);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<Lesson> SeriesLesson(
        Guid orgId, PlanningStatus status = PlanningStatus.Scheduled, bool cancelled = false, DateOnly? date = null)
    {
        TennisClub club = new() { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Club" };
        LessonSerie series = new()
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Reeks", TennisClubId = club.Id, TennisClub = club,
            PlanningStatus = status,
        };
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(), OrganizationId = orgId, TrainerId = Guid.NewGuid(), LessonSerieId = series.Id,
            LessonSerie = series, Date = date ?? Day, StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0),
            IsCancelled = cancelled,
        };
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();
        return lesson;
    }

    [Test]
    public async Task GetByOrganization_ReturnsLessonsOfAllTrainersAndClubs_ButOnlyOfficialOnesInRange()
    {
        Lesson first = await SeriesLesson(OrgId);
        Lesson second = await SeriesLesson(OrgId);
        await SeriesLesson(OrgId, PlanningStatus.Planning);
        await SeriesLesson(OrgId, cancelled: true);
        await SeriesLesson(OrgId, date: Day.AddDays(40));
        await SeriesLesson(OtherOrgId);
        Lesson standalone = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, TrainerId = Guid.NewGuid(), Date = Day,
            StartTime = new TimeOnly(20, 0), EndTime = new TimeOnly(21, 0),
        };
        _db.Lessons.Add(standalone);
        await _db.SaveChangesAsync();

        List<Lesson> result = await _sut.GetByOrganizationAndDateRangeAsync(OrgId, Day, Day.AddDays(7));

        result.Select(l => l.Id).Should().BeEquivalentTo([first.Id, second.Id, standalone.Id]);
    }

    [Test]
    public async Task GetOfficialById_IgnoresTrainer_ButNotOrganizationOrPlanningStatus()
    {
        Lesson lesson = await SeriesLesson(OrgId);
        Lesson unplanned = await SeriesLesson(OrgId, PlanningStatus.Planning);
        Lesson otherOrg = await SeriesLesson(OtherOrgId);

        (await _sut.GetOfficialByIdAsync(lesson.Id, OrgId)).Should().NotBeNull();
        (await _sut.GetOfficialByIdAsync(unplanned.Id, OrgId)).Should().BeNull();
        (await _sut.GetOfficialByIdAsync(otherOrg.Id, OrgId)).Should().BeNull();
    }
}

[TestFixture]
public class OrganizationLessonsServiceTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly DateOnly From = new(2026, 10, 1);

    private Mock<ILessonRepository> _lessons = null!;
    private Mock<IEnrollmentRepository> _enrollments = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private DashboardService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _lessons = new Mock<ILessonRepository>();
        _enrollments = new Mock<IEnrollmentRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _sut = new DashboardService(
            Mock.Of<ILessonSerieRepository>(), _lessons.Object, _enrollments.Object,
            Mock.Of<ITennisClubRepository>(), _userLookup.Object,
            Mock.Of<IAssignmentConfirmationTokenRepository>(), Mock.Of<IScheduleAssignmentRepository>(),
            Mock.Of<ILessonInvitationRepository>(), Mock.Of<ICampRepository>(), TimeProvider.System);
    }

    [Test]
    public async Task GetOrganizationLessons_InvalidRange_ReturnsValidationError()
    {
        Result<List<MyLessonDto>> reversed = await _sut.GetOrganizationLessonsAsync(OrgId, From.AddDays(5), From);
        Result<List<MyLessonDto>> tooLong = await _sut.GetOrganizationLessonsAsync(OrgId, From, From.AddDays(120));

        reversed.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        tooLong.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _lessons.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GetOrganizationLessons_MapsTrainerNameAndEnrolledCount()
    {
        Guid trainerId = Guid.NewGuid();
        Guid seriesId = Guid.NewGuid();
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, TrainerId = trainerId, LessonSerieId = seriesId,
            Date = new DateOnly(2026, 10, 21), StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0),
            LessonSerie = new LessonSerie { Id = seriesId, Name = "Reeks" },
        };
        _lessons.Setup(r => r.GetByOrganizationAndDateRangeAsync(
                OrgId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([lesson]);
        _enrollments.Setup(r => r.CountActiveBySeriesIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [seriesId] = 6 });
        _userLookup.Setup(u => u.GetUserNamesByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [trainerId] = "Sophie De Vries" });

        MyLessonDto dto = (await _sut.GetOrganizationLessonsAsync(OrgId, From, From.AddDays(30))).Value!.Single();

        dto.TrainerName.Should().Be("Sophie De Vries");
        dto.TrainerId.Should().Be(trainerId);
        dto.EnrolledCount.Should().Be(6);
    }

    [Test]
    public async Task GetOrganizationLesson_UnknownLesson_IsNotFound()
    {
        _lessons.Setup(r => r.GetOfficialByIdAsync(It.IsAny<Guid>(), OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        Result<MyLessonDetailDto> result = await _sut.GetOrganizationLessonAsync(OrgId, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }
}
