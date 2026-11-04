using CoachOS.Application.Abstractions;
using CoachOS.Application.Dashboard;
using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Common;
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

/// <summary>
/// Hoofdtrainer-dashboard: lessen van alle trainers in de club(s) waar de gebruiker hoofdtrainer is,
/// met dezelfde "officieel ingepland"-regel als het trainersdashboard.
/// </summary>
[TestFixture]
public class ClubLessonsRepositoryTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid Me = Guid.NewGuid();
    private static readonly Guid Colleague = Guid.NewGuid();
    private static readonly Guid ClubA = Guid.NewGuid();
    private static readonly Guid ClubB = Guid.NewGuid();
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
        Guid clubId, Guid trainerId, PlanningStatus status = PlanningStatus.Scheduled, bool cancelled = false)
    {
        LessonSerie series = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, Name = "Reeks", TennisClubId = clubId, PlanningStatus = status,
        };
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, TrainerId = trainerId, LessonSerieId = series.Id,
            LessonSerie = series, Date = Day, StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0),
            IsCancelled = cancelled,
        };
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();
        return lesson;
    }

    private async Task<Lesson> Standalone(Guid? clubId, Guid trainerId)
    {
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, TrainerId = trainerId, LessonSerieId = null,
            TennisClubId = clubId, Date = Day, StartTime = new TimeOnly(20, 0), EndTime = new TimeOnly(21, 0),
        };
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();
        return lesson;
    }

    [Test]
    public async Task GetByClubs_ReturnsLessonsOfAllTrainersInTheClub()
    {
        Lesson mine = await SeriesLesson(ClubA, Me);
        Lesson colleagues = await SeriesLesson(ClubA, Colleague);
        Lesson standalone = await Standalone(ClubA, Colleague);

        List<Lesson> result = await _sut.GetByClubsAndDateRangeAsync([ClubA], OrgId, Day, Day);

        result.Select(l => l.Id).Should().BeEquivalentTo([mine.Id, colleagues.Id, standalone.Id]);
    }

    [Test]
    public async Task GetByClubs_ExcludesOtherClubsCancelledAndNotYetPlanned()
    {
        Lesson inClub = await SeriesLesson(ClubA, Colleague);
        await SeriesLesson(ClubB, Colleague);                                   // andere club
        await SeriesLesson(ClubA, Colleague, cancelled: true);                  // geannuleerd
        await SeriesLesson(ClubA, Colleague, PlanningStatus.Enrollment);        // nog niet ingepland
        await SeriesLesson(ClubA, Colleague, PlanningStatus.Planning);          // enkel voorstel
        await Standalone(ClubB, Colleague);                                     // losse les andere club
        await Standalone(null, Colleague);                                      // losse les zonder (legacy) club

        List<Lesson> result = await _sut.GetByClubsAndDateRangeAsync([ClubA], OrgId, Day, Day);

        result.Select(l => l.Id).Should().Equal(inClub.Id);
    }

    [Test]
    public async Task GetByClubs_MultipleClubs_ReturnsLessonsOfEachHeadTrainerClub()
    {
        Lesson a = await SeriesLesson(ClubA, Colleague);
        Lesson b = await SeriesLesson(ClubB, Colleague);

        List<Lesson> result = await _sut.GetByClubsAndDateRangeAsync([ClubA, ClubB], OrgId, Day, Day);

        result.Select(l => l.Id).Should().BeEquivalentTo([a.Id, b.Id]);
    }

    [Test]
    public async Task GetForTrainer_HeadTrainerSeesColleagueLessonInOwnClub_ButNotInAnotherClub()
    {
        Lesson inClub = await SeriesLesson(ClubA, Colleague);
        Lesson otherClub = await SeriesLesson(ClubB, Colleague);
        Lesson standaloneInClub = await Standalone(ClubA, Colleague);

        (await _sut.GetForTrainerAsync(inClub.Id, Me, [ClubA], OrgId)).Should().NotBeNull();
        (await _sut.GetForTrainerAsync(standaloneInClub.Id, Me, [ClubA], OrgId)).Should().NotBeNull();
        (await _sut.GetForTrainerAsync(otherClub.Id, Me, [ClubA], OrgId)).Should().BeNull();
    }

    [Test]
    public async Task GetForTrainer_PlainTrainerDoesNotSeeColleagueLesson_ButKeepsOwnLessonInAnyClub()
    {
        Lesson colleagues = await SeriesLesson(ClubA, Colleague);
        Lesson ownOtherClub = await SeriesLesson(ClubB, Me);

        (await _sut.GetForTrainerAsync(colleagues.Id, Me, [], OrgId)).Should().BeNull();
        // Een eigen les blijft zichtbaar, ook in een club waar je geen hoofdtrainer van bent.
        (await _sut.GetForTrainerAsync(ownOtherClub.Id, Me, [ClubA], OrgId)).Should().NotBeNull();
    }

    [Test]
    public async Task GetForTrainer_ClubLessonStillRequiresPlanningConfirmation()
    {
        Lesson unplanned = await SeriesLesson(ClubA, Colleague, PlanningStatus.Planning);

        (await _sut.GetForTrainerAsync(unplanned.Id, Me, [ClubA], OrgId)).Should().BeNull();
    }
}

