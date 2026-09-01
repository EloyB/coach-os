using CoachOS.Application.Enrollments;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class EnrollmentServiceTests
{
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<IEnrollmentFormRepository> _enrollmentFormRepo = null!;
    private Mock<ILessonSerieRepository> _lessonSeriesRepo = null!;
    private Mock<IEnrollmentGroupRepository> _enrollmentGroupRepo = null!;
    private Mock<ITimeSlotPreferenceRepository> _timeSlotPreferenceRepo = null!;
    private Mock<IOrganizationSettingsRepository> _orgSettingsRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IEmailService> _emailService = null!;
    private Mock<IEmailOutboxRepository> _emailOutboxRepository = null!;
    private ApplicationMapper _mapper = null!;
    private Mock<ILogger<EnrollmentService>> _logger = null!;
    private EnrollmentService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _enrollmentFormRepo = new Mock<IEnrollmentFormRepository>();
        _lessonSeriesRepo = new Mock<ILessonSerieRepository>();
        _enrollmentGroupRepo = new Mock<IEnrollmentGroupRepository>();
        _timeSlotPreferenceRepo = new Mock<ITimeSlotPreferenceRepository>();
        _orgSettingsRepo = new Mock<IOrganizationSettingsRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _emailService = new Mock<IEmailService>();
        _emailOutboxRepository = new Mock<IEmailOutboxRepository>();
        _mapper = new ApplicationMapper();
        _logger = new Mock<ILogger<EnrollmentService>>();

        _service = new EnrollmentService(
            _enrollmentRepo.Object,
            _enrollmentFormRepo.Object,
            _lessonSeriesRepo.Object,
            _enrollmentGroupRepo.Object,
            _timeSlotPreferenceRepo.Object,
            _orgSettingsRepo.Object,
            _userLookup.Object,
            _emailOutboxRepository.Object,
            _mapper,
            _logger.Object,
            TimeProvider.System);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LessonSerie BuildActiveSeries(int templateEntries = 0) => new()
    {
        Id = SeriesId,
        OrganizationId = OrgId,
        Name = "Beginners A",
        Level = LessonLevel.Beginner,
        Price = 120m,
        StartDate = DateOnly.FromDateTime(DateTime.Today),
        EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
        RegistrationDeadline = DateTime.UtcNow.AddMonths(1),
        MaxRegistrations = 20,
        IsActive = true,
        Lessons = new List<Lesson>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgId,
                TrainerId = TrainerId,
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
                EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)),
                CourtName = "Baan 1",
            }
        },
        WeeklyTemplate = Enumerable
            .Range(0, templateEntries)
            .Select(i => new WeeklyTemplateEntry
            {
                Id = Guid.NewGuid(),
                LessonSerieId = SeriesId,
                DayOfWeek = i,
                StartTime = new TimeOnly(17, 0),
                EndTime = new TimeOnly(18, 0),
                TrainerId = TrainerId,
                CourtName = $"Baan {i + 1}",
                MaxStudents = 4,
            })
            .ToList(),
    };

    private static EnrollmentForm BuildFormWithFields() => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = OrgId,
        LessonSerieId = SeriesId,
        Fields = new List<FormField>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Label = "Geboortedatum",
                Type = FormFieldType.Text,
                IsRequired = true,
                Order = 0,
            },
        },
    };

    // ── GetPublicLessonSerieAsync ───────────────────────────────────────────

    [Test]
    public async Task GetPublicLessonSerie_ReturnsDto_WhenFound()
    {
        var series = BuildActiveSeries(templateEntries: 2);
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentRepo
            .Setup(r => r.CountActiveBySeriesAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _service.GetPublicLessonSerieAsync(SeriesId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Beginners A");
        result.Value.EnrollmentCount.Should().Be(5);
        result.Value.MaxRegistrations.Should().Be(20);
        result.Value.WeeklyTemplate.Should().HaveCount(2);
        result.Value.WeeklyTemplate[0].DayOfWeek.Should().Be(0);
        result.Value.WeeklyTemplate[0].MaxStudents.Should().Be(4);
    }

    [Test]
    public async Task GetPublicLessonSerie_ReturnsNotFound_WhenMissing()
    {
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        var result = await _service.GetPublicLessonSerieAsync(SeriesId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    // ── GetEnrollmentFormAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetEnrollmentForm_ReturnsNull_WhenNoForm()
    {
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentForm?)null);

        var result = await _service.GetEnrollmentFormAsync(SeriesId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Test]
    public async Task GetEnrollmentForm_ReturnsDto_WhenExists()
    {
        var form = BuildFormWithFields();
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var result = await _service.GetEnrollmentFormAsync(SeriesId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Fields.Should().HaveCount(1);
        result.Value.Fields[0].Label.Should().Be("Geboortedatum");
    }

    [Test]
    public async Task GetPublicTimeSlots_UsesLightweightRepositoryQuery()
    {
        var series = BuildActiveSeries(templateEntries: 2);
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicForTimeSlotsAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        var result = await _service.GetPublicTimeSlotsAsync(SeriesId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _lessonSeriesRepo.Verify(
            r => r.GetByIdPublicForTimeSlotsAsync(SeriesId, It.IsAny<CancellationToken>()),
            Times.Once);
        _lessonSeriesRepo.Verify(
            r => r.GetByIdPublicAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetPublicTimeSlots_ReturnsNotFound_WhenSeriesMissing()
    {
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicForTimeSlotsAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        var result = await _service.GetPublicTimeSlotsAsync(SeriesId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    // ── GetSeriesEnrollmentsAsync ────────────────────────────────────────────

    [Test]
    public async Task GetSeriesEnrollments_ReturnsList()
    {
        List<Enrollment> enrollments = new()
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgId,
                LessonSerieId = SeriesId,
                StudentName = "Piet Janssen",
                StudentEmail = "piet@test.be",
                Status = EnrollmentStatus.Confirmed,
                EnrolledAt = DateTime.UtcNow,
                FormResponses = new List<FormResponse>(),
            },
        };

        _enrollmentRepo
            .Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);
        _enrollmentGroupRepo
            .Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentGroup>());

        var result =
            await _service.GetSeriesEnrollmentsAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].StudentName.Should().Be("Piet Janssen");
        result.Value![0].IsGroupLeader.Should().BeFalse();
        result.Value![0].EnrollmentGroupId.Should().BeNull();
    }

    // ── SaveFormAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task SaveForm_ReturnsNotFound_WhenSeriesMissing()
    {
        _lessonSeriesRepo
            .Setup(r => r.ExistsAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        SaveEnrollmentFormRequest request = new() { Fields = new() };

        var result = await _service.SaveFormAsync(SeriesId, OrgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task SaveForm_CreatesNewForm_WhenNoneExists()
    {
        _lessonSeriesRepo
            .Setup(r => r.ExistsAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdWithFieldsAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentForm?)null);

        SaveEnrollmentFormRequest request = new()
        {
            Fields = new()
            {
                new() { Label = "Naam ouder", Type = (int)FormFieldType.Text, IsRequired = true },
            },
        };

        var result = await _service.SaveFormAsync(SeriesId, OrgId, request);

        result.IsSuccess.Should().BeTrue();
        _enrollmentFormRepo.Verify(
            r => r.AddAsync(It.IsAny<EnrollmentForm>(), It.IsAny<CancellationToken>()), Times.Once);
        _enrollmentFormRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── SubmitEnrollmentAsync ────────────────────────────────────────────────

    [Test]
    public async Task SubmitEnrollment_ReturnsNotFound_WhenSeriesMissing()
    {
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Test",
            StudentEmail = "test@test.be",
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task SubmitEnrollment_ReturnsDuplicate_WhenAlreadyEnrolled()
    {
        var series = BuildActiveSeries();
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentForm?)null);
        _enrollmentRepo
            .Setup(r => r.IsDuplicateParticipantAsync(
                SeriesId, "piet@test.be", It.IsAny<string>(),
                It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Piet",
            StudentEmail = "piet@test.be",
            DateOfBirth = "1990-05-12",
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task SubmitEnrollment_QueuesNotifications_WithoutSendingEmailInline()
    {
        var series = BuildActiveSeries();
        SetupSuccessfulEnrollment(series, "anna@test.be");

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Anna",
            StudentEmail = "anna@test.be",
            DateOfBirth = "1990-05-12",
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeTrue();
        _emailOutboxRepository.Verify(r => r.AddRangeAsync(
                It.Is<IEnumerable<EmailOutboxMessage>>(messages =>
                    messages.Count() == 2
                    && messages.All(m => m.EnrollmentId == result.Value)
                    && messages.Any(m => m.Type == EmailOutboxMessageTypes.EnrollmentConfirmation)
                    && messages.Any(m => m.Type == EmailOutboxMessageTypes.TrainerNotification)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _emailOutboxRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailService.VerifyNoOtherCalls();
    }

    [Test]
    public async Task SubmitEnrollment_Succeeds_WhenValid()
    {
        var series = BuildActiveSeries();
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentForm?)null);
        _enrollmentRepo
            .Setup(r => r.IsDuplicateParticipantAsync(
                SeriesId, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userLookup
            .Setup(u => u.GetUserInfoByIdAsync(TrainerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("Jan Peeters", "jan@coach.be"));

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Anna",
            StudentEmail = "anna@test.be",
            DateOfBirth = "1990-05-12",
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeTrue();
        _enrollmentRepo.Verify(
            r => r.AddAsync(It.Is<Enrollment>(e => e.StudentName == "Anna"), It.IsAny<CancellationToken>()),
            Times.Once);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SubmitEnrollment_ReturnsConflict_WhenMaxRegistrationsReached()
    {
        var series = BuildActiveSeries();
        series.MaxRegistrations = 10;

        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentRepo
            .Setup(r => r.CountActiveBySeriesAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Anna",
            StudentEmail = "anna@test.be",
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task SubmitEnrollment_FailsValidation_WhenRequiredFieldMissing()
    {
        var series = BuildActiveSeries();
        var form = BuildFormWithFields();

        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Anna",
            StudentEmail = "anna@test.be",
            Responses = new(), // Missing required field response
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
    }

    // ── Group enrollment tests ───────────────────────────────────────────────

    [Test]
    public async Task SubmitEnrollment_GroupEnrollment_CreatesGroupAndMembers()
    {
        var series = BuildActiveSeries(templateEntries: 1);
        SetupSuccessfulEnrollment(series, "leader@test.be");

        var slotId = series.WeeklyTemplate.First().Id;

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Leader",
            StudentEmail = "leader@test.be",
            EnrollmentType = "group",
            GroupMembers = new()
            {
                new() { StudentName = "Member A", StudentEmail = "a@test.be" },
                new() { StudentName = "Member B", StudentEmail = "b@test.be" },
            },
            TimeSlotPreferences = new()
            {
                new() { WeeklyTemplateEntryId = slotId, Preference = 2 },
            },
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeTrue();
        // Leader enrollment
        _enrollmentRepo.Verify(
            r => r.AddAsync(It.Is<Enrollment>(e => e.StudentName == "Leader"), It.IsAny<CancellationToken>()),
            Times.Once);
        // Group created
        _enrollmentGroupRepo.Verify(
            r => r.AddAsync(It.IsAny<EnrollmentGroup>(), It.IsAny<CancellationToken>()),
            Times.Once);
        // 2 member enrollments
        _enrollmentRepo.Verify(
            r => r.AddAsync(It.Is<Enrollment>(e => e.StudentName == "Member A" || e.StudentName == "Member B"), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        // Preferences saved
        _timeSlotPreferenceRepo.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<TimeSlotPreference>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SubmitEnrollment_GroupEnrollment_CapacityAccountsForGroupSize()
    {
        var series = BuildActiveSeries();
        series.MaxRegistrations = 5;

        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentRepo
            .Setup(r => r.CountActiveBySeriesAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3); // 3 existing + group of 3 (leader + 2) = 6 > 5

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Leader",
            StudentEmail = "leader@test.be",
            EnrollmentType = "group",
            GroupMembers = new()
            {
                new() { StudentName = "Member A", StudentEmail = "a@test.be" },
                new() { StudentName = "Member B", StudentEmail = "b@test.be" },
            },
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task SubmitEnrollment_GroupEnrollment_ReturnsConflict_WhenMemberAlreadyEnrolled()
    {
        var series = BuildActiveSeries();
        SetupSuccessfulEnrollment(series, "leader@test.be");
        _enrollmentRepo
            .Setup(r => r.IsDuplicateParticipantAsync(
                SeriesId, "a@test.be", It.IsAny<string>(),
                It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Leader",
            StudentEmail = "leader@test.be",
            EnrollmentType = "group",
            GroupMembers = new()
            {
                new() { StudentName = "Member A", StudentEmail = "a@test.be", DateOfBirth = "2012-05-12" },
            },
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
        _enrollmentRepo.Verify(
            r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Bootst een Npgsql PostgresException na: de Application-laag herkent die enkel
    /// aan de SqlState-property, niet aan het type.
    /// </summary>
    private sealed class FakePostgresException(string sqlState) : Exception("db error")
    {
        public string SqlState { get; } = sqlState;
    }

    [Test]
    public async Task SubmitEnrollment_ReturnsConflict_WhenInsertHitsUniqueViolation()
    {
        // Race condition: een parallelle submitter insert hetzelfde adres tussen check en insert.
        var series = BuildActiveSeries();
        SetupSuccessfulEnrollment(series, "anna@test.be");
        _enrollmentRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed", new FakePostgresException("23505")));

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Anna",
            StudentEmail = "anna@test.be",
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task SubmitEnrollment_ReturnsUnexpected_WhenInsertFailsForOtherReason()
    {
        var series = BuildActiveSeries();
        SetupSuccessfulEnrollment(series, "anna@test.be");
        _enrollmentRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection lost"));

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Anna",
            StudentEmail = "anna@test.be",
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Unexpected);
    }

    [Test]
    public async Task SubmitEnrollment_SoloWithPreferences_SavesPreferences()
    {
        var series = BuildActiveSeries(templateEntries: 2);
        SetupSuccessfulEnrollment(series, "alice@test.be");

        var slot1 = series.WeeklyTemplate.First().Id;
        var slot2 = series.WeeklyTemplate.Last().Id;

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Alice",
            StudentEmail = "alice@test.be",
            EnrollmentType = "solo",
            IsOpenToGrouping = true,
            TimeSlotPreferences = new()
            {
                new() { WeeklyTemplateEntryId = slot1, Preference = 2 },
                new() { WeeklyTemplateEntryId = slot2, Preference = 3 },
            },
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeTrue();
        _timeSlotPreferenceRepo.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<TimeSlotPreference>>(prefs => prefs.Count() == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SubmitEnrollment_SoloWithoutPreferences_NoPreferencesSaved()
    {
        var series = BuildActiveSeries();
        SetupSuccessfulEnrollment(series, "bob@test.be");

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Bob",
            StudentEmail = "bob@test.be",
            EnrollmentType = "solo",
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeTrue();
        _timeSlotPreferenceRepo.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<TimeSlotPreference>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Enrollment-mode gating ───────────────────────────────────────────────

    [Test]
    public async Task SubmitEnrollment_RejectsGroup_WhenSeriesIsSoloOnly()
    {
        LessonSerie series = BuildActiveSeries();
        series.AllowSoloEnrollment = true;
        series.AllowGroupEnrollment = false;
        SetupSuccessfulEnrollment(series, "leader@test.be");

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Leader",
            StudentEmail = "leader@test.be",
            DateOfBirth = "1990-05-12",
            EnrollmentType = "group",
            GroupMembers = new()
            {
                new() { StudentName = "Bob", DateOfBirth = "2000-01-01" },
            },
        };

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
        _enrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SubmitEnrollment_RejectsSolo_WhenSeriesIsGroupOnly()
    {
        LessonSerie series = BuildActiveSeries();
        series.AllowSoloEnrollment = false;
        series.AllowGroupEnrollment = true;
        SetupSuccessfulEnrollment(series, "anna@test.be");

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Anna",
            StudentEmail = "anna@test.be",
            DateOfBirth = "1990-05-12",
            EnrollmentType = "solo",
        };

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
        _enrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Age eligibility ───────────────────────────────────────────────────────

    [Test]
    public async Task SubmitEnrollment_ParticipantYoungerThanMinAge_ReturnsConflict()
    {
        LessonSerie series = BuildActiveSeries();
        series.MinAge = 6;
        series.MaxAge = 99;
        series.StartDate = new DateOnly(2026, 1, 1);
        SetupSuccessfulEnrollment(series, "kind@test.be");

        // 3 jaar oud op de startdatum → onder de min van 6.
        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Jong Kind",
            StudentEmail = "kind@test.be",
            DateOfBirth = "2023-01-01",
        };

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
        _enrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SubmitEnrollment_ParticipantExactlyMinAge_Succeeds()
    {
        LessonSerie series = BuildActiveSeries();
        series.MinAge = 3;
        series.MaxAge = 99;
        series.StartDate = new DateOnly(2026, 1, 1);
        SetupSuccessfulEnrollment(series, "kind@test.be");

        // Precies 3 op de startdatum.
        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Net Drie",
            StudentEmail = "kind@test.be",
            DateOfBirth = "2023-01-01",
        };

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task SubmitEnrollment_GroupMemberOutsideRange_RejectsWholeEnrollment()
    {
        LessonSerie series = BuildActiveSeries();
        series.MinAge = 6;
        series.MaxAge = 12;
        series.StartDate = new DateOnly(2026, 1, 1);
        SetupSuccessfulEnrollment(series, "leader@test.be");

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Leader",
            StudentEmail = "leader@test.be",
            DateOfBirth = "2016-01-01", // 10 jaar → ok
            EnrollmentType = "group",
            GroupMembers = new()
            {
                new() { StudentName = "Te Jong", StudentEmail = null, DateOfBirth = "2023-01-01" }, // 3 → buiten
            },
        };

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
        _enrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── GetSeriesEnrollmentsWithPreferencesAsync ─────────────────────────────

    [Test]
    public async Task GetEnrollmentsWithPreferences_SeriesNotFound_ReturnsNotFound()
    {
        _lessonSeriesRepo
            .Setup(r => r.ExistsAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.GetSeriesEnrollmentsWithPreferencesAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task GetEnrollmentsWithPreferences_ReturnsEnrichedDtos()
    {
        _lessonSeriesRepo
            .Setup(r => r.ExistsAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var groupId = Guid.NewGuid();
        var leaderId = Guid.NewGuid();
        var enrollment = new Enrollment
        {
            Id = leaderId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            StudentName = "Leader",
            StudentEmail = "leader@test.be",
            Status = EnrollmentStatus.Confirmed,
            EnrolledAt = DateTime.UtcNow,
            EnrollmentGroupId = groupId,
            IsOpenToGrouping = false,
        };

        var slotId = Guid.NewGuid();
        var preference = new TimeSlotPreference
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            EnrollmentId = leaderId,
            WeeklyTemplateEntryId = slotId,
            Preference = SlotPreference.Preferred,
        };

        var group = new EnrollmentGroup
        {
            Id = groupId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            Name = "Groep A",
            LeaderEnrollmentId = leaderId,
            Members = new List<Enrollment> { enrollment },
        };

        _enrollmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { enrollment });
        _timeSlotPreferenceRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlotPreference> { preference });
        _enrollmentGroupRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentGroup> { group });

        var result = await _service.GetSeriesEnrollmentsWithPreferencesAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var dto = result.Value![0];
        dto.StudentName.Should().Be("Leader");
        dto.GroupName.Should().Be("Groep A");
        dto.IsGroupLeader.Should().BeTrue();
        dto.Preferences.Should().HaveCount(1);
        dto.Preferences[0].WeeklyTemplateEntryId.Should().Be(slotId);
        dto.Preferences[0].Preference.Should().Be(2);
    }

    // ── Setup helpers ────────────────────────────────────────────────────────

    private void SetupSuccessfulEnrollment(LessonSerie series, string email)
    {
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentForm?)null);
        _enrollmentRepo
            .Setup(r => r.IsDuplicateParticipantAsync(
                SeriesId, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _enrollmentGroupRepo
            .Setup(r => r.CountBySeriesAsync(SeriesId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _userLookup
            .Setup(u => u.GetUserInfoByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("Trainer Name", "trainer@test.be"));
    }

    // ── UpdateBasicEnrollmentAsync ────────────────────────────────────────────

    [Test]
    public async Task UpdateBasicEnrollmentAsync_UpdatesBasicFields_WithoutChangingStatusOrGroup()
    {
        Guid enrollmentId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        Enrollment enrollment = new()
        {
            Id = enrollmentId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            EnrollmentGroupId = groupId,
            StudentName = "Jan Jansen",
            ContactEmail = "old@test.be",
            StudentEmail = "jan@test.be",
            StudentPhone = "0470000000",
            DateOfBirth = new DateOnly(2012, 5, 10),
            Category = ParticipantCategory.Youth,
            Status = EnrollmentStatus.PendingPayment,
            IsOpenToGrouping = true,
        };
        _enrollmentRepo
            .Setup(r => r.GetByIdAsync(enrollmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
        _enrollmentRepo
            .Setup(r => r.IsDuplicateParticipantExceptAsync(
                SeriesId, enrollmentId, "parent@example.be", "Piet Jansen",
                new DateOnly(2010, 4, 3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _orgSettingsRepo
            .Setup(r => r.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationSettings { OrganizationId = OrgId, YouthMaxAge = 17 });

        UpdateBasicEnrollmentRequest request = new()
        {
            StudentName = "Piet Jansen",
            ContactEmail = " Parent@Example.BE ",
            StudentEmail = " Piet@Example.BE ",
            StudentPhone = "0499000000",
            DateOfBirth = "2010-04-03",
            IsOpenToGrouping = false,
        };

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(
            SeriesId, enrollmentId, OrgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        enrollment.StudentName.Should().Be("Piet Jansen");
        enrollment.ContactEmail.Should().Be("parent@example.be");
        enrollment.StudentEmail.Should().Be("piet@example.be");
        enrollment.StudentPhone.Should().Be("0499000000");
        enrollment.DateOfBirth.Should().Be(new DateOnly(2010, 4, 3));
        enrollment.Category.Should().Be(ParticipantCategory.Youth);
        enrollment.IsOpenToGrouping.Should().BeFalse();
        enrollment.Status.Should().Be(EnrollmentStatus.PendingPayment);
        enrollment.EnrollmentGroupId.Should().Be(groupId);
        result.Value!.ContactEmail.Should().Be("parent@example.be");
        result.Value.StudentPhone.Should().Be("0499000000");
        result.Value.IsOpenToGrouping.Should().BeFalse();
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateBasicEnrollmentAsync_AllowsSharedContactEmail_WhenParticipantIsDifferent()
    {
        Guid enrollmentId = Guid.NewGuid();
        Enrollment enrollment = new()
        {
            Id = enrollmentId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            StudentName = "Kind Een",
            ContactEmail = "ouder@example.be",
            Status = EnrollmentStatus.Pending,
        };
        _enrollmentRepo
            .Setup(r => r.GetByIdAsync(enrollmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
        _enrollmentRepo
            .Setup(r => r.IsDuplicateParticipantExceptAsync(
                SeriesId, enrollmentId, "ouder@example.be", "Kind Twee",
                new DateOnly(2014, 1, 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _orgSettingsRepo
            .Setup(r => r.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationSettings?)null);

        UpdateBasicEnrollmentRequest request = new()
        {
            StudentName = "Kind Twee",
            ContactEmail = "ouder@example.be",
            StudentEmail = null,
            StudentPhone = null,
            DateOfBirth = "2014-01-02",
            IsOpenToGrouping = true,
        };

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(
            SeriesId, enrollmentId, OrgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        enrollment.ContactEmail.Should().Be("ouder@example.be");
        enrollment.StudentName.Should().Be("Kind Twee");
    }

    [Test]
    public async Task UpdateBasicEnrollmentAsync_DuplicateParticipant_ReturnsConflict()
    {
        Guid enrollmentId = Guid.NewGuid();
        Enrollment enrollment = new()
        {
            Id = enrollmentId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            StudentName = "Kind Een",
            ContactEmail = "ouder@example.be",
            Status = EnrollmentStatus.Pending,
        };
        _enrollmentRepo
            .Setup(r => r.GetByIdAsync(enrollmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
        _enrollmentRepo
            .Setup(r => r.IsDuplicateParticipantExceptAsync(
                SeriesId, enrollmentId, "ouder@example.be", "Kind Een",
                new DateOnly(2014, 1, 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        UpdateBasicEnrollmentRequest request = new()
        {
            StudentName = "Kind Een",
            ContactEmail = "ouder@example.be",
            DateOfBirth = "2014-01-02",
            IsOpenToGrouping = true,
        };

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(
            SeriesId, enrollmentId, OrgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateBasicEnrollmentAsync_WrongSeries_ReturnsNotFound()
    {
        Guid enrollmentId = Guid.NewGuid();
        Enrollment enrollment = new()
        {
            Id = enrollmentId,
            OrganizationId = OrgId,
            LessonSerieId = Guid.NewGuid(),
            StudentName = "Jan Jansen",
            ContactEmail = "jan@test.be",
            Status = EnrollmentStatus.Pending,
        };
        _enrollmentRepo
            .Setup(r => r.GetByIdAsync(enrollmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        UpdateBasicEnrollmentRequest request = new()
        {
            StudentName = "Jan Jansen",
            ContactEmail = "jan@test.be",
            DateOfBirth = "2014-01-02",
            IsOpenToGrouping = true,
        };

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(
            SeriesId, enrollmentId, OrgId, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── CancelEnrollmentAsync ─────────────────────────────────────────────────

    [Test]
    public async Task CancelEnrollmentAsync_ExistingEnrollment_SetsStatusToCancelled()
    {
        Guid enrollmentId = Guid.NewGuid();
        Enrollment enrollment = new()
        {
            Id = enrollmentId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            StudentName = "Jan Jansen",
            StudentEmail = "jan@test.be",
            Status = EnrollmentStatus.Confirmed,
        };
        _enrollmentRepo
            .Setup(r => r.GetByIdAsync(enrollmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        Result<bool> result = await _service.CancelEnrollmentAsync(
            SeriesId, enrollmentId, OrgId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        enrollment.Status.Should().Be(EnrollmentStatus.Cancelled);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CancelEnrollmentAsync_UnknownEnrollment_ReturnsNotFound()
    {
        Guid enrollmentId = Guid.NewGuid();
        _enrollmentRepo
            .Setup(r => r.GetByIdAsync(enrollmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);

        Result<bool> result = await _service.CancelEnrollmentAsync(
            SeriesId, enrollmentId, OrgId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CancelEnrollmentAsync_WrongSeries_ReturnsNotFound()
    {
        // De route bevat de reeks-id; een geldige inschrijving onder een ándere reeks
        // (zelfde organisatie) mag niet annuleerbaar zijn via die verkeerde reeks.
        Guid enrollmentId = Guid.NewGuid();
        Guid otherSeriesId = Guid.NewGuid();
        Enrollment enrollment = new()
        {
            Id = enrollmentId,
            OrganizationId = OrgId,
            LessonSerieId = otherSeriesId,
            StudentName = "Jan Jansen",
            StudentEmail = "jan@test.be",
            Status = EnrollmentStatus.Confirmed,
        };
        _enrollmentRepo
            .Setup(r => r.GetByIdAsync(enrollmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        Result<bool> result = await _service.CancelEnrollmentAsync(
            SeriesId, enrollmentId, OrgId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        enrollment.Status.Should().Be(EnrollmentStatus.Confirmed);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CancelEnrollmentAsync_OtherOrganization_ReturnsNotFound()
    {
        // Multi-tenancy: de repository filtert op organizationId, dus een inschrijving
        // van een andere organisatie levert null op en mag niet annuleerbaar zijn.
        Guid enrollmentId = Guid.NewGuid();
        Guid otherOrgId = Guid.NewGuid();
        _enrollmentRepo
            .Setup(r => r.GetByIdAsync(enrollmentId, otherOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);

        Result<bool> result = await _service.CancelEnrollmentAsync(
            SeriesId, enrollmentId, otherOrgId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task CancelEnrollmentAsync_AlreadyCancelled_ReturnsValidationError()
    {
        Guid enrollmentId = Guid.NewGuid();
        Enrollment enrollment = new()
        {
            Id = enrollmentId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            StudentName = "Jan Jansen",
            StudentEmail = "jan@test.be",
            Status = EnrollmentStatus.Cancelled,
        };
        _enrollmentRepo
            .Setup(r => r.GetByIdAsync(enrollmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        Result<bool> result = await _service.CancelEnrollmentAsync(
            SeriesId, enrollmentId, OrgId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── CancelGroupAsync (atomair) ────────────────────────────────────────────

    [Test]
    public async Task CancelGroupAsync_ActiveMembers_CancelsAllInOneSave()
    {
        Guid groupId = Guid.NewGuid();
        Enrollment m1 = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId, Status = EnrollmentStatus.Confirmed, StudentName = "A", StudentEmail = "a@t.be" };
        Enrollment m2 = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId, Status = EnrollmentStatus.PendingPayment, StudentName = "B", StudentEmail = "b@t.be" };
        EnrollmentGroup group = new() { Id = groupId, OrganizationId = OrgId, LessonSerieId = SeriesId, Members = new List<Enrollment> { m1, m2 } };
        _enrollmentGroupRepo
            .Setup(r => r.GetByIdAsync(groupId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        Result<bool> result = await _service.CancelGroupAsync(SeriesId, groupId, OrgId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        m1.Status.Should().Be(EnrollmentStatus.Cancelled);
        m2.Status.Should().Be(EnrollmentStatus.Cancelled);
        // Eén SaveChanges over alle leden = atomair (alles-of-niets).
        _enrollmentGroupRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CancelGroupAsync_UnknownGroup_ReturnsNotFound()
    {
        Guid groupId = Guid.NewGuid();
        _enrollmentGroupRepo
            .Setup(r => r.GetByIdAsync(groupId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentGroup?)null);

        Result<bool> result = await _service.CancelGroupAsync(SeriesId, groupId, OrgId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        _enrollmentGroupRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CancelGroupAsync_WrongSeries_ReturnsNotFoundAndDoesNotMutate()
    {
        Guid groupId = Guid.NewGuid();
        Enrollment m1 = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = Guid.NewGuid(), Status = EnrollmentStatus.Confirmed, StudentName = "A", StudentEmail = "a@t.be" };
        EnrollmentGroup group = new() { Id = groupId, OrganizationId = OrgId, LessonSerieId = Guid.NewGuid(), Members = new List<Enrollment> { m1 } };
        _enrollmentGroupRepo
            .Setup(r => r.GetByIdAsync(groupId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        Result<bool> result = await _service.CancelGroupAsync(SeriesId, groupId, OrgId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
        m1.Status.Should().Be(EnrollmentStatus.Confirmed);
        _enrollmentGroupRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CancelGroupAsync_AllAlreadyCancelled_ReturnsValidationError()
    {
        Guid groupId = Guid.NewGuid();
        Enrollment m1 = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId, Status = EnrollmentStatus.Cancelled, StudentName = "A", StudentEmail = "a@t.be" };
        EnrollmentGroup group = new() { Id = groupId, OrganizationId = OrgId, LessonSerieId = SeriesId, Members = new List<Enrollment> { m1 } };
        _enrollmentGroupRepo
            .Setup(r => r.GetByIdAsync(groupId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        Result<bool> result = await _service.CancelGroupAsync(SeriesId, groupId, OrgId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
        _enrollmentGroupRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
