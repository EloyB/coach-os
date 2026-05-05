using CoachOS.Application.Configuration;
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

[TestFixture]
public class StandaloneLessonServiceTests
{
    private Mock<ILessonRepository> _lessonRepo = null!;
    private Mock<ILessonInvitationRepository> _invitationRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IEmailService> _emailService = null!;
    private ApplicationMapper _mapper = null!;
    private StandaloneLessonService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _lessonRepo = new Mock<ILessonRepository>();
        _invitationRepo = new Mock<ILessonInvitationRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _emailService = new Mock<IEmailService>();
        _mapper = new ApplicationMapper();

        IOptions<AppOptions> appOptions = Options.Create(new AppOptions
        {
            StandaloneLessonInvitationBaseUrl = "http://localhost:5317/invitation"
        });

        _service = new StandaloneLessonService(
            _lessonRepo.Object,
            _invitationRepo.Object,
            _userLookup.Object,
            _emailService.Object,
            _mapper,
            appOptions,
            NullLogger<StandaloneLessonService>.Instance);

        // Default: trainer is actief lid van de organisatie.
        _userLookup
            .Setup(u => u.IsActiveTrainerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userLookup
            .Setup(u => u.GetUserNameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Sara Trainer");

        _userLookup
            .Setup(u => u.GetUserNamesByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { { TrainerId, "Sara Trainer" } });

        // Default: geen trainer-conflict.
        _lessonRepo
            .Setup(r => r.FindTrainerConflictAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        // Default: geen bestaande invitations voor add-flow.
        _invitationRepo
            .Setup(r => r.ExistsByLessonAndEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Default: lege invitations-lijst (cancel/replan iteratie veilig).
        _invitationRepo
            .Setup(r => r.GetByLessonAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LessonInvitation>());
    }

    private static CreateStandaloneLessonRequest BuildCreateRequest(
        List<string>? emails = null, int duration = 60, int level = (int)LessonLevel.Beginner)
        => new()
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd"),
            StartTime = "10:00",
            DurationMinutes = duration,
            CourtName = "Baan 1",
            Level = level,
            TrainerId = TrainerId,
            MaxParticipants = 4,
            Notes = "Test les",
            ParticipantEmails = emails ?? new List<string> { "alice@test.com", "bob@test.com" }
        };

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task CreateAsync_HappyPath_PersistsLessonAndInvitations()
    {
        CreateStandaloneLessonRequest req = BuildCreateRequest();

        Result<Guid> result = await _service.CreateAsync(OrgId, req, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _lessonRepo.Verify(r => r.AddAsync(
            It.Is<Lesson>(l =>
                l.OrganizationId == OrgId &&
                l.LessonSerieId == null &&
                l.TrainerId == TrainerId &&
                l.MaxStudents == 4 &&
                l.CourtName == "Baan 1" &&
                l.Level == LessonLevel.Beginner),
            It.IsAny<CancellationToken>()), Times.Once);

        _invitationRepo.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<LessonInvitation>>(invs =>
                invs.Count() == 2 &&
                invs.All(i => i.OrganizationId == OrgId
                              && i.Status == LessonInvitationStatus.Pending
                              && i.TokenHash.Length == 64)),
            It.IsAny<CancellationToken>()), Times.Once);

        _lessonRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Best-effort emails — moeten zijn aangeroepen voor beide deelnemers.
        _emailService.Verify(e => e.SendStandaloneLessonInvitationAsync(
            It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task CreateAsync_ComputesEndTimeFromDuration()
    {
        CreateStandaloneLessonRequest req = BuildCreateRequest(duration: 90);

        Lesson? captured = null;
        _lessonRepo
            .Setup(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .Callback<Lesson, CancellationToken>((l, _) => captured = l)
            .Returns(Task.CompletedTask);

        Result<Guid> result = await _service.CreateAsync(OrgId, req, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.StartTime.Should().Be(new TimeOnly(10, 0));
        captured.EndTime.Should().Be(new TimeOnly(11, 30));
    }

    [Test]
    public async Task CreateAsync_DedupesEmails_CaseInsensitive()
    {
        CreateStandaloneLessonRequest req = BuildCreateRequest(
            emails: new List<string> { "Alice@Test.com", "alice@test.com", "BOB@TEST.COM" });

        IEnumerable<LessonInvitation>? captured = null;
        _invitationRepo
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<LessonInvitation>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<LessonInvitation>, CancellationToken>((invs, _) => captured = invs.ToList())
            .Returns(Task.CompletedTask);

        Result<Guid> result = await _service.CreateAsync(OrgId, req, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Should().HaveCount(2);
        captured.Select(i => i.Email).Should().BeEquivalentTo(new[] { "alice@test.com", "bob@test.com" });
    }

    [Test]
    public async Task CreateAsync_RejectsTrainerNotInOrganization()
    {
        _userLookup
            .Setup(u => u.IsActiveTrainerAsync(TrainerId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Result<Guid> result = await _service.CreateAsync(OrgId, BuildCreateRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_RejectsTrainerConflict()
    {
        _lessonRepo
            .Setup(r => r.FindTrainerConflictAsync(
                TrainerId, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Lesson { Id = Guid.NewGuid() });

        Result<Guid> result = await _service.CreateAsync(OrgId, BuildCreateRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_RejectsPastDate()
    {
        CreateStandaloneLessonRequest req = BuildCreateRequest() with
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd")
        };

        Result<Guid> result = await _service.CreateAsync(OrgId, req, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoLessons()
    {
        _lessonRepo
            .Setup(r => r.GetStandaloneByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lesson>());

        Result<List<StandaloneLessonListItemDto>> result =
            await _service.GetAllAsync(OrgId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllAsync_ComputesAcceptedCount()
    {
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            TrainerId = TrainerId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            CourtName = "Baan 1",
            MaxStudents = 4,
        };
        List<LessonInvitation> invitations =
        [
            new() { Id = Guid.NewGuid(), Status = LessonInvitationStatus.Accepted, Email = "a@t.com" },
            new() { Id = Guid.NewGuid(), Status = LessonInvitationStatus.Pending, Email = "b@t.com" },
            new() { Id = Guid.NewGuid(), Status = LessonInvitationStatus.Declined, Email = "c@t.com" },
        ];

        _lessonRepo
            .Setup(r => r.GetStandaloneByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lesson> { lesson });
        _invitationRepo
            .Setup(r => r.GetByLessonAsync(lesson.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitations);

        Result<List<StandaloneLessonListItemDto>> result =
            await _service.GetAllAsync(OrgId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].InvitedCount.Should().Be(3);
        result.Value[0].AcceptedCount.Should().Be(1);
        result.Value[0].TrainerName.Should().Be("Sara Trainer");
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_ReturnsNotFound_WhenLessonHasSerieId()
    {
        Guid lessonId = Guid.NewGuid();
        Lesson lessonWithSeries = new()
        {
            Id = lessonId,
            OrganizationId = OrgId,
            LessonSerieId = Guid.NewGuid(), // Niet standalone!
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
        };
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(lessonId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessonWithSeries);

        Result<StandaloneLessonDetailDto> result =
            await _service.GetByIdAsync(OrgId, lessonId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task CancelAsync_SetsIsCancelled()
    {
        Guid lessonId = Guid.NewGuid();
        Lesson lesson = new()
        {
            Id = lessonId,
            OrganizationId = OrgId,
            LessonSerieId = null,
            IsCancelled = false,
        };
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(lessonId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        Result result = await _service.CancelAsync(OrgId, lessonId, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        lesson.IsCancelled.Should().BeTrue();
        _lessonRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CancelAsync_ReturnsOk_WhenAlreadyCancelled()
    {
        Guid lessonId = Guid.NewGuid();
        Lesson lesson = new()
        {
            Id = lessonId,
            OrganizationId = OrgId,
            LessonSerieId = null,
            IsCancelled = true,
        };
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(lessonId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        Result result = await _service.CancelAsync(OrgId, lessonId, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Geen extra SaveChanges nodig wanneer al geannuleerd.
        _lessonRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CancelAsync_WithReason_StoresReasonAndMailsActiveInvitees()
    {
        Guid lessonId = Guid.NewGuid();
        Lesson lesson = new()
        {
            Id = lessonId,
            OrganizationId = OrgId,
            LessonSerieId = null,
            IsCancelled = false,
            Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
            StartTime = new TimeOnly(10, 0),
            TrainerId = TrainerId,
        };
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(lessonId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        List<LessonInvitation> invitees =
        [
            new() { Email = "p@x.be", FirstName = "Piet", Status = LessonInvitationStatus.Pending },
            new() { Email = "a@x.be", FirstName = "Ann",  Status = LessonInvitationStatus.Accepted },
            new() { Email = "d@x.be", FirstName = "Dirk", Status = LessonInvitationStatus.Declined },
        ];
        _invitationRepo
            .Setup(r => r.GetByLessonAsync(lessonId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitees);

        Result result = await _service.CancelAsync(OrgId, lessonId, "Trainer ziek", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        lesson.IsCancelled.Should().BeTrue();
        lesson.CancellationReason.Should().Be("Trainer ziek");

        _emailService.Verify(e => e.SendLessonCancellationAsync(
                "p@x.be", "Piet", "Losse les",
                lesson.Date, lesson.StartTime, "Trainer ziek",
                It.IsAny<CancellationToken>()),
            Times.Once);
        _emailService.Verify(e => e.SendLessonCancellationAsync(
                "a@x.be", "Ann", "Losse les",
                lesson.Date, lesson.StartTime, "Trainer ziek",
                It.IsAny<CancellationToken>()),
            Times.Once);
        _emailService.Verify(e => e.SendLessonCancellationAsync(
                "d@x.be", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── AddInvitationsAsync ───────────────────────────────────────────────────

    [Test]
    public async Task AddInvitationsAsync_SkipsExistingEmails()
    {
        Guid lessonId = Guid.NewGuid();
        Lesson lesson = new()
        {
            Id = lessonId,
            OrganizationId = OrgId,
            LessonSerieId = null,
            TrainerId = TrainerId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            CourtName = "Baan 1",
        };
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(lessonId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _invitationRepo
            .Setup(r => r.ExistsByLessonAndEmailAsync(lessonId, "alice@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Alice bestaat al
        _invitationRepo
            .Setup(r => r.ExistsByLessonAndEmailAsync(lessonId, "carol@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Result result = await _service.AddInvitationsAsync(
            OrgId, lessonId,
            new List<string> { "alice@test.com", "carol@test.com" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _invitationRepo.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<LessonInvitation>>(invs =>
                invs.Count() == 1 && invs.Single().Email == "carol@test.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
