using CoachOS.Application.Planning;
using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class PlanningServiceTests
{
    private Mock<ILessonSerieRepository> _seriesRepo = null!;
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<IEnrollmentGroupRepository> _groupRepo = null!;
    private Mock<ITimeSlotPreferenceRepository> _prefRepo = null!;
    private Mock<IScheduleAssignmentRepository> _assignmentRepo = null!;
    private Mock<ILessonRepository> _lessonRepo = null!;
    private Mock<ILogger<PlanningService>> _logger = null!;
    private PlanningService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _seriesRepo = new Mock<ILessonSerieRepository>();
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _groupRepo = new Mock<IEnrollmentGroupRepository>();
        _prefRepo = new Mock<ITimeSlotPreferenceRepository>();
        _assignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _lessonRepo = new Mock<ILessonRepository>();
        _logger = new Mock<ILogger<PlanningService>>();

        _service = new PlanningService(
            _seriesRepo.Object,
            _enrollmentRepo.Object,
            _groupRepo.Object,
            _prefRepo.Object,
            _assignmentRepo.Object,
            _lessonRepo.Object,
            _logger.Object);
    }

    // ── GenerateProposalAsync ────────────────────────────────────────────────

    [Test]
    public async Task GenerateProposalAsync_SeriesNotFound_ReturnsNotFound()
    {
        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        var result = await _service.GenerateProposalAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("not_found");
    }

    [Test]
    public async Task GenerateProposalAsync_NoSlots_ReturnsValidationError()
    {
        var series = BuildSeries(withSlots: false);
        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        var result = await _service.GenerateProposalAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("validation");
    }

    [Test]
    public async Task GenerateProposalAsync_WithEnrollments_CreatesAssignments()
    {
        var series = BuildSeries(withSlots: true);
        var enrollments = new List<Enrollment>
        {
            BuildEnrollment("Alice"),
            BuildEnrollment("Bob"),
        };

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);
        _groupRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentGroup>());
        _prefRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlotPreference>());
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment>());

        var result = await _service.GenerateProposalAsync(SeriesId, OrgId);

        // Algorithm ran — assignments were persisted
        _assignmentRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleAssignment>>(), It.IsAny<CancellationToken>()), Times.Once);
        _seriesRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    // ── ConfirmScheduleAsync ─────────────────────────────────────────────────

    [Test]
    public async Task ConfirmScheduleAsync_SeriesNotFound_ReturnsNotFound()
    {
        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        var result = await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("not_found");
    }

    [Test]
    public async Task ConfirmScheduleAsync_WrongStatus_ReturnsValidationError()
    {
        var series = BuildSeries(withSlots: true);
        series.PlanningStatus = PlanningStatus.Enrollment; // not Planning

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        var result = await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("validation");
    }

    [Test]
    public async Task ConfirmScheduleAsync_NoProposedAssignments_ReturnsValidationError()
    {
        var series = BuildSeries(withSlots: true);
        series.PlanningStatus = PlanningStatus.Planning;

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment>());

        var result = await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("validation");
    }

    [Test]
    public async Task ConfirmScheduleAsync_ValidState_GeneratesLessonsAndConfirms()
    {
        var series = BuildSeries(withSlots: true);
        series.PlanningStatus = PlanningStatus.Planning;
        // Series spans 4 weeks on slot's DayOfWeek
        series.StartDate = new DateOnly(2026, 5, 4); // Monday
        series.EndDate = new DateOnly(2026, 5, 25);   // 4 Mondays

        var slot = series.WeeklyTemplate.First();
        var assignment = new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = slot.Id,
            EnrollmentId = Guid.NewGuid(),
            Status = ScheduleAssignmentStatus.Proposed,
            WeeklyTemplateEntry = slot,
        };

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { assignment });

        var result = await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeTrue();
        series.PlanningStatus.Should().Be(PlanningStatus.Scheduled);
        assignment.Status.Should().Be(ScheduleAssignmentStatus.Confirmed);

        // Slot is Monday (DayOfWeek=1), series spans 4 Mondays → 4 lessons
        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    // ── UpdateAssignmentAsync ────────────────────────────────────────────────

    [Test]
    public async Task UpdateAssignmentAsync_NotFound_ReturnsNotFound()
    {
        var assignmentId = Guid.NewGuid();
        _assignmentRepo.Setup(r => r.GetByIdAsync(assignmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduleAssignment?)null);

        var result = await _service.UpdateAssignmentAsync(
            SeriesId, assignmentId, new UpdateAssignmentRequest { WeeklyTemplateEntryId = SlotId }, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("not_found");
    }

    [Test]
    public async Task UpdateAssignmentAsync_ConfirmedAssignment_ReturnsValidationError()
    {
        var assignment = new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            Status = ScheduleAssignmentStatus.Confirmed,
        };

        _assignmentRepo.Setup(r => r.GetByIdAsync(assignment.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var result = await _service.UpdateAssignmentAsync(
            SeriesId, assignment.Id, new UpdateAssignmentRequest { WeeklyTemplateEntryId = Guid.NewGuid() }, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("validation");
    }

    [Test]
    public async Task UpdateAssignmentAsync_ValidProposed_UpdatesSlot()
    {
        var newSlotId = Guid.NewGuid();
        var assignment = new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            Status = ScheduleAssignmentStatus.Proposed,
        };

        _assignmentRepo.Setup(r => r.GetByIdAsync(assignment.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var result = await _service.UpdateAssignmentAsync(
            SeriesId, assignment.Id, new UpdateAssignmentRequest { WeeklyTemplateEntryId = newSlotId }, OrgId);

        result.IsSuccess.Should().BeTrue();
        assignment.WeeklyTemplateEntryId.Should().Be(newSlotId);
    }

    // ── CreateGroupAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task CreateGroupAsync_EnrollmentAlreadyInGroup_ReturnsValidationError()
    {
        _seriesRepo.Setup(r => r.ExistsAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var enrollment = BuildEnrollment("Alice");
        enrollment.EnrollmentGroupId = Guid.NewGuid(); // already in a group

        _enrollmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { enrollment });

        var result = await _service.CreateGroupAsync(
            SeriesId, new CreateGroupRequest { EnrollmentIds = [enrollment.Id] }, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("validation");
    }

    // ── DissolveGroupAsync ───────────────────────────────────────────────────

    [Test]
    public async Task DissolveGroupAsync_GroupNotFound_ReturnsNotFound()
    {
        var groupId = Guid.NewGuid();
        _groupRepo.Setup(r => r.GetByIdAsync(groupId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentGroup?)null);

        var result = await _service.DissolveGroupAsync(SeriesId, groupId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("not_found");
    }

    [Test]
    public async Task DissolveGroupAsync_ValidGroup_RemovesGroupAndAssignments()
    {
        var group = new EnrollmentGroup
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            Name = "Groep A",
            LeaderEnrollmentId = Guid.NewGuid(),
            Members = new List<Enrollment>
            {
                new() { Id = Guid.NewGuid(), EnrollmentGroupId = Guid.NewGuid() },
                new() { Id = Guid.NewGuid(), EnrollmentGroupId = Guid.NewGuid() },
            },
        };

        _groupRepo.Setup(r => r.GetByIdAsync(group.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment>());

        var result = await _service.DissolveGroupAsync(SeriesId, group.Id, OrgId);

        result.IsSuccess.Should().BeTrue();
        group.Members.Should().AllSatisfy(m => m.EnrollmentGroupId.Should().BeNull());
        _groupRepo.Verify(r => r.Delete(group), Times.Once);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static LessonSerie BuildSeries(bool withSlots)
    {
        var series = new LessonSerie
        {
            Id = SeriesId,
            OrganizationId = OrgId,
            Name = "Test Series",
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2026, 7, 31),
            PlanningStatus = PlanningStatus.Enrollment,
            WeeklyTemplate = new List<WeeklyTemplateEntry>(),
        };

        if (withSlots)
        {
            series.WeeklyTemplate.Add(new WeeklyTemplateEntry
            {
                Id = SlotId,
                LessonSerieId = SeriesId,
                DayOfWeek = 1, // Monday
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                CourtName = "Baan 1",
                MaxStudents = 4,
            });
        }

        return series;
    }

    private static Enrollment BuildEnrollment(string name)
    {
        return new Enrollment
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            StudentName = name,
            StudentEmail = $"{name.ToLower()}@test.com",
            Status = EnrollmentStatus.Confirmed,
            EnrolledAt = DateTime.UtcNow,
        };
    }
}
