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

/// <summary>Cluboverzicht voor de hoofdtrainer: bezetting + aandachtspunten, enkel van de eigen club(s).</summary>
[TestFixture]
public class ClubOverviewRepositoryTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid ClubA = Guid.NewGuid();
    private static readonly Guid ClubB = Guid.NewGuid();

    private sealed class NoTenantContext : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    private ApplicationDbContext _db = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options, new NoTenantContext());
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetByClubs_ReturnsOnlySeriesOfThoseClubs()
    {
        TennisClub clubA = new() { Id = ClubA, OrganizationId = OrgId, Name = "Club A" };
        TennisClub clubB = new() { Id = ClubB, OrganizationId = OrgId, Name = "Club B" };
        _db.LessonSeries.AddRange(
            new LessonSerie { Id = Guid.NewGuid(), OrganizationId = OrgId, Name = "A", TennisClubId = ClubA, TennisClub = clubA },
            new LessonSerie { Id = Guid.NewGuid(), OrganizationId = OrgId, Name = "B", TennisClubId = ClubB, TennisClub = clubB });
        await _db.SaveChangesAsync();

        IReadOnlyList<LessonSerie> result =
            await new LessonSerieRepository(_db).GetByClubsAsync(OrgId, [ClubA]);

        result.Select(s => s.Name).Should().Equal("A");
    }

    [Test]
    public async Task GetPendingByClubs_ReturnsOnlyPendingTokensOfThoseClubs()
    {
        _db.TennisClubs.AddRange(
            new TennisClub { Id = ClubA, OrganizationId = OrgId, Name = "Club A" },
            new TennisClub { Id = ClubB, OrganizationId = OrgId, Name = "Club B" });

        async Task<AssignmentConfirmationToken> Token(Guid club, ConfirmationResponse response)
        {
            LessonSerie series = new()
            {
                Id = Guid.NewGuid(), OrganizationId = OrgId, Name = "R", TennisClubId = club,
            };
            ScheduleAssignment assignment = new()
            {
                Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = series.Id, LessonSerie = series,
            };
            AssignmentConfirmationToken token = new()
            {
                Id = Guid.NewGuid(), OrganizationId = OrgId, ScheduleAssignmentId = assignment.Id,
                ScheduleAssignment = assignment, Response = response, ExpiresAt = DateTime.UtcNow.AddDays(1),
                Enrollment = new Enrollment { Id = Guid.NewGuid(), StudentName = "Speler" },
            };
            _db.AssignmentConfirmationTokens.Add(token);
            await _db.SaveChangesAsync();
            return token;
        }

        AssignmentConfirmationToken mine = await Token(ClubA, ConfirmationResponse.Pending);
        await Token(ClubA, ConfirmationResponse.Confirmed);
        await Token(ClubB, ConfirmationResponse.Pending);

        List<AssignmentConfirmationToken> result =
            await new AssignmentConfirmationTokenRepository(_db).GetPendingByClubsAsync(OrgId, [ClubA]);

        result.Select(t => t.Id).Should().Equal(mine.Id);
    }
}

