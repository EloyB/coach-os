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
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<ITennisClubRepository> _tennisClubRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IEmailService> _emailService = null!;
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
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _tennisClubRepo = new Mock<ITennisClubRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _emailService = new Mock<IEmailService>();
        _mapper = new ApplicationMapper();
        _service = new LessonSerieService(
            _lessonSeriesRepo.Object,
            _lessonRepo.Object,
            _enrollmentRepo.Object,
            _tennisClubRepo.Object,
            _userLookup.Object,
            _emailService.Object,
            _mapper);

        // Default: enrollment counts returnen lege dictionary (geen inschrijvingen).
        _enrollmentRepo
            .Setup(r => r.CountActiveBySeriesIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        // Default: alle trainers valideren als actief binnen de org. Individuele tests
        // kunnen dit overriden voor negatieve scenarios (invalid/cross-tenant trainer).
        _userLookup
            .Setup(u => u.IsActiveTrainerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static LessonSerie BuildSeries(Guid? id = null, int enrollments = 0, int lessons = 0, int templateEntries = 0)
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

        var templateList = Enumerable
            .Range(0, templateEntries)
            .Select(i => new WeeklyTemplateEntry
            {
                Id = Guid.NewGuid(),
                LessonSerieId = seriesId,
                DayOfWeek = i,
                StartTime = new TimeOnly(17, 0),
                EndTime = new TimeOnly(18, 0),
                TrainerId = TrainerId,
                CourtName = $"Baan {i + 1}",
                MaxStudents = 4,
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
            WeeklyTemplate = templateList,
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
        var series = BuildSeries(lessons: 2, templateEntries: 3);
        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        var result = await _service.GetByIdAsync(series.Id, OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(series.Id);
        result.Value.Lessons.Should().HaveCount(2);
        result.Value.WeeklyTemplate.Should().HaveCount(3);
        result.Value.WeeklyTemplate[0].DayOfWeek.Should().Be(0);
        result.Value.WeeklyTemplate[0].CourtName.Should().Be("Baan 1");
        result.Value.WeeklyTemplate[0].MaxStudents.Should().Be(4);
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
            MaxRegistrations = 20,
            WeeklyTemplate =
            [
                new WeeklyTemplateEntryRequest
                {
                    DayOfWeek = 0,
                    StartTime = "17:00",
                    EndTime = "18:00",
                    TrainerId = TrainerId,
                    CourtName = "Baan 2",
                    MaxStudents = 4,
                },
                new WeeklyTemplateEntryRequest
                {
                    DayOfWeek = 2,
                    StartTime = "17:00",
                    EndTime = "18:00",
                    TrainerId = TrainerId,
                    CourtName = "Baan 1",
                    MaxStudents = 4,
                },
            ],
            Lessons =
            [
                new CreateLessonRequest
                {
                    TrainerId = TrainerId,
                    Date = "2026-06-15",
                    StartTime = "10:00",
                    EndTime = "11:00",
                    CourtName = "Baan 1",
                    MaxStudents = 4,
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
            r => r.AddAsync(It.Is<LessonSerie>(s =>
                s.Name == "Zomerlessen 2026"
                && s.OrganizationId == OrgId
                && s.Lessons.Count == 1
                && s.WeeklyTemplate.Count == 2
                && s.MaxRegistrations == 20),
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
    public async Task CreateAsync_ReturnsValidation_WhenTrainerBelongsToAnotherOrg()
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

        // Simuleer cross-tenant trainer: IsActiveTrainerAsync geeft false voor deze org.
        _userLookup
            .Setup(u => u.IsActiveTrainerAsync(TrainerId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.CreateAsync(OrgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
        _lessonSeriesRepo.Verify(r => r.AddAsync(It.IsAny<LessonSerie>(), It.IsAny<CancellationToken>()), Times.Never);
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

    // ── CreateAsync — WeeklyTemplate ─────────────────────────────────────────

    [Test]
    public async Task CreateAsync_PersistsWeeklyTemplateEntries()
    {
        LessonSerie? captured = null;
        CreateLessonSerieRequest request = new()
        {
            Name = "Met template",
            Price = 100m,
            StartDate = "2026-06-01",
            EndDate = "2026-08-31",
            TennisClubId = ClubId,
            WeeklyTemplate =
            [
                new WeeklyTemplateEntryRequest
                {
                    DayOfWeek = 0,
                    StartTime = "17:00",
                    EndTime = "18:00",
                    TrainerId = TrainerId,
                    CourtName = "Baan 2",
                    MaxStudents = 4,
                },
            ],
            Lessons =
            [
                new CreateLessonRequest
                {
                    TrainerId = TrainerId,
                    Date = "2026-06-01",
                    StartTime = "17:00",
                    EndTime = "18:00",
                    CourtName = "Baan 2",
                    MaxStudents = 4,
                },
            ],
        };

        _tennisClubRepo
            .Setup(r => r.ExistsAsync(ClubId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _lessonSeriesRepo
            .Setup(r => r.AddAsync(It.IsAny<LessonSerie>(), It.IsAny<CancellationToken>()))
            .Callback<LessonSerie, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(OrgId, request);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.WeeklyTemplate.Should().HaveCount(1);
        var entry = captured.WeeklyTemplate.First();
        entry.DayOfWeek.Should().Be(0);
        entry.StartTime.Should().Be(new TimeOnly(17, 0));
        entry.EndTime.Should().Be(new TimeOnly(18, 0));
        entry.TrainerId.Should().Be(TrainerId);
        entry.CourtName.Should().Be("Baan 2");
        entry.MaxStudents.Should().Be(4);
    }

    [Test]
    public async Task CreateAsync_ReturnsValidation_WhenWeeklyTemplateHasExactDuplicateSlot()
    {
        CreateLessonSerieRequest request = new()
        {
            Name = "Dubbele slot",
            Price = 100m,
            StartDate = "2026-06-01",
            EndDate = "2026-08-31",
            TennisClubId = ClubId,
            WeeklyTemplate =
            [
                new WeeklyTemplateEntryRequest { DayOfWeek = 0, StartTime = "10:00", EndTime = "11:00", TrainerId = TrainerId, CourtName = "Baan 1", MaxStudents = 4 },
                new WeeklyTemplateEntryRequest { DayOfWeek = 0, StartTime = "10:00", EndTime = "11:00", TrainerId = TrainerId, CourtName = "Baan 1", MaxStudents = 4 },
            ],
        };

        _tennisClubRepo
            .Setup(r => r.ExistsAsync(ClubId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(OrgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
        _lessonSeriesRepo.Verify(r => r.AddAsync(It.IsAny<LessonSerie>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Regression (issue #140): two slots on the same day/start/court with DIFFERENT end times used to
    // slip past the duplicate guard (which keyed on EndTime) and hit the unique index
    // IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime_CourtName → DbUpdateException → HTTP 500.
    // The guard must now reject it cleanly, matching the index (no EndTime in the key).
    [Test]
    public async Task CreateAsync_ReturnsValidation_WhenSlotsShareDayStartAndCourtButDifferentEnd()
    {
        CreateLessonSerieRequest request = new()
        {
            Name = "Zelfde start, andere eindtijd",
            Price = 100m,
            StartDate = "2026-06-01",
            EndDate = "2026-08-31",
            TennisClubId = ClubId,
            WeeklyTemplate =
            [
                new WeeklyTemplateEntryRequest { DayOfWeek = 0, StartTime = "10:00", EndTime = "11:00", TrainerId = TrainerId, CourtName = "Baan 1", MaxStudents = 4 },
                new WeeklyTemplateEntryRequest { DayOfWeek = 0, StartTime = "10:00", EndTime = "12:00", TrainerId = TrainerId, CourtName = "Baan 1", MaxStudents = 4 },
            ],
        };

        _tennisClubRepo
            .Setup(r => r.ExistsAsync(ClubId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(OrgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
        _lessonSeriesRepo.Verify(r => r.AddAsync(It.IsAny<LessonSerie>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_Succeeds_WhenSlotsShareDayAndStartButDifferentCourt()
    {
        CreateLessonSerieRequest request = new()
        {
            Name = "Parallelle banen",
            Price = 100m,
            StartDate = "2026-06-01",
            EndDate = "2026-08-31",
            TennisClubId = ClubId,
            WeeklyTemplate =
            [
                new WeeklyTemplateEntryRequest { DayOfWeek = 0, StartTime = "10:00", EndTime = "11:00", TrainerId = TrainerId, CourtName = "Baan 1", MaxStudents = 4 },
                new WeeklyTemplateEntryRequest { DayOfWeek = 0, StartTime = "10:00", EndTime = "11:00", TrainerId = TrainerId, CourtName = "Baan 2", MaxStudents = 4 },
            ],
            Lessons =
            [
                new CreateLessonRequest { TrainerId = TrainerId, Date = "2026-06-01", StartTime = "10:00", EndTime = "11:00", CourtName = "Baan 1", MaxStudents = 4 },
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
        _lessonSeriesRepo.Verify(r => r.AddAsync(It.IsAny<LessonSerie>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_PersistsMaxStudentsOnLesson()
    {
        LessonSerie? captured = null;
        CreateLessonSerieRequest request = new()
        {
            Name = "Met maxStudents",
            Price = 100m,
            StartDate = "2026-06-01",
            EndDate = "2026-08-31",
            TennisClubId = ClubId,
            Lessons =
            [
                new CreateLessonRequest
                {
                    TrainerId = TrainerId,
                    Date = "2026-06-01",
                    StartTime = "10:00",
                    EndTime = "11:00",
                    CourtName = "Baan 1",
                    MaxStudents = 6,
                },
            ],
        };

        _tennisClubRepo
            .Setup(r => r.ExistsAsync(ClubId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _lessonSeriesRepo
            .Setup(r => r.AddAsync(It.IsAny<LessonSerie>(), It.IsAny<CancellationToken>()))
            .Callback<LessonSerie, CancellationToken>((s, _) => captured = s)
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(OrgId, request);

        result.IsSuccess.Should().BeTrue();
        captured!.Lessons.Should().HaveCount(1);
        captured.Lessons.First().MaxStudents.Should().Be(6);
    }

    // ── GetByIdAsync — WeeklyTemplate sorting ────────────────────────────────

    [Test]
    public async Task GetByIdAsync_ReturnsWeeklyTemplateSortedByDayAndTime()
    {
        var series = BuildSeries(lessons: 1);
        series.WeeklyTemplate = new List<WeeklyTemplateEntry>
        {
            new() { Id = Guid.NewGuid(), DayOfWeek = 2, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), TrainerId = TrainerId, CourtName = "Baan 1", MaxStudents = 4 },
            new() { Id = Guid.NewGuid(), DayOfWeek = 0, StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(18, 0), TrainerId = TrainerId, CourtName = "Baan 2", MaxStudents = 4 },
            new() { Id = Guid.NewGuid(), DayOfWeek = 0, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), TrainerId = TrainerId, CourtName = "Baan 1", MaxStudents = 4 },
        };

        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        var result = await _service.GetByIdAsync(series.Id, OrgId);

        result.IsSuccess.Should().BeTrue();
        var template = result.Value!.WeeklyTemplate;
        template.Should().HaveCount(3);
        template[0].DayOfWeek.Should().Be(0);
        template[0].StartTime.Should().Be("09:00");
        template[1].DayOfWeek.Should().Be(0);
        template[1].StartTime.Should().Be("17:00");
        template[2].DayOfWeek.Should().Be(2);
    }

    // ── UpdateLessonAsync — Les annuleren ─────────────────────────────────────

    [Test]
    public async Task UpdateLessonAsync_CancelLesson_SetsCancelledAndReason()
    {
        LessonSerie series = BuildSeries();
        Lesson lesson = BuildLesson(series.Id);

        _lessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentRepo
            .Setup(r => r.GetBySeriesAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Enrollment>());

        UpdateLessonRequest request = new() { IsCancelled = true, CancellationReason = "Trainer ziek" };

        Result<LessonDto> result = await _service.UpdateLessonAsync(series.Id, lesson.Id, OrgId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsCancelled.Should().BeTrue();
        result.Value.CancellationReason.Should().Be("Trainer ziek");
    }

    [Test]
    public async Task UpdateLessonAsync_CancelLesson_SendsEmailToActiveEnrollments()
    {
        LessonSerie series = BuildSeries();
        Lesson lesson = BuildLesson(series.Id);

        List<Domain.Entities.Enrollment> enrollments =
        [
            new Domain.Entities.Enrollment
            {
                Id = Guid.NewGuid(), OrganizationId = OrgId,
                StudentName = "Jan Janssen", StudentEmail = "jan@example.com",
                Status = Domain.Enums.EnrollmentStatus.Confirmed,
            },
            new Domain.Entities.Enrollment
            {
                Id = Guid.NewGuid(), OrganizationId = OrgId,
                StudentName = "Sofie Peeters", StudentEmail = "sofie@example.com",
                Status = Domain.Enums.EnrollmentStatus.Pending,
            },
            new Domain.Entities.Enrollment
            {
                Id = Guid.NewGuid(), OrganizationId = OrgId,
                StudentName = "Marc Dubois", StudentEmail = "marc@example.com",
                Status = Domain.Enums.EnrollmentStatus.Cancelled,
            },
        ];

        _lessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentRepo
            .Setup(r => r.GetBySeriesAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        UpdateLessonRequest request = new() { IsCancelled = true, CancellationReason = "Overmacht" };

        await _service.UpdateLessonAsync(series.Id, lesson.Id, OrgId, request);

        // Enkel de 2 actieve studenten (Confirmed + Pending) krijgen een mail; de Cancelled niet.
        _emailService.Verify(
            e => e.SendLessonCancellationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _emailService.Verify(
            e => e.SendLessonCancellationAsync(
                "jan@example.com", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailService.Verify(
            e => e.SendLessonCancellationAsync(
                "sofie@example.com", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task UpdateLessonAsync_CancelAlreadyCancelledLesson_DoesNotSendEmail()
    {
        LessonSerie series = BuildSeries();
        Lesson lesson = BuildLesson(series.Id);
        lesson.IsCancelled = true;
        lesson.CancellationReason = "Al eerder geannuleerd";

        _lessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        UpdateLessonRequest request = new() { IsCancelled = true };

        await _service.UpdateLessonAsync(series.Id, lesson.Id, OrgId, request);

        _emailService.Verify(
            e => e.SendLessonCancellationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task UpdateLessonAsync_UndoCancel_ClearsCancellationReason()
    {
        LessonSerie series = BuildSeries();
        Lesson lesson = BuildLesson(series.Id);
        lesson.IsCancelled = true;
        lesson.CancellationReason = "Trainer ziek";

        _lessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _lessonSeriesRepo
            .Setup(r => r.GetByIdAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        UpdateLessonRequest request = new() { IsCancelled = false };

        Result<LessonDto> result = await _service.UpdateLessonAsync(series.Id, lesson.Id, OrgId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsCancelled.Should().BeFalse();
        result.Value.CancellationReason.Should().BeNull();
    }
}
