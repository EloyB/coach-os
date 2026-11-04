using CoachOS.Application.Abstractions;
using CoachOS.Application.Dashboard;
using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Dashboard;

/// <summary>
/// Samenvatting voor het trainersdashboard: volgende les, uren (lessen + kampdagen) deze week en
/// maand en de kampdagen in de kalender.
/// </summary>
[TestFixture]
public class DashboardServiceMyOverviewTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();

    // Donderdag 15 oktober 2026, 14:00 Brusselse tijd (CEST = UTC+2).
    private static readonly DateOnly Today = new(2026, 10, 15);
    private sealed class FixedTime(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private Mock<ILessonRepository> _lessons = null!;
    private Mock<IEnrollmentRepository> _enrollments = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IAssignmentConfirmationTokenRepository> _tokens = null!;
    private Mock<ICampRepository> _camps = null!;
    private DashboardService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _lessons = new Mock<ILessonRepository>();
        _enrollments = new Mock<IEnrollmentRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _tokens = new Mock<IAssignmentConfirmationTokenRepository>();
        _camps = new Mock<ICampRepository>();

        _sut = new DashboardService(
            Mock.Of<ILessonSerieRepository>(), _lessons.Object, _enrollments.Object,
            Mock.Of<ITennisClubRepository>(), _userLookup.Object, _tokens.Object,
            Mock.Of<IScheduleAssignmentRepository>(), Mock.Of<ILessonInvitationRepository>(),
            _camps.Object, new FixedTime(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero)));

        _lessons.Setup(r => r.GetByTrainerAndDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _camps.Setup(r => r.GetTrainerDaysAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _enrollments.Setup(r => r.CountActiveBySeriesIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
    }

    private static Lesson L(DateOnly date, int startHour, int startMin, int endHour, int endMin,
        Guid? seriesId = null) => new()
    {
        Id = Guid.NewGuid(), OrganizationId = OrgId, TrainerId = TrainerId, LessonSerieId = seriesId,
        Date = date, StartTime = new TimeOnly(startHour, startMin), EndTime = new TimeOnly(endHour, endMin),
        CourtName = "Baan 1",
    };

    private static CampDayTrainer CampDay(DateOnly date, int startHour, int endHour, string name = "Paaskamp") => new()
    {
        OrganizationId = OrgId, TrainerId = TrainerId,
        StartTime = new TimeOnly(startHour, 0), EndTime = new TimeOnly(endHour, 0),
        CampDay = new CampDay
        {
            Date = date, CampId = Guid.NewGuid(),
            Camp = new Camp { Name = name, TennisClub = new TennisClub { Name = "TC De Aces" } },
        },
    };

    private void GivenRange(DateOnly from, DateOnly to, params Lesson[] lessons) =>
        _lessons.Setup(r => r.GetByTrainerAndDateRangeAsync(TrainerId, OrgId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. lessons]);

    private static readonly DateOnly WeekStart = new(2026, 10, 12);   // maandag
    private static readonly DateOnly MonthStart = new(2026, 10, 1);
    private static readonly DateOnly RangeEnd = new(2026, 10, 31);

    [Test]
    public async Task GetMyOverview_NextLesson_SkipsLessonsThatAlreadyEndedToday()
    {
        Lesson ended = L(Today, 9, 0, 10, 0);          // voorbij (nu = 14:00)
        Lesson running = L(Today, 13, 30, 14, 30);     // loopt nog
        Lesson tomorrow = L(Today.AddDays(1), 10, 0, 11, 0);
        GivenRange(Today, Today.AddDays(120), ended, running, tomorrow);

        MyOverviewDto dto = (await _sut.GetMyOverviewAsync(OrgId, TrainerId)).Value!;

        dto.NextLesson.Should().NotBeNull();
        dto.NextLesson!.Id.Should().Be(running.Id);
    }

    [Test]
    public async Task GetMyOverview_AllTodayLessonsEnded_PicksFirstLessonOfALaterDay()
    {
        Lesson ended = L(Today, 9, 0, 10, 0);
        Lesson tomorrow = L(Today.AddDays(1), 10, 0, 11, 0);
        GivenRange(Today, Today.AddDays(120), ended, tomorrow);

        MyOverviewDto dto = (await _sut.GetMyOverviewAsync(OrgId, TrainerId)).Value!;

        dto.NextLesson!.Id.Should().Be(tomorrow.Id);
    }

    [Test]
    public async Task GetMyOverview_NoUpcomingLessons_NextLessonIsNull()
    {
        MyOverviewDto dto = (await _sut.GetMyOverviewAsync(OrgId, TrainerId)).Value!;

        dto.NextLesson.Should().BeNull();
        dto.HoursThisWeek.Should().Be(0);
    }

    [Test]
    public async Task GetMyOverview_NextLessonOfSeries_IncludesEnrolledCount()
    {
        Guid seriesId = Guid.NewGuid();
        Lesson next = L(Today.AddDays(2), 18, 0, 19, 0, seriesId);
        GivenRange(Today, Today.AddDays(120), next);
        _enrollments.Setup(r => r.CountActiveBySeriesIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(seriesId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [seriesId] = 7 });

        MyOverviewDto dto = (await _sut.GetMyOverviewAsync(OrgId, TrainerId)).Value!;

        dto.NextLesson!.EnrolledCount.Should().Be(7);
    }

    [Test]
    public async Task GetMyOverview_Hours_SplitsWeekAndMonthAndCountsCampDays()
    {
        // Eén bereik (week ∪ maand = 1–31 oktober) wordt opgehaald en verdeeld.
        Lesson inWeek1 = L(new DateOnly(2026, 10, 13), 18, 0, 19, 30);     // 1,5u, deze week
        Lesson inWeek2 = L(new DateOnly(2026, 10, 17), 10, 0, 11, 0);      // 1u, deze week (zaterdag)
        Lesson laterThisMonth = L(new DateOnly(2026, 10, 27), 20, 0, 21, 0); // 1u, enkel maand
        Lesson earlierThisMonth = L(new DateOnly(2026, 10, 2), 9, 0, 10, 0); // 1u, enkel maand
        GivenRange(MonthStart, RangeEnd, inWeek1, inWeek2, laterThisMonth, earlierThisMonth);
        _camps.Setup(r => r.GetTrainerDaysAsync(TrainerId, OrgId, MonthStart, RangeEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CampDay(new DateOnly(2026, 10, 14), 9, 12)]);      // 3u, deze week

        MyOverviewDto dto = (await _sut.GetMyOverviewAsync(OrgId, TrainerId)).Value!;

        dto.HoursThisWeek.Should().Be(5.5);    // 1,5 + 1 + 3
        dto.HoursThisMonth.Should().Be(7.5);   // 5,5 + 1 + 1
    }

    [Test]
    public async Task GetMyOverview_WeekCrossingMonthBoundary_CountsEachPeriodCorrectly()
    {
        // Donderdag 1 oktober 2026: de week (28 sep – 4 okt) loopt over de maandgrens,
        // dus het opgehaalde bereik is 28 sep – 31 okt.
        DashboardService sut = new(
            Mock.Of<ILessonSerieRepository>(), _lessons.Object, _enrollments.Object,
            Mock.Of<ITennisClubRepository>(), _userLookup.Object, _tokens.Object,
            Mock.Of<IScheduleAssignmentRepository>(), Mock.Of<ILessonInvitationRepository>(),
            _camps.Object, new FixedTime(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)));

        DateOnly from = new(2026, 9, 28), to = new(2026, 10, 31);
        Lesson weekOnly = L(new DateOnly(2026, 9, 29), 18, 0, 19, 0);    // deze week, vorige maand
        Lesson both = L(new DateOnly(2026, 10, 3), 10, 0, 11, 0);        // deze week én deze maand
        Lesson monthOnly = L(new DateOnly(2026, 10, 20), 20, 0, 21, 0);  // enkel deze maand
        GivenRange(from, to, weekOnly, both, monthOnly);

        MyOverviewDto dto = (await sut.GetMyOverviewAsync(OrgId, TrainerId)).Value!;

        dto.HoursThisWeek.Should().Be(2);    // 29 sep + 3 okt
        dto.HoursThisMonth.Should().Be(2);   // 3 okt + 20 okt (29 sep valt in vorige maand)
    }

    [Test]
    public async Task GetMyOverview_CapacityComesFromMembership()
    {
        _userLookup.Setup(u => u.GetWeeklyCapacityHoursAsync(TrainerId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);

        MyOverviewDto dto = (await _sut.GetMyOverviewAsync(OrgId, TrainerId)).Value!;

        dto.WeeklyCapacityHours.Should().Be(12);
    }

    // ── Kampdagen ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetMyCampDays_MapsTrainerWindowAndClub()
    {
        DateOnly from = new(2026, 10, 1), to = new(2026, 10, 31);
        CampDayTrainer day = CampDay(new DateOnly(2026, 10, 20), 9, 12, "Gratis Padel Proefkamp");
        _camps.Setup(r => r.GetTrainerDaysAsync(TrainerId, OrgId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync([day]);

        List<MyCampDayDto> result = (await _sut.GetMyCampDaysAsync(OrgId, TrainerId, from, to)).Value!;

        MyCampDayDto dto = result.Single();
        dto.CampId.Should().Be(day.CampDay.CampId);
        dto.CampName.Should().Be("Gratis Padel Proefkamp");
        dto.Date.Should().Be("2026-10-20");
        dto.StartTime.Should().Be("09:00");
        dto.EndTime.Should().Be("12:00");
        dto.TennisClubName.Should().Be("TC De Aces");
    }

    [Test]
    public async Task GetMyCampDays_InvalidRange_ReturnsValidationError()
    {
        Result<List<MyCampDayDto>> reversed =
            await _sut.GetMyCampDaysAsync(OrgId, TrainerId, new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 1));
        Result<List<MyCampDayDto>> tooLong =
            await _sut.GetMyCampDaysAsync(OrgId, TrainerId, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        reversed.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        tooLong.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _camps.VerifyNoOtherCalls();
    }
}