[TestFixture]
public class ClubLessonsServiceTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid ClubA = Guid.NewGuid();
    private static readonly DateOnly From = new(2026, 10, 1);
    private static readonly DateOnly To = new(2026, 10, 31);

    private Mock<ILessonRepository> _lessons = null!;
    private Mock<IEnrollmentRepository> _enrollments = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<ILessonInvitationRepository> _invitations = null!;
    private DashboardService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _lessons = new Mock<ILessonRepository>();
        _enrollments = new Mock<IEnrollmentRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _invitations = new Mock<ILessonInvitationRepository>();
        _invitations.Setup(r => r.GetByLessonAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LessonInvitation>());
        _sut = new DashboardService(
            Mock.Of<ILessonSerieRepository>(), _lessons.Object, _enrollments.Object,
            Mock.Of<ITennisClubRepository>(), _userLookup.Object,
            Mock.Of<IAssignmentConfirmationTokenRepository>(), Mock.Of<IScheduleAssignmentRepository>(),
            _invitations.Object, Mock.Of<ICampRepository>(), TimeProvider.System);
    }

    [Test]
    public async Task GetMyClubLessons_WithoutHeadTrainerClub_IsForbidden()
    {
        Result<List<MyLessonDto>> result = await _sut.GetMyClubLessonsAsync(OrgId, [], From, To);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Forbidden);
        _lessons.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GetMyClubLessons_InvalidRange_ReturnsValidationError()
    {
        Result<List<MyLessonDto>> reversed = await _sut.GetMyClubLessonsAsync(OrgId, [ClubA], To, From);
        Result<List<MyLessonDto>> tooLong =
            await _sut.GetMyClubLessonsAsync(OrgId, [ClubA], From, From.AddDays(120));

        reversed.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        tooLong.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _lessons.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GetMyClubLessons_MapsTrainerNameIdAndEnrolledCount()
    {
        Guid trainerId = Guid.NewGuid();
        Guid seriesId = Guid.NewGuid();
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, TrainerId = trainerId, LessonSerieId = seriesId,
            Date = new DateOnly(2026, 10, 21), StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0),
            CourtName = "Baan 2",
            LessonSerie = new LessonSerie
            {
                Id = seriesId, Name = "Jeugdtraining", TennisClub = new TennisClub { Name = "TC De Aces" },
            },
        };
        _lessons.Setup(r => r.GetByClubsAndDateRangeAsync(
                It.Is<IReadOnlyList<Guid>>(c => c.SequenceEqual(new[] { ClubA })), OrgId, From, To,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([lesson]);
        _enrollments.Setup(r => r.CountActiveBySeriesIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [seriesId] = 5 });
        _userLookup.Setup(u => u.GetUserNamesByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [trainerId] = "Pieter Mertens" });

        List<MyLessonDto> result = (await _sut.GetMyClubLessonsAsync(OrgId, [ClubA], From, To)).Value!;

        MyLessonDto dto = result.Single();
        dto.TrainerId.Should().Be(trainerId);
        dto.TrainerName.Should().Be("Pieter Mertens");
        dto.SeriesName.Should().Be("Jeugdtraining");
        dto.TennisClubName.Should().Be("TC De Aces");
        dto.EnrolledCount.Should().Be(5);
    }

    [Test]
    public async Task GetMyLesson_DetailIncludesTrainerNameAndId()
    {
        Guid trainerId = Guid.NewGuid();
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, TrainerId = trainerId, LessonSerieId = null,
            Date = new DateOnly(2026, 10, 21), StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0),
        };
        _lessons.Setup(r => r.GetForTrainerAsync(
                lesson.Id, It.IsAny<Guid>(), It.Is<IReadOnlyList<Guid>>(c => c.Contains(ClubA)), OrgId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _userLookup.Setup(u => u.GetUserNameByIdAsync(trainerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Jan Janssen");

        Result<MyLessonDetailDto> result = await _sut.GetMyLessonAsync(OrgId, Guid.NewGuid(), lesson.Id, [ClubA]);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TrainerId.Should().Be(trainerId);
        result.Value.TrainerName.Should().Be("Jan Janssen");
    }
}