[TestFixture]
public class ClubOverviewServiceTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid ClubA = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private Mock<ILessonSerieRepository> _series = null!;
    private Mock<ILessonRepository> _lessons = null!;
    private Mock<IEnrollmentRepository> _enrollments = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IAssignmentConfirmationTokenRepository> _tokens = null!;
    private DashboardService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _series = new Mock<ILessonSerieRepository>();
        _lessons = new Mock<ILessonRepository>();
        _enrollments = new Mock<IEnrollmentRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _tokens = new Mock<IAssignmentConfirmationTokenRepository>();
        _lessons.Setup(r => r.GetByClubsAndDateRangeAsync(
                It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _userLookup.Setup(u => u.GetUserNamesByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        _tokens.Setup(t => t.GetPendingByClubsAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _sut = new DashboardService(
            _series.Object, _lessons.Object, _enrollments.Object, Mock.Of<ITennisClubRepository>(),
            _userLookup.Object, _tokens.Object, Mock.Of<IScheduleAssignmentRepository>(),
            Mock.Of<ILessonInvitationRepository>(), Mock.Of<ICampRepository>(), new FixedTime(Now));
    }

    private LessonSerie Series(
        int? max, DateOnly? start = null, PlanningStatus status = PlanningStatus.Scheduled, bool active = true)
    {
        LessonSerie series = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, Name = "Reeks " + Guid.NewGuid().ToString("N")[..4],
            TennisClubId = ClubA, TennisClub = new TennisClub { Name = "Club A" }, MaxRegistrations = max,
            StartDate = start ?? new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 31),
            PlanningStatus = status, IsActive = active,
        };
        return series;
    }

    private void Given(IEnumerable<LessonSerie> series, Dictionary<Guid, int> counts)
    {
        _series.Setup(r => r.GetByClubsAsync(OrgId, It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(series.ToList());
        _enrollments.Setup(r => r.CountActiveBySeriesIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(counts);
    }

    [Test]
    public async Task WithoutHeadTrainerClub_IsForbidden()
    {
        Result<ClubOverviewDto> result = await _sut.GetMyClubOverviewAsync(OrgId, []);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Forbidden);
        _series.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ComputesAverageOccupancy_AndSkipsInactiveSeries()
    {
        LessonSerie full = Series(8);
        LessonSerie half = Series(8);
        LessonSerie inactive = Series(8, active: false);
        Given([full, half, inactive], new() { [full.Id] = 8, [half.Id] = 4, [inactive.Id] = 0 });

        ClubOverviewDto dto = (await _sut.GetMyClubOverviewAsync(OrgId, [ClubA])).Value!;

        dto.ActiveSeriesCount.Should().Be(2);
        dto.AverageOccupancyPercent.Should().Be(75);
        dto.Series.Should().HaveCount(2);
    }

    [Test]
    public async Task FlagsUnderbookedSeriesBelowSixtyPercent()
    {
        LessonSerie low = Series(10);
        LessonSerie ok = Series(10);
        Given([low, ok], new() { [low.Id] = 5, [ok.Id] = 6 });

        ClubOverviewDto dto = (await _sut.GetMyClubOverviewAsync(OrgId, [ClubA])).Value!;

        dto.Attention.Where(a => a.Type == "series_underbooked").Select(a => a.SeriesId)
            .Should().Equal(low.Id);
        dto.AttentionCount.Should().Be(1);
    }

    [Test]
    public async Task FlagsStalledPlanning_OnlyWhenStartIsNearAndPlayersExist()
    {
        LessonSerie stalled = Series(null, new DateOnly(2026, 10, 25), PlanningStatus.Planning);
        LessonSerie farAway = Series(null, new DateOnly(2026, 12, 1), PlanningStatus.Enrollment);
        LessonSerie noPlayers = Series(null, new DateOnly(2026, 10, 25), PlanningStatus.Planning);
        LessonSerie scheduled = Series(null, new DateOnly(2026, 10, 25), PlanningStatus.Scheduled);
        Given([stalled, farAway, noPlayers, scheduled],
            new() { [stalled.Id] = 3, [farAway.Id] = 3, [scheduled.Id] = 3 });

        ClubOverviewDto dto = (await _sut.GetMyClubOverviewAsync(OrgId, [ClubA])).Value!;

        dto.Attention.Where(a => a.Type == "planning_stalled").Select(a => a.SeriesId)
            .Should().Equal(stalled.Id);
    }

    [Test]
    public async Task GroupsPendingConfirmationsPerSeries_WithEarliestDeadline()
    {
        LessonSerie series = Series(null);
        LessonSerie other = Series(null);
        Given([series, other], new());
        AssignmentConfirmationToken Token(LessonSerie s, string name, int hours) => new()
        {
            ExpiresAt = Now.UtcDateTime.AddHours(hours),
            Enrollment = new Enrollment { StudentName = name },
            ScheduleAssignment = new ScheduleAssignment { LessonSerieId = s.Id, LessonSerie = s },
        };
        _tokens.Setup(t => t.GetPendingByClubsAsync(OrgId, It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Token(series, "Lotte", 30), Token(series, "Anna", 10), Token(other, "Bram", 50)]);

        ClubOverviewDto dto = (await _sut.GetMyClubOverviewAsync(OrgId, [ClubA])).Value!;

        List<ClubAttentionItemDto> items = dto.Attention.Where(a => a.Type == "confirmation_pending").ToList();
        items.Should().HaveCount(2);
        ClubAttentionItemDto grouped = items.Single(i => i.SeriesId == series.Id);
        grouped.StudentNames.Should().Equal("Anna", "Lotte");
        grouped.HoursRemaining.Should().Be(10);
    }

    [Test]
    public async Task ListsUpcomingLessonsWithoutTrainer_AndTrainerNamesPerSeries()
    {
        LessonSerie series = Series(null);
        Given([series], new());
        Guid trainerId = Guid.NewGuid();
        _lessons.Setup(r => r.GetByClubsAndDateRangeAsync(
                It.IsAny<IReadOnlyList<Guid>>(), OrgId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Lesson { LessonSerieId = series.Id, LessonSerie = series, TrainerId = trainerId, Date = new DateOnly(2026, 10, 21) },
                new Lesson { LessonSerieId = series.Id, LessonSerie = series, TrainerId = null, Date = new DateOnly(2026, 10, 28) },
            ]);
        _userLookup.Setup(u => u.GetUserNamesByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [trainerId] = "Sophie de Vries" });

        ClubOverviewDto dto = (await _sut.GetMyClubOverviewAsync(OrgId, [ClubA])).Value!;

        dto.Attention.Single(a => a.Type == "lesson_without_trainer").Date.Should().Be("2026-10-28");
        dto.Series.Single().TrainerNames.Should().Equal("Sophie de Vries");
    }
}
