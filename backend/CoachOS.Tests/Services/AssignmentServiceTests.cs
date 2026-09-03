using CoachOS.Application.Planning;
using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
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

    // ── RemoveMemberFromGroupAsync ───────────────────────────────────────────

    private (EnrollmentGroup Group, Enrollment Leader, List<Enrollment> Members) BuildGroup(
        int size, EnrollmentStatus status = EnrollmentStatus.Pending)
    {
        Guid groupId = Guid.NewGuid();
        List<Enrollment> members = [];
        for (int i = 0; i < size; i++)
        {
            members.Add(new Enrollment
            {
                Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId,
                StudentName = $"Lid {i}", EnrolledAt = new DateTime(2026, 1, 1).AddDays(i),
                Status = status, EnrollmentGroupId = groupId,
            });
        }
        EnrollmentGroup group = new()
        {
            Id = groupId, OrganizationId = OrgId, LessonSerieId = SeriesId,
            Name = "Groep A", LeaderEnrollmentId = members[0].Id, Members = members,
        };
        _groupRepo.Setup(r => r.GetByIdAsync(groupId, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment>());
        return (group, members[0], members);
    }

    [Test]
    public async Task RemoveMember_GroupOf3_RegularMember_DetachesOnly()
    {
        var (group, leader, members) = BuildGroup(3);
        Enrollment target = members[2]; // geen leider

        Result<bool> result = await _service.RemoveMemberFromGroupAsync(
            SeriesId, group.Id, target.Id, OrgId, ct: CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.EnrollmentGroupId.Should().BeNull();
        target.Status.Should().Be(EnrollmentStatus.Pending);       // niet geannuleerd: blijft actieve solo
        group.LeaderEnrollmentId.Should().Be(leader.Id);           // leider onveranderd
        _groupRepo.Verify(r => r.Delete(It.IsAny<EnrollmentGroup>()), Times.Never); // niet ontbonden
        _assignmentRepo.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<ScheduleAssignment>>()), Times.Never);
        _groupRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RemoveMember_GroupOf3_RegularMember_WithCancel_DetachesAndCancels()
    {
        var (group, leader, members) = BuildGroup(3);
        Enrollment target = members[2];

        Result<bool> result = await _service.RemoveMemberFromGroupAsync(
            SeriesId, group.Id, target.Id, OrgId, cancelEnrollment: true, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.EnrollmentGroupId.Should().BeNull();
        target.Status.Should().Be(EnrollmentStatus.Cancelled);     // ook geannuleerd
        group.LeaderEnrollmentId.Should().Be(leader.Id);
        _groupRepo.Verify(r => r.Delete(It.IsAny<EnrollmentGroup>()), Times.Never);
        _groupRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RemoveMember_GroupOf3_Leader_PromotesEarliestEnrolled()
    {
        var (group, leader, members) = BuildGroup(3);
        // members[1] is vroeger ingeschreven dan members[2] (EnrolledAt oplopend) -> die wordt leider

        Result<bool> result = await _service.RemoveMemberFromGroupAsync(
            SeriesId, group.Id, leader.Id, OrgId, ct: CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        leader.EnrollmentGroupId.Should().BeNull();
        group.LeaderEnrollmentId.Should().Be(members[1].Id);
        _groupRepo.Verify(r => r.Delete(It.IsAny<EnrollmentGroup>()), Times.Never);
    }

    [Test]
    public async Task RemoveMember_GroupOf2_Dissolves_AndConvertsAssignmentToRemainingMember()
    {
        var (group, leader, members) = BuildGroup(2);
        Enrollment remaining = members[1];
        // groep is ingepland: één groeps-toewijzing (Proposed)
        ScheduleAssignment groupAssignment = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = Guid.NewGuid(), EnrollmentGroupId = group.Id,
            Status = ScheduleAssignmentStatus.Proposed, IsLocked = false,
        };
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { groupAssignment });

        List<ScheduleAssignment>? added = null;
        _assignmentRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleAssignment>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ScheduleAssignment>, CancellationToken>((a, _) => added = a.ToList())
            .Returns(Task.CompletedTask);

        Result<bool> result = await _service.RemoveMemberFromGroupAsync(
            SeriesId, group.Id, leader.Id, OrgId, ct: CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        remaining.EnrollmentGroupId.Should().BeNull();                       // laatste lid wordt solo
        _groupRepo.Verify(r => r.Delete(group), Times.Once);                 // groep ontbonden
        _assignmentRepo.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<ScheduleAssignment>>()), Times.Once);
        added.Should().NotBeNull();
        added!.Should().ContainSingle();
        added![0].EnrollmentId.Should().Be(remaining.Id);                    // plek behouden als individueel
        added![0].EnrollmentGroupId.Should().BeNull();
        added![0].WeeklyTemplateEntryId.Should().Be(groupAssignment.WeeklyTemplateEntryId);
        added![0].Status.Should().Be(ScheduleAssignmentStatus.Proposed);
    }

    [Test]
    public async Task RemoveMember_Confirmed_ReturnsConflict()
    {
        var (group, leader, members) = BuildGroup(3, EnrollmentStatus.Confirmed);

        Result<bool> result = await _service.RemoveMemberFromGroupAsync(
            SeriesId, group.Id, members[2].Id, OrgId, ct: CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        members[2].EnrollmentGroupId.Should().Be(group.Id);                 // niets gemuteerd
        _groupRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RemoveMember_NotAMember_ReturnsNotFound()
    {
        var (group, _, _) = BuildGroup(3);

        Result<bool> result = await _service.RemoveMemberFromGroupAsync(
            SeriesId, group.Id, Guid.NewGuid(), OrgId, ct: CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.NotFound);
    }
}
