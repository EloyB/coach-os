using CoachOS.Application.Configuration;
using CoachOS.Application.LessonReschedule;
using CoachOS.Application.LessonReschedule.DTOs;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Application.StandaloneLessons;
using CoachOS.Application.StandaloneLessons.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Baan-bezettingscheck over de vier services die lessen inplannen of verplaatsen.
/// De trim + case-insensitive normalisatie zit in LessonRepository.FindCourtConflictAsync;
/// deze fixture emuleert dat gedrag in de mock (zie <see cref="SetupOccupiedCourt"/>)
/// zodat het service-contract getest wordt zonder EF-harnas.
/// </summary>
[TestFixture]
public class LessonCourtConflictTests
{
    private Mock<ILessonSerieRepository> _serieRepo = null!;
    private Mock<ILessonRepository> _lessonRepo = null!;
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<ITennisClubRepository> _tennisClubRepo = null!;
    private Mock<ILessonInvitationRepository> _invitationRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IEmailService> _emailService = null!;
    private Mock<IMollieConnectionRepository> _mollieConnectionRepo = null!;
    private Mock<IScheduleAssignmentRepository> _scheduleAssignmentRepo = null!;
    private Mock<ITimeSlotPreferenceRepository> _timeSlotPreferenceRepo = null!;
    private ApplicationMapper _mapper = null!;

    private LessonSerieService _serieService = null!;
    private StandaloneLessonService _standaloneService = null!;
    private LessonRescheduleService _rescheduleService = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid OtherOrgId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();
    private static readonly Guid ClubId = Guid.NewGuid();

    private static readonly DateOnly LessonDate = new(2026, 12, 5);

