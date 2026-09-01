using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CoachOS.Tests.Repositories;

/// <summary>
/// Toetst de daadwerkelijke EF/LINQ-predicaat van LessonRepository.FindCourtConflictAsync tegen
/// een echte (InMemory) DbContext — gemockte repository-tests in LessonCourtConflictTests
/// verbergen fouten in de query zelf (bv. een verkeerde join of een omgedraaide voorwaarde).
/// Dekt expliciet: reeks vs. losse les in beide aanmaakvolgordes, verschillende clubs,
/// geannuleerde lessen, self-exclusion en de legacy null-club fallback.
/// </summary>
[TestFixture]
public class LessonRepositoryCourtConflictTests
{
    private ApplicationDbContext _db = null!;
    private LessonRepository _repo = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid ClubA = Guid.NewGuid();
    private static readonly Guid ClubB = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 12, 5);
    private static readonly TimeOnly Start = new(19, 0);
    private static readonly TimeOnly End = new(20, 0);

    private sealed class NoTenantContext : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options, new NoTenantContext());
        _repo = new LessonRepository(_db, TimeProvider.System);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private static LessonSerie BuildSeries(Guid clubId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = OrgId,
        Name = "Reeks",
        TennisClubId = clubId,
        StartDate = Date,
        EndDate = Date,
        RegistrationDeadline = DateTime.UtcNow,
    };

    private static Lesson BuildSeriesLesson(LessonSerie series, string court = "Baan 2", bool cancelled = false) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = OrgId,
        LessonSerieId = series.Id,
        Date = Date,
        StartTime = Start,
        EndTime = End,
        CourtName = court,
        IsCancelled = cancelled,
    };

    private static Lesson BuildStandaloneLesson(Guid? clubId, string court = "Baan 2", bool cancelled = false) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = OrgId,
        LessonSerieId = null,
        TennisClubId = clubId,
        Date = Date,
        StartTime = Start,
        EndTime = End,
        CourtName = court,
        IsCancelled = cancelled,
    };

    // ── Series vs. standalone, both creation orders ─────────────────────────────

    [Test]
    public async Task SeriesLesson_ThenStandaloneAtSameClub_Conflicts()
    {
        LessonSerie series = BuildSeries(ClubA);
        _db.LessonSeries.Add(series);
        _db.Lessons.Add(BuildSeriesLesson(series));
        await _db.SaveChangesAsync();

        Lesson? conflict = await _repo.FindCourtConflictAsync(
            OrgId, "Baan 2", Date, Start, End, tennisClubId: ClubA);

        conflict.Should().NotBeNull();
    }

    [Test]
    public async Task StandaloneLesson_ThenSeriesAtSameClub_Conflicts()
    {
        _db.Lessons.Add(BuildStandaloneLesson(ClubA));
        await _db.SaveChangesAsync();

        LessonSerie series = BuildSeries(ClubA);
        _db.LessonSeries.Add(series);
        await _db.SaveChangesAsync();

        // Simuleert de check die AddLessonAsync doet vóór het opslaan van de nieuwe reeks-les.
        Lesson? conflict = await _repo.FindCourtConflictAsync(
            OrgId, "Baan 2", Date, Start, End, tennisClubId: series.TennisClubId);

        conflict.Should().NotBeNull();
    }

    [Test]
    public async Task SeriesLesson_ThenStandaloneAtDifferentClub_NoConflict()
    {
        LessonSerie series = BuildSeries(ClubA);
        _db.LessonSeries.Add(series);
        _db.Lessons.Add(BuildSeriesLesson(series));
        await _db.SaveChangesAsync();

        Lesson? conflict = await _repo.FindCourtConflictAsync(
            OrgId, "Baan 2", Date, Start, End, tennisClubId: ClubB);

        conflict.Should().BeNull();
    }

    [Test]
    public async Task StandaloneLesson_ThenSeriesAtDifferentClub_NoConflict()
    {
        _db.Lessons.Add(BuildStandaloneLesson(ClubA));
        await _db.SaveChangesAsync();

        LessonSerie series = BuildSeries(ClubB);
        _db.LessonSeries.Add(series);
        await _db.SaveChangesAsync();

        Lesson? conflict = await _repo.FindCourtConflictAsync(
            OrgId, "Baan 2", Date, Start, End, tennisClubId: series.TennisClubId);

        conflict.Should().BeNull();
    }

    [Test]
    public async Task TwoStandaloneLessons_DifferentClubs_NoConflict()
    {
        _db.Lessons.Add(BuildStandaloneLesson(ClubA));
        await _db.SaveChangesAsync();

        Lesson? conflict = await _repo.FindCourtConflictAsync(
            OrgId, "Baan 2", Date, Start, End, tennisClubId: ClubB);

        conflict.Should().BeNull();
    }

    [Test]
    public async Task TwoStandaloneLessons_SameClub_Conflicts()
    {
        _db.Lessons.Add(BuildStandaloneLesson(ClubA));
        await _db.SaveChangesAsync();

        Lesson? conflict = await _repo.FindCourtConflictAsync(
            OrgId, "Baan 2", Date, Start, End, tennisClubId: ClubA);

        conflict.Should().NotBeNull();
    }

    // ── Legacy / null-club fallback ──────────────────────────────────────────────

    [Test]
    public async Task LegacyStandaloneLessonWithoutClub_TreatedAsConflict_SafeFallback()
    {
        // Pré-migratie losse les: geen TennisClubId bekend.
        _db.Lessons.Add(BuildStandaloneLesson(clubId: null));
        await _db.SaveChangesAsync();

        // Nieuwe boeking heeft wél een gekende club — mag niet stilzwijgend toegelaten worden
        // want we kunnen niet bewijzen dat de legacy les elders plaatsvindt.
        Lesson? conflict = await _repo.FindCourtConflictAsync(
            OrgId, "Baan 2", Date, Start, End, tennisClubId: ClubA);

        conflict.Should().NotBeNull();
    }

    // ── Cancelled + self-exclusion ────────────────────────────────────────────────

    [Test]
    public async Task CancelledLesson_NeverConflicts()
    {
        _db.Lessons.Add(BuildStandaloneLesson(ClubA, cancelled: true));
        await _db.SaveChangesAsync();

        Lesson? conflict = await _repo.FindCourtConflictAsync(
            OrgId, "Baan 2", Date, Start, End, tennisClubId: ClubA);

        conflict.Should().BeNull();
    }

    [Test]
    public async Task ExcludedLessonId_DoesNotConflictWithItself()
    {
        Lesson lesson = BuildStandaloneLesson(ClubA);
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();

        Lesson? conflict = await _repo.FindCourtConflictAsync(
            OrgId, "Baan 2", Date, Start, End, excludeLessonId: lesson.Id, tennisClubId: ClubA);

        conflict.Should().BeNull();
    }

    [Test]
    public async Task NoTennisClubIdGiven_FallsBackToOrgWide_MatchesAnyClub()
    {
        _db.Lessons.Add(BuildStandaloneLesson(ClubB));
        await _db.SaveChangesAsync();

        // Wordt gebruikt wanneer de te checken les zelf geen gekende club heeft.
        Lesson? conflict = await _repo.FindCourtConflictAsync(
            OrgId, "Baan 2", Date, Start, End, tennisClubId: null);

        conflict.Should().NotBeNull();
    }
}
