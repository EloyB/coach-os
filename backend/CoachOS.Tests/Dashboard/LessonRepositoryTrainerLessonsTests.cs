using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using CoachOS.Application.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CoachOS.Tests.Dashboard;

/// <summary>
/// Het trainersdashboard toont enkel "officieel ingeplande" lessen: losse lessen en lessen van
/// reeksen waarvan de planning door de admin bevestigd is. Dit toetst die regel tegen een
/// echte (InMemory) DbContext.
/// </summary>
[TestFixture]
public class LessonRepositoryTrainerLessonsTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();
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

    private async Task<Lesson> AddSeriesLessonAsync(
        PlanningStatus status, Guid? trainerId = null, bool cancelled = false)
    {
        LessonSerie series = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            Name = $"Reeks {status}",
            PlanningStatus = status,
        };
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            TrainerId = trainerId ?? TrainerId,
            LessonSerieId = series.Id,
            LessonSerie = series,
            Date = Day,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(19, 0),
            IsCancelled = cancelled,
        };
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();
        return lesson;
    }

    private async Task<Lesson> AddStandaloneAsync()
    {
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            TrainerId = TrainerId,
            LessonSerieId = null,
            Date = Day,
            StartTime = new TimeOnly(20, 0),
            EndTime = new TimeOnly(21, 0),
        };
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();
        return lesson;
    }

    [Test]
    public async Task GetByTrainerAndDateRange_OnlyShowsStandaloneAndPlanningConfirmedSeries()
    {
        Lesson enrollmentPhase = await AddSeriesLessonAsync(PlanningStatus.Enrollment);
        Lesson proposal = await AddSeriesLessonAsync(PlanningStatus.Planning);
        Lesson awaiting = await AddSeriesLessonAsync(PlanningStatus.AwaitingConfirmation);
        Lesson scheduled = await AddSeriesLessonAsync(PlanningStatus.Scheduled);
        Lesson standalone = await AddStandaloneAsync();

        List<Lesson> result = await _sut.GetByTrainerAndDateRangeAsync(TrainerId, OrgId, Day, Day);

        result.Select(l => l.Id).Should().BeEquivalentTo(
            [awaiting.Id, scheduled.Id, standalone.Id]);
        result.Select(l => l.Id).Should().NotContain([enrollmentPhase.Id, proposal.Id]);
    }

    [Test]
    public async Task GetByTrainerAndDateRange_ExcludesCancelledAndOtherTrainers()
    {
        Lesson mine = await AddSeriesLessonAsync(PlanningStatus.Scheduled);
        await AddSeriesLessonAsync(PlanningStatus.Scheduled, cancelled: true);
        await AddSeriesLessonAsync(PlanningStatus.Scheduled, trainerId: Guid.NewGuid());

        List<Lesson> result = await _sut.GetByTrainerAndDateRangeAsync(TrainerId, OrgId, Day, Day);

        result.Select(l => l.Id).Should().Equal(mine.Id);
    }

    [TestCase(PlanningStatus.Enrollment, false)]
    [TestCase(PlanningStatus.Planning, false)]
    [TestCase(PlanningStatus.AwaitingConfirmation, true)]
    [TestCase(PlanningStatus.Scheduled, true)]
    public async Task GetForTrainer_FollowsTheSameVisibilityRule(PlanningStatus status, bool visible)
    {
        Lesson lesson = await AddSeriesLessonAsync(status);

        Lesson? result = await _sut.GetForTrainerAsync(lesson.Id, TrainerId, OrgId);

        (result is not null).Should().Be(visible);
    }

    [Test]
    public async Task GetForTrainer_StandaloneLesson_IsAlwaysVisibleToItsTrainer()
    {
        Lesson lesson = await AddStandaloneAsync();

        (await _sut.GetForTrainerAsync(lesson.Id, TrainerId, OrgId)).Should().NotBeNull();
        (await _sut.GetForTrainerAsync(lesson.Id, Guid.NewGuid(), OrgId)).Should().BeNull();
    }
}
