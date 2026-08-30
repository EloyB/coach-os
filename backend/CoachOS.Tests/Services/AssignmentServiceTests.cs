using CoachOS.Application.Planning;
using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class AssignmentServiceTests
{
    private Mock<ILessonSerieRepository> _seriesRepo = null!;
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<IEnrollmentGroupRepository> _groupRepo = null!;
    private Mock<IScheduleAssignmentRepository> _assignmentRepo = null!;
    private AssignmentService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _seriesRepo = new Mock<ILessonSerieRepository>();
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _groupRepo = new Mock<IEnrollmentGroupRepository>();
        _assignmentRepo = new Mock<IScheduleAssignmentRepository>();

        _service = new AssignmentService(
            _seriesRepo.Object,
            _enrollmentRepo.Object,
            _groupRepo.Object,
            _assignmentRepo.Object);
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

        var enrollment = PlanningServiceTests.BuildEnrollment("Alice", OrgId, SeriesId);
        enrollment.EnrollmentGroupId = Guid.NewGuid();

        _enrollmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { enrollment });

        var result = await _service.CreateGroupAsync(
            SeriesId, new CreateGroupRequest { EnrollmentIds = [enrollment.Id] }, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("validation");
    }

    [Test]
    public async Task CreateAssignmentAsync_IgnoresCancelledMembersInExistingGroupCapacity()
    {
        var slot = new WeeklyTemplateEntry
        {
            Id = SlotId,
            LessonSerieId = SeriesId,
            MaxStudents = 4,
        };
        var series = new LessonSerie
        {
            Id = SeriesId,
            OrganizationId = OrgId,
            WeeklyTemplate = [slot],
        };
        var cancelledGroup = new EnrollmentGroup
        {
            Id = Guid.NewGuid(),
            LessonSerieId = SeriesId,
            Members =
            [
                new() { Status = EnrollmentStatus.Cancelled },
                new() { Status = EnrollmentStatus.Cancelled },
                new() { Status = EnrollmentStatus.Cancelled },
            ],
        };
        var activeExistingEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            LessonSerieId = SeriesId,
            Status = EnrollmentStatus.Pending,
            StudentName = "Sven Van Eester",
        };
        var activeExistingAssignment = new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentId = activeExistingEnrollment.Id,
            Enrollment = activeExistingEnrollment,
            Status = ScheduleAssignmentStatus.Proposed,
        };
        var newEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            LessonSerieId = SeriesId,
            Status = EnrollmentStatus.Pending,
            StudentName = "Wim Schippers",
        };

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(newEnrollment.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newEnrollment);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ScheduleAssignment
                {
                    Id = Guid.NewGuid(),
                    LessonSerieId = SeriesId,
                    WeeklyTemplateEntryId = SlotId,
                    EnrollmentGroupId = cancelledGroup.Id,
                    EnrollmentGroup = cancelledGroup,
                    Status = ScheduleAssignmentStatus.Proposed,
                },
                activeExistingAssignment,
            ]);

        var result = await _service.CreateAssignmentAsync(
            SeriesId,
            new CreateAssignmentRequest { EnrollmentId = newEnrollment.Id, WeeklyTemplateEntryId = SlotId },
            OrgId);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CreateAssignmentAsync_UsesActiveMemberCountForNewGroupCapacity()
    {
        var series = PlanningServiceTests.BuildSeries(withSlots: true, slotId: SlotId);
        var group = new EnrollmentGroup
        {
            Id = Guid.NewGuid(),
            LessonSerieId = SeriesId,
            Members =
            [
                new() { Status = EnrollmentStatus.Confirmed },
                new() { Status = EnrollmentStatus.Cancelled },
                new() { Status = EnrollmentStatus.Cancelled },
            ],
        };
        var existing = Enumerable.Range(0, 3).Select(_ => new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            Enrollment = new Enrollment { Status = EnrollmentStatus.Confirmed },
            Status = ScheduleAssignmentStatus.Proposed,
        }).ToList();

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _groupRepo.Setup(r => r.GetByIdAsync(group.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.CreateAssignmentAsync(
            SeriesId,
            new CreateAssignmentRequest { GroupId = group.Id, WeeklyTemplateEntryId = SlotId },
            OrgId);

        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
    }

    [Test]
    public async Task UpdateAssignmentAsync_UsesActiveMemberCountForMovedGroup()
    {
        var newSlotId = Guid.NewGuid();
        var series = PlanningServiceTests.BuildSeries(withSlots: true, slotId: SlotId);
        series.WeeklyTemplate.Add(new WeeklyTemplateEntry { Id = newSlotId, MaxStudents = 4 });
        var assignment = new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentGroup = new EnrollmentGroup
            {
                Members =
                [
                    new() { Status = EnrollmentStatus.Confirmed },
                    new() { Status = EnrollmentStatus.Cancelled },
                    new() { Status = EnrollmentStatus.Cancelled },
                ],
            },
            Status = ScheduleAssignmentStatus.Proposed,
        };
        var existing = Enumerable.Range(0, 3).Select(_ => new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = newSlotId,
            Enrollment = new Enrollment { Status = EnrollmentStatus.Confirmed },
            Status = ScheduleAssignmentStatus.Proposed,
        }).Append(assignment).ToList();

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _assignmentRepo.Setup(r => r.GetByIdAsync(assignment.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.UpdateAssignmentAsync(
            SeriesId, assignment.Id,
            new UpdateAssignmentRequest { WeeklyTemplateEntryId = newSlotId }, OrgId);

        result.IsSuccess.Should().BeTrue();
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
}
