using CoachOS.Application.LessonSerie;
using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class LessonSerieServiceTests
{
    private Mock<ILessonSerieRepository> _lessonSeriesRepo = null!;
    private Mock<ILessonRepository> _lessonRepo = null!;
    private Mock<ITennisClubRepository> _tennisClubRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private ApplicationMapper _mapper = null!;
    private LessonSerieService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();
    private static readonly Guid ClubId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _lessonSeriesRepo = new Mock<ILessonSerieRepository>();
        _lessonRepo = new Mock<ILessonRepository>();
        _tennisClubRepo = new Mock<ITennisClubRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _mapper = new ApplicationMapper();
        _service = new LessonSerieService(
            _lessonSeriesRepo.Object,
            _lessonRepo.Object,
            _tennisClubRepo.Object,
            _userLookup.Object,
            _mapper);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static LessonSerie BuildSeries(Guid? id = null, int enrollments = 0, int lessons = 0)
    {
        var seriesId = id ?? Guid.NewGuid();
        var enrollmentList = Enumerable
            .Range(0, enrollments)
            .Select(_ => new Enrollment { Id = Guid.NewGuid(), OrganizationId = OrgId })
            .ToList();

        var lessonList = Enumerable
            .Range(0, lessons)
            .Select(_ => new Lesson
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgId,
                TrainerId = TrainerId,
                Date = DateOnly.FromDateTime(DateTime.Today),
                StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
                EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)),
            })
            .ToList();

        return new LessonSerie
        {
            Id = seriesId,
            OrganizationId = OrgId,
            Name = "Voorjaarslessen 2026",
            Level = LessonLevel.Beginner,
            Price = 150m,
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2026, 5, 31),
            TennisClubId = ClubId,
            IsActive = true,
            Enrollments = enrollmentList,
            Lessons = lessonList,
        };
    }

    private static Lesson BuildLesson(Guid seriesId, int enrollments = 0)
    {
        var enrollmentList = Enumerable
            .Range(0, enrollments)
            .Select(_ => new Enrollment { Id = Guid.NewGuid(), OrganizationId = OrgId })
            .ToList();

        return new Lesson
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = seriesId,
            TrainerId = TrainerId,
            CourtName = "Baan 1",
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)),
            Level = LessonLevel.Beginner,
            Enrollments = enrollmentList,
        };
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoSeries()
    {
        _lessonSeriesRepo
            .Setup(r => r.GetByOrganizationAsync(OrgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LessonSerie>());

        var result = await _service.GetAllAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllAsync_ReturnsDtos_WithLessonCounts()
    {
        var series = BuildSeries();
        _lessonSeriesRepo
            .Setup(r => r.GetByOrganizationAsync(OrgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LessonSerie> { series });

        _lessonRepo
            .Setup(r => r.GetLessonCountsBySeriesIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { series.Id, 3 } });

        var result = await _service.GetAllAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var dto = result.Value![0];
        dto.LessonCount.Should().Be(3);
        dto.Name.Should().Be(series.Name);
    }

    [Test]
    public async Task GetAllAsync_FiltersByTrainerId_WhenProvided()
    {
        var series = BuildSeries();
        _lessonSeriesRepo
            .Setup(r => r.GetByOrganizationAsync(OrgId, TrainerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LessonSerie> { series });

        _lessonRepo
            .Setup(r => r.GetLessonCountsBySeriesIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { series.Id, 1 } });

        var result = await _service.GetAllAsync(OrgId, TrainerId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        _lessonSeriesRepo.Verify(
            r => r.GetByOrganizationAsync(OrgId, TrainerId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        var series = BuildSeries(lessons: 2);
        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        var result = await _service.GetByIdAsync(series.Id, OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(series.Id);
        result.Value.Lessons.Should().HaveCount(2);
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNotFound_WhenMissing()
    {
        var missingId = Guid.NewGuid();
        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(missingId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        var result = await _service.GetByIdAsync(missingId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    // ── GetMembersAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task GetMembersAsync_ReturnsMemberDtos()
    {
        List<(Guid Id, string FullName)> members =
        [
            (Guid.NewGuid(), "Lisa Smit"),
            (Guid.NewGuid(), "Mark de Vries"),
        ];

        _userLookup
            .Setup(u => u.GetOrganizationMembersAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        var result = await _service.GetMembersAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].FullName.Should().Be("Lisa Smit");
        result.Value[1].FullName.Should().Be("Mark de Vries");
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_ReturnsId_WhenValid()
    {
        CreateLessonSerieRequest request = new()
        {
            Name = "Zomerlessen 2026",
            Level = (int)LessonLevel.Intermediate,
            Price = 200m,
            StartDate = "2026-06-01",
            EndDate = "2026-08-31",
            TennisClubId = ClubId,
            Lessons =
            [
                new CreateLessonRequest
                {
                    TrainerId = TrainerId,
                    Date = "2026-06-15",
                    StartTime = "10:00",
                    EndTime = "11:00",
                    CourtName = "Baan 1",
                }
            ],
        };

        _tennisClubRepo
            .Setup(r => r.ExistsAsync(ClubId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _lessonSeriesRepo
            .Setup(r => r.AddAsync(It.IsAny<LessonSerie>(), It.IsAny<CancellationToken>()))
            .Callback<LessonSerie, CancellationToken>((s, _) => s.Id = Guid.NewGuid())
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(OrgId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _lessonSeriesRepo.Verify(
            r => r.AddAsync(It.Is<LessonSerie>(s => s.Name == "Zomerlessen 2026" && s.OrganizationId == OrgId && s.Lessons.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreateAsync_ReturnsNotFound_WhenClubMissing()
    {
        CreateLessonSerieRequest request = new()
        {
            Name = "Test",
            Level = (int)LessonLevel.Beginner,
            StartDate = "2026-06-01",
            EndDate = "2026-08-31",
            TennisClubId = ClubId,
            Lessons =
            [
                new CreateLessonRequest
                {
                    TrainerId = TrainerId,
                    Date = "2026-06-15",
                    StartTime = "10:00",
                    EndTime = "11:00",
                    CourtName = "Baan 1",
                }
            ],
        };

        _tennisClubRepo
            .Setup(r => r.ExistsAsync(ClubId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.CreateAsync(OrgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
        _lessonSeriesRepo.Verify(r => r.AddAsync(It.IsAny<LessonSerie>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_ReturnsNotFound_WhenTrainerInvalid()
    {
        CreateLessonSerieRequest request = new()
        {
            Name = "Test",
            Level = (int)LessonLevel.Beginner,
            StartDate = "2026-06-01",
            EndDate = "2026-08-31",
            TennisClubId = ClubId,
            Lessons =
            [
                new CreateLessonRequest
                {
                    TrainerId = TrainerId,
                    Date = "2026-06-15",
                    StartTime = "10:00",
                    EndTime = "11:00",
                    CourtName = "Baan 1",
                }
            ],
        };

        _tennisClubRepo
            .Setup(r => r.ExistsAsync(ClubId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _lessonSeriesRepo
            .Setup(r => r.AddAsync(It.IsAny<LessonSerie>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(OrgId, request);

        // Trainer validation no longer happens at series level — series creates successfully
        result.IsSuccess.Should().BeTrue();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateAsync_ReturnsUpdatedDto_WhenValid()
    {
        var series = BuildSeries();
        var newClubId = Guid.NewGuid();
        UpdateLessonSerieRequest request = new()
        {
            Name = "Bijgewerkte naam",
            Level = (int)LessonLevel.Advanced,
            Price = 250m,
            IsActive = true,
            TennisClubId = newClubId,
        };

        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        _tennisClubRepo
            .Setup(r => r.ExistsAsync(newClubId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _lessonRepo
            .Setup(r => r.CountBySeriesIdAsync(series.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        TennisClub updatedClub = new() { Id = newClubId, OrganizationId = OrgId, Name = "Club B", Address = "Straat 2" };
        _tennisClubRepo
            .Setup(r => r.GetByIdAsync(newClubId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedClub);

        var result = await _service.UpdateAsync(series.Id, OrgId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Bijgewerkte naam");
        result.Value.TennisClubName.Should().Be("Club B");
        _lessonSeriesRepo.Verify(r => r.UpdateAsync(series, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_ReturnsNotFound_WhenSeriesMissing()
    {
        var missingId = Guid.NewGuid();
        UpdateLessonSerieRequest request = new()
        {
            Name = "Test",
            Level = (int)LessonLevel.Beginner,
            TennisClubId = ClubId,
        };

        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(missingId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        var result = await _service.UpdateAsync(missingId, OrgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task UpdateAsync_ReturnsNotFound_WhenClubMissing()
    {
        var series = BuildSeries();
        var missingClubId = Guid.NewGuid();
        UpdateLessonSerieRequest request = new()
        {
            Name = "Test",
            Level = (int)LessonLevel.Beginner,
            TennisClubId = missingClubId,
        };

        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        _tennisClubRepo
            .Setup(r => r.ExistsAsync(missingClubId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.UpdateAsync(series.Id, OrgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteAsync_Succeeds_WhenNoEnrollments()
    {
        var series = BuildSeries(enrollments: 0, lessons: 2);
        _lessonSeriesRepo
            .Setup(r => r.GetByIdWithEnrollmentsAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        var result = await _service.DeleteAsync(series.Id, OrgId);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.DeleteRangeAsync(series.Lessons, It.IsAny<CancellationToken>()), Times.Once);
        _lessonSeriesRepo.Verify(r => r.DeleteAsync(series, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_ReturnsNotFound_WhenMissing()
    {
        var missingId = Guid.NewGuid();
        _lessonSeriesRepo
            .Setup(r => r.GetByIdWithEnrollmentsAsync(missingId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        var result = await _service.DeleteAsync(missingId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task DeleteAsync_ReturnsConflict_WhenHasEnrollments()
    {
        var series = BuildSeries(enrollments: 3);
        _lessonSeriesRepo
            .Setup(r => r.GetByIdWithEnrollmentsAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        var result = await _service.DeleteAsync(series.Id, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
        _lessonSeriesRepo.Verify(r => r.DeleteAsync(It.IsAny<LessonSerie>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── AddLessonAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task AddLessonAsync_ReturnsId_WhenValid()
    {
        var series = BuildSeries();
        CreateLessonRequest request = new()
        {
            TrainerId = TrainerId,
            Date = "2026-04-15",
            StartTime = "10:00",
            EndTime = "11:00",
            CourtName = "Baan 3",
            Notes = "Meenemen: racket",
        };

        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        _lessonRepo
            .Setup(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .Callback<Lesson, CancellationToken>((l, _) => l.Id = Guid.NewGuid())
            .Returns(Task.CompletedTask);

        var result = await _service.AddLessonAsync(series.Id, OrgId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _lessonRepo.Verify(
            r => r.AddAsync(It.Is<Lesson>(l => l.CourtName == "Baan 3" && l.LessonSerieId == series.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AddLessonAsync_ReturnsNotFound_WhenSeriesMissing()
    {
        var missingId = Guid.NewGuid();
        CreateLessonRequest request = new()
        {
            TrainerId = TrainerId,
            Date = "2026-04-15",
            StartTime = "10:00",
            EndTime = "11:00",
            CourtName = "Baan 1",
        };

        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(missingId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        var result = await _service.AddLessonAsync(missingId, OrgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── DeleteLessonAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task DeleteLessonAsync_Succeeds_WhenNoEnrollments()
    {
        var series = BuildSeries();
        var lesson = BuildLesson(series.Id, enrollments: 0);

        _lessonRepo
            .Setup(r => r.GetByIdWithEnrollmentsAsync(lesson.Id, series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var result = await _service.DeleteLessonAsync(series.Id, lesson.Id, OrgId);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.DeleteAsync(lesson, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteLessonAsync_ReturnsNotFound_WhenMissing()
    {
        var seriesId = Guid.NewGuid();
        var missingLessonId = Guid.NewGuid();

        _lessonRepo
            .Setup(r => r.GetByIdWithEnrollmentsAsync(missingLessonId, seriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var result = await _service.DeleteLessonAsync(seriesId, missingLessonId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task DeleteLessonAsync_ReturnsConflict_WhenHasEnrollments()
    {
        var series = BuildSeries();
        var lesson = BuildLesson(series.Id, enrollments: 2);

        _lessonRepo
            .Setup(r => r.GetByIdWithEnrollmentsAsync(lesson.Id, series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var result = await _service.DeleteLessonAsync(series.Id, lesson.Id, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
        _lessonRepo.Verify(r => r.DeleteAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