    [SetUp]
    public void SetUp()
    {
        _serieRepo = new Mock<ILessonSerieRepository>();
        _lessonRepo = new Mock<ILessonRepository>();
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _tennisClubRepo = new Mock<ITennisClubRepository>();
        _invitationRepo = new Mock<ILessonInvitationRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _emailService = new Mock<IEmailService>();
        _mollieConnectionRepo = new Mock<IMollieConnectionRepository>();
        _scheduleAssignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _timeSlotPreferenceRepo = new Mock<ITimeSlotPreferenceRepository>();
        _mapper = new ApplicationMapper();

        _serieService = new LessonSerieService(
            _serieRepo.Object,
            _lessonRepo.Object,
            _enrollmentRepo.Object,
            _tennisClubRepo.Object,
            _userLookup.Object,
            _emailService.Object,
            _mollieConnectionRepo.Object,
            _scheduleAssignmentRepo.Object,
            _timeSlotPreferenceRepo.Object,
            _invitationRepo.Object,
            _mapper);

        _standaloneService = new StandaloneLessonService(
            _lessonRepo.Object,
            _invitationRepo.Object,
            _tennisClubRepo.Object,
            _userLookup.Object,
            _emailService.Object,
            _mapper,
            Options.Create(new AppOptions
            {
                StandaloneLessonInvitationBaseUrl = "http://localhost:5317/invitation"
            }),
            NullLogger<StandaloneLessonService>.Instance);

        _rescheduleService = new LessonRescheduleService(
            _lessonRepo.Object,
            _invitationRepo.Object,
            _enrollmentRepo.Object,
            _serieRepo.Object,
            _userLookup.Object,
            _emailService.Object,
            NullLogger<LessonRescheduleService>.Instance);

        _userLookup
            .Setup(u => u.IsActiveTrainerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userLookup
            .Setup(u => u.GetUserNameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Sara Trainer");

        _tennisClubRepo
            .Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Default: geen trainer-conflict, geen baan-conflict.
        _lessonRepo
            .Setup(r => r.FindTrainerConflictAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);
        _lessonRepo
            .Setup(r => r.FindCourtConflictAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(),
                It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        _invitationRepo
            .Setup(r => r.GetByLessonAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LessonInvitation>());
        _enrollmentRepo
            .Setup(r => r.GetBySeriesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Emuleert LessonRepository.FindCourtConflictAsync: matcht op organizationId
    /// en op een getrimde, hoofdletter-ongevoelige baannaam.
    /// </summary>
    private void SetupOccupiedCourt(string occupiedCourtName, string seriesName = "Voorjaarslessen")
    {
        Lesson occupying = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            Date = LessonDate,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(13, 0),
            CourtName = occupiedCourtName,
            LessonSerie = new Domain.Entities.LessonSerie { Id = Guid.NewGuid(), Name = seriesName },
        };

        _lessonRepo
            .Setup(r => r.FindCourtConflictAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(),
                It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid orgId, string courtName, DateOnly date, TimeOnly start, TimeOnly end,
                Guid? excludeId, Guid? _, CancellationToken _) =>
            {
                if (orgId != occupying.OrganizationId) return null;
                if (date != occupying.Date) return null;
                if (excludeId is not null && excludeId == occupying.Id) return null;
                if (!string.Equals(courtName.Trim(), occupying.CourtName!.Trim(),
                        StringComparison.OrdinalIgnoreCase)) return null;
                if (start >= occupying.EndTime || end <= occupying.StartTime) return null;
                return occupying;
            });
    }

    private Domain.Entities.LessonSerie BuildSeries()
    {
        Domain.Entities.LessonSerie series = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            Name = "Najaarslessen",
            TennisClubId = ClubId,
        };
        _serieRepo
            .Setup(r => r.GetByIdAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        return series;
    }

    private static CreateLessonRequest BuildCreateLessonRequest(string? courtName)
        => new()
        {
            Date = LessonDate.ToString("yyyy-MM-dd"),
            StartTime = "10:00",
            EndTime = "11:00",
            CourtName = courtName,
            TrainerId = TrainerId,
            MaxStudents = 4,
        };

    // ── LessonSerieService.AddLessonAsync ────────────────────────────────────

    [Test]
    public async Task AddLessonAsync_CourtOccupied_ReturnsConflictWithCourtTimeAndSeriesName()
    {
        Domain.Entities.LessonSerie series = BuildSeries();
        SetupOccupiedCourt("Baan 1");

        Result<Guid> result = await _serieService.AddLessonAsync(
            series.Id, OrgId, BuildCreateLessonRequest("Baan 1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        result.Errors[0].Message.Should().Be(
            "Baan 1 is op 05/12/2026 van 09:00–13:00 al bezet door reeks Voorjaarslessen.");

        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AddLessonAsync_CourtFree_ReturnsSuccessAndPersists()
    {
        Domain.Entities.LessonSerie series = BuildSeries();
        SetupOccupiedCourt("Baan 9");

        Result<Guid> result = await _serieService.AddLessonAsync(
            series.Id, OrgId, BuildCreateLessonRequest("Baan 1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddLessonAsync_CourtNameDiffersInCaseAndWhitespace_StillConflicts()
    {
        Domain.Entities.LessonSerie series = BuildSeries();
        SetupOccupiedCourt("Baan 1");

        Result<Guid> result = await _serieService.AddLessonAsync(
            series.Id, OrgId, BuildCreateLessonRequest("  baan 1 "), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        // De baannaam in de melding is getrimd weergegeven.
        result.Errors[0].Message.Should().StartWith("baan 1 is op 05/12/2026");
    }

    [Test]
    public async Task AddLessonAsync_NullCourtName_SkipsCheckEntirely()
    {
        Domain.Entities.LessonSerie series = BuildSeries();
        SetupOccupiedCourt("Baan 1");

        Result<Guid> result = await _serieService.AddLessonAsync(
            series.Id, OrgId, BuildCreateLessonRequest(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.FindCourtConflictAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(),
            It.IsAny<TimeOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AddLessonAsync_BlankCourtName_SkipsCheckEntirely()
    {
        Domain.Entities.LessonSerie series = BuildSeries();
        SetupOccupiedCourt("Baan 1");

        Result<Guid> result = await _serieService.AddLessonAsync(
            series.Id, OrgId, BuildCreateLessonRequest("   "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.FindCourtConflictAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(),
            It.IsAny<TimeOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AddLessonAsync_SameCourtInOtherOrganization_NoConflict()
    {
        Domain.Entities.LessonSerie otherSeries = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OtherOrgId,
            Name = "Andere org reeks",
            TennisClubId = ClubId,
        };
        _serieRepo
            .Setup(r => r.GetByIdAsync(otherSeries.Id, OtherOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherSeries);

        // Baan 1 is bezet, maar in OrgId — niet in OtherOrgId.
        SetupOccupiedCourt("Baan 1");

        Result<Guid> result = await _serieService.AddLessonAsync(
            otherSeries.Id, OtherOrgId, BuildCreateLessonRequest("Baan 1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.FindCourtConflictAsync(
            OtherOrgId, "Baan 1", LessonDate, It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddLessonAsync_ScopesCourtConflictCheckToSeriesClub()
    {
        // Regressie: een baan-conflictcheck mag alleen botsen binnen dezelfde club. Zonder deze
        // scoping flipt "Baan 2" om 19u bij club A onterecht tegen een gelijknamige baan bij club B
        // binnen dezelfde organisatie.
        Domain.Entities.LessonSerie series = BuildSeries();
        SetupOccupiedCourt("Baan 1");

        await _serieService.AddLessonAsync(
            series.Id, OrgId, BuildCreateLessonRequest("Baan 1"), CancellationToken.None);

        _lessonRepo.Verify(r => r.FindCourtConflictAsync(
            OrgId, "Baan 1", LessonDate, It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
            It.IsAny<Guid?>(), series.TennisClubId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── LessonSerieService.UpdateLessonAsync ─────────────────────────────────

    [Test]
    public async Task UpdateLessonAsync_MovesOntoOccupiedCourt_ReturnsConflict()
    {
        Domain.Entities.LessonSerie series = BuildSeries();
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = series.Id,
            Date = LessonDate,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            CourtName = "Baan 3",
            TrainerId = TrainerId,
        };
        _lessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        SetupOccupiedCourt("Baan 1");

        UpdateLessonRequest request = new() { TrainerId = TrainerId, CourtName = "Baan 1" };

        Result<LessonDto> result = await _serieService.UpdateLessonAsync(
            series.Id, lesson.Id, OrgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        result.Errors[0].Message.Should().Contain("Voorjaarslessen");
        _lessonRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateLessonAsync_KeepsOwnCourtSlot_ExcludesItselfSoNoConflict()
    {
        Domain.Entities.LessonSerie series = BuildSeries();
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = series.Id,
            Date = LessonDate,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            CourtName = "Baan 1",
            TrainerId = TrainerId,
        };
        _lessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        // De enige bezetter van Baan 1 is de les zelf → excludeLessonId filtert hem weg.
        Lesson self = new()
        {
            Id = lesson.Id,
            OrganizationId = OrgId,
            Date = LessonDate,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            CourtName = "Baan 1",
        };
        _lessonRepo
            .Setup(r => r.FindCourtConflictAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(),
                It.IsAny<TimeOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, string _, DateOnly _, TimeOnly _, TimeOnly _,
                Guid? excludeId, Guid? _, CancellationToken _) => excludeId == self.Id ? null : self);

        UpdateLessonRequest request = new() { TrainerId = TrainerId, Notes = "gewijzigd" };

        Result<LessonDto> result = await _serieService.UpdateLessonAsync(
            series.Id, lesson.Id, OrgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.FindCourtConflictAsync(
            OrgId, "Baan 1", LessonDate, It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
            lesson.Id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateLessonAsync_LessonWithoutCourt_SkipsCheck()
    {
        Domain.Entities.LessonSerie series = BuildSeries();
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = series.Id,
            Date = LessonDate,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            CourtName = null,
            TrainerId = TrainerId,
        };
        _lessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        SetupOccupiedCourt("Baan 1");

        Result<LessonDto> result = await _serieService.UpdateLessonAsync(
            series.Id, lesson.Id, OrgId, new UpdateLessonRequest { TrainerId = TrainerId },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.FindCourtConflictAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(),
            It.IsAny<TimeOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── StandaloneLessonService.CreateAsync ──────────────────────────────────

    private static CreateStandaloneLessonRequest BuildStandaloneRequest(string? courtName)
        => new()
        {
            Date = LessonDate.ToString("yyyy-MM-dd"),
            StartTime = "10:00",
            DurationMinutes = 60,
            CourtName = courtName,
            TennisClubId = ClubId,
            Level = (int)LessonLevel.Beginner,
            TrainerId = TrainerId,
            MaxParticipants = 4,
            ParticipantEmails = new List<string> { "alice@test.com" },
        };

    [Test]
    public async Task StandaloneCreateAsync_CourtOccupied_ReturnsConflict()
    {
        SetupOccupiedCourt("Baan 1");

        Result<Guid> result = await _standaloneService.CreateAsync(
            OrgId, BuildStandaloneRequest("Baan 1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        result.Errors[0].Message.Should().Be(
            "Baan 1 is op 05/12/2026 van 09:00–13:00 al bezet door reeks Voorjaarslessen.");
        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task StandaloneCreateAsync_CourtNameCaseAndWhitespaceDiffer_StillConflicts()
    {
        SetupOccupiedCourt("Baan 1");

        Result<Guid> result = await _standaloneService.CreateAsync(
            OrgId, BuildStandaloneRequest("BAAN 1  "), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
    }

    [Test]
    public async Task StandaloneCreateAsync_CourtFree_Succeeds()
    {
        SetupOccupiedCourt("Baan 9");

        Result<Guid> result = await _standaloneService.CreateAsync(
            OrgId, BuildStandaloneRequest("Baan 1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StandaloneCreateAsync_NoCourtName_SkipsCheck()
    {
        SetupOccupiedCourt("Baan 1");

        Result<Guid> result = await _standaloneService.CreateAsync(
            OrgId, BuildStandaloneRequest(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.FindCourtConflictAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(),
            It.IsAny<TimeOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task StandaloneCreateAsync_OtherOrganization_NoConflict()
    {
        SetupOccupiedCourt("Baan 1");

        Result<Guid> result = await _standaloneService.CreateAsync(
            OtherOrgId, BuildStandaloneRequest("Baan 1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.FindCourtConflictAsync(
            OtherOrgId, "Baan 1", LessonDate, It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
            null, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── LessonRescheduleService.RescheduleAsync ──────────────────────────────

    private Lesson BuildReschedulableLesson(string? courtName, Guid? orgId = null, Guid? tennisClubId = null)
    {
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId ?? OrgId,
            LessonSerieId = null,
            TennisClubId = tennisClubId,
            Date = LessonDate.AddDays(-7),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            TrainerId = TrainerId,
            CourtName = courtName,
            MaxStudents = 4,
        };
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(lesson.Id, lesson.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        return lesson;
    }

    private static RescheduleLessonRequest BuildRescheduleRequest()
        => new(LessonDate.ToString("yyyy-MM-dd"), "10:00", "11:00", null);

    [Test]
    public async Task RescheduleAsync_TargetSlotCourtOccupied_ReturnsConflict()
    {
        Lesson lesson = BuildReschedulableLesson("Baan 1");
        SetupOccupiedCourt("Baan 1");

        Result<RescheduleLessonResultDto> result = await _rescheduleService.RescheduleAsync(
            OrgId, lesson.Id, BuildRescheduleRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        result.Errors[0].Message.Should().Be(
            "Baan 1 is op 05/12/2026 van 09:00–13:00 al bezet door reeks Voorjaarslessen.");
        lesson.IsCancelled.Should().BeFalse();
        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RescheduleAsync_TargetSlotCourtFree_Succeeds()
    {
        Lesson lesson = BuildReschedulableLesson("Baan 1");
        SetupOccupiedCourt("Baan 9");

        Result<RescheduleLessonResultDto> result = await _rescheduleService.RescheduleAsync(
            OrgId, lesson.Id, BuildRescheduleRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        lesson.IsCancelled.Should().BeTrue();
    }

    [Test]
    public async Task RescheduleAsync_LessonWithoutCourt_SkipsCheck()
    {
        Lesson lesson = BuildReschedulableLesson(null);
        SetupOccupiedCourt("Baan 1");

        Result<RescheduleLessonResultDto> result = await _rescheduleService.RescheduleAsync(
            OrgId, lesson.Id, BuildRescheduleRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _lessonRepo.Verify(r => r.FindCourtConflictAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(),
            It.IsAny<TimeOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RescheduleAsync_ExcludesItselfFromCourtConflict()
    {
        Lesson lesson = BuildReschedulableLesson("Baan 1");
        SetupOccupiedCourt("Baan 1");

        await _rescheduleService.RescheduleAsync(
            OrgId, lesson.Id, BuildRescheduleRequest(), CancellationToken.None);

        _lessonRepo.Verify(r => r.FindCourtConflictAsync(
            OrgId, "Baan 1", LessonDate, It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
            lesson.Id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RescheduleAsync_OtherOrganizationOccupiesSameCourt_NoConflict()
    {
        Lesson lesson = BuildReschedulableLesson("Baan 1", OtherOrgId);
        SetupOccupiedCourt("Baan 1"); // bezet binnen OrgId

        Result<RescheduleLessonResultDto> result = await _rescheduleService.RescheduleAsync(
            OtherOrgId, lesson.Id, BuildRescheduleRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task RescheduleAsync_StandaloneLesson_ScopesCourtConflictCheckToOwnClub()
    {
        // Regressie: een losse les draagt sinds de club-koppeling haar eigen TennisClubId — het
        // verplaatsen moet die club doorgeven aan de conflictcheck, niet org-breed checken.
        Lesson lesson = BuildReschedulableLesson("Baan 1", tennisClubId: ClubId);

        await _rescheduleService.RescheduleAsync(
            OrgId, lesson.Id, BuildRescheduleRequest(), CancellationToken.None);

        _lessonRepo.Verify(r => r.FindCourtConflictAsync(
            OrgId, "Baan 1", LessonDate, It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
            lesson.Id, ClubId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RescheduleAsync_CopiesTennisClubIdToNewLesson()
    {
        Lesson lesson = BuildReschedulableLesson("Baan 1", tennisClubId: ClubId);

        Lesson? captured = null;
        _lessonRepo
            .Setup(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .Callback<Lesson, CancellationToken>((l, _) => captured = l)
            .Returns(Task.CompletedTask);

        await _rescheduleService.RescheduleAsync(
            OrgId, lesson.Id, BuildRescheduleRequest(), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TennisClubId.Should().Be(ClubId);
    }
}
