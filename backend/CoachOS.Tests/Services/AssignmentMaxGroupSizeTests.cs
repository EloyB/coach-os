using AwesomeAssertions;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Planning;
using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Handmatig inplannen en het maximum aantal personen per tijdslot (#387): een waarschuwing (HTTP 409) die de admin
/// bewust kan negeren. Capaciteit blijft wel een harde regel.
/// </summary>
[TestFixture]
public class AssignmentMaxGroupSizeTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotId = Guid.NewGuid();
    private static readonly Guid OtherSlotId = Guid.NewGuid();

    private Mock<ILessonSerieRepository> _seriesRepo = null!;
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<IEnrollmentGroupRepository> _groupRepo = null!;
    private Mock<IScheduleAssignmentRepository> _assignmentRepo = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private AssignmentService _service = null!;
    private List<ScheduleAssignment> _existing = null!;

    [SetUp]
    public void SetUp()
    {
        _seriesRepo = new Mock<ILessonSerieRepository>();
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _groupRepo = new Mock<IEnrollmentGroupRepository>();
        _assignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _existing = [];

        LessonSerie series = new()
        {
            Id = SeriesId,
            OrganizationId = OrgId,
            Name = "Testreeks",
            WeeklyTemplate =
            [
                new WeeklyTemplateEntry { Id = SlotId, LessonSerieId = SeriesId, DayOfWeek = 1, MaxStudents = 4 },
                new WeeklyTemplateEntry { Id = OtherSlotId, LessonSerieId = SeriesId, DayOfWeek = 2, MaxStudents = 4 },
            ],
        };
        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(series);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _existing);

        _service = new AssignmentService(
            _seriesRepo.Object, _enrollmentRepo.Object, _groupRepo.Object, _assignmentRepo.Object,
            _unitOfWork.Object, new Mock<IEmailService>().Object, NullLogger<AssignmentService>.Instance);
    }

    private static Enrollment Person(string name, int maxGroupSize, Guid? groupId = null) => new()
    {
        Id = Guid.NewGuid(),
        LessonSerieId = SeriesId,
        OrganizationId = OrgId,
        StudentName = name,
        Status = EnrollmentStatus.Confirmed,
        MaxGroupSize = maxGroupSize,
        EnrollmentGroupId = groupId,
    };

    private ScheduleAssignment PlaceSolo(Enrollment enrollment, Guid slotId, ScheduleAssignmentStatus status = ScheduleAssignmentStatus.Proposed)
    {
        ScheduleAssignment assignment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = slotId,
            EnrollmentId = enrollment.Id,
            Enrollment = enrollment,
            Status = status,
        };
        _existing.Add(assignment);
        return assignment;
    }

    private void Arrange(Enrollment enrollment)
        => _enrollmentRepo.Setup(r => r.GetByIdAsync(enrollment.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);

    private Task<Result<Guid>> Create(Enrollment enrollment, bool acknowledge = false)
        => _service.CreateAssignmentAsync(
            SeriesId,
            new CreateAssignmentRequest
            {
                EnrollmentId = enrollment.Id,
                WeeklyTemplateEntryId = SlotId,
                AcknowledgeExceedsMaxGroupSize = acknowledge,
            },
            OrgId);

    // ── Aanmaken ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_SlotStaysWithinEveryonesMaximum_Succeeds()
    {
        PlaceSolo(Person("Bram", 4), SlotId);
        Enrollment anna = Person("Anna", 2);
        Arrange(anna);

        Result<Guid> result = await Create(anna);

        result.IsSuccess.Should().BeTrue();
        _assignmentRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleAssignment>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Create_NewPersonWouldExceedOwnMaximum_WarnsWithConflictAndPlansNothing()
    {
        PlaceSolo(Person("Bram", 4), SlotId);
        PlaceSolo(Person("Carl", 4), SlotId);
        Enrollment anna = Person("Anna", 2);
        Arrange(anna);

        Result<Guid> result = await Create(anna);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        result.Errors[0].Message.Should().Contain("3 personen").And.Contain("Anna (max. 2)");
        _assignmentRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleAssignment>>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Create_WarningAcknowledged_PlansAnyway()
    {
        PlaceSolo(Person("Bram", 4), SlotId);
        PlaceSolo(Person("Carl", 4), SlotId);
        Enrollment anna = Person("Anna", 2);
        Arrange(anna);

        Result<Guid> result = await Create(anna, acknowledge: true);

        result.IsSuccess.Should().BeTrue();
        _assignmentRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleAssignment>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Create_SomeoneAlreadyInTheSlotWouldBeExceeded_NamesThatPerson()
    {
        PlaceSolo(Person("Dora", 2), SlotId);
        PlaceSolo(Person("Bram", 4), SlotId);
        Enrollment eva = Person("Eva", 4);
        Arrange(eva);

        Result<Guid> result = await Create(eva);

        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        result.Errors[0].Message.Should().Contain("Dora (max. 2)").And.NotContain("Bram").And.NotContain("Eva");
    }

    [Test]
    public async Task Create_DeclinedAssignmentsDoNotCountTowardsTheSlot()
    {
        PlaceSolo(Person("Bram", 4), SlotId, ScheduleAssignmentStatus.Declined);
        PlaceSolo(Person("Carl", 4), SlotId, ScheduleAssignmentStatus.Declined);
        Enrollment anna = Person("Anna", 2);
        Arrange(anna);

        (await Create(anna)).IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Create_GroupWithItsOwnMaximum_IsJudgedAsAWhole()
    {
        Guid groupId = Guid.NewGuid();
        Enrollment lead = Person("Lead", 2, groupId);
        Enrollment mate = Person("Mate", 2, groupId);
        EnrollmentGroup group = new()
        {
            Id = groupId, OrganizationId = OrgId, LessonSerieId = SeriesId, Name = "Groep A",
            LeaderEnrollmentId = lead.Id, Members = [lead, mate],
        };
        _groupRepo.Setup(r => r.GetByIdAsync(groupId, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        PlaceSolo(Person("Bram", 4), SlotId);

        Result<Guid> result = await _service.CreateAssignmentAsync(
            SeriesId, new CreateAssignmentRequest { GroupId = groupId, WeeklyTemplateEntryId = SlotId }, OrgId);

        // Groep van 2 + Bram = 3 personen, maar de groep wil maximum 2.
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        result.Errors[0].Message.Should().Contain("Lead (max. 2)").And.Contain("Mate (max. 2)");
    }

    [Test]
    public async Task Create_OverCapacity_StaysAHardValidationErrorEvenWhenAcknowledged()
    {
        for (int i = 0; i < 4; i++) PlaceSolo(Person($"P{i}", 4), SlotId);
        Enrollment anna = Person("Anna", 4);
        Arrange(anna);

        Result<Guid> result = await Create(anna, acknowledge: true);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        result.Errors[0].Message.Should().Contain("geen plaats meer");
    }

    [Test]
    public async Task Create_PersonWhoseGroupIsLargerThanTheStoredMaximum_IsNotFlaggedForTheirOwnGroup()
    {
        // Onverwachte data: een groep van twee met maximum 1 mag niet tegen zichzelf waarschuwen.
        Guid groupId = Guid.NewGuid();
        Enrollment lead = Person("Lead", 1, groupId);
        Enrollment mate = Person("Mate", 1, groupId);
        _groupRepo.Setup(r => r.GetByIdAsync(groupId, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(new EnrollmentGroup
        {
            Id = groupId, OrganizationId = OrgId, LessonSerieId = SeriesId, Name = "G", LeaderEnrollmentId = lead.Id, Members = [lead, mate],
        });

        Result<Guid> result = await _service.CreateAssignmentAsync(
            SeriesId, new CreateAssignmentRequest { GroupId = groupId, WeeklyTemplateEntryId = SlotId }, OrgId);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Verplaatsen ──────────────────────────────────────────────────────────

    [Test]
    public async Task Update_MovingIntoASlotThatIsTooBigForSomeone_WarnsAndMovesNothing()
    {
        PlaceSolo(Person("Bram", 4), SlotId);
        PlaceSolo(Person("Carl", 4), SlotId);
        Enrollment anna = Person("Anna", 2);
        ScheduleAssignment moving = PlaceSolo(anna, OtherSlotId);
        _assignmentRepo.Setup(r => r.GetByIdAsync(moving.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(moving);

        Result<bool> result = await _service.UpdateAssignmentAsync(
            SeriesId, moving.Id, new UpdateAssignmentRequest { WeeklyTemplateEntryId = SlotId }, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        result.Errors[0].Message.Should().Contain("Anna (max. 2)");
        moving.WeeklyTemplateEntryId.Should().Be(OtherSlotId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Update_WarningAcknowledged_Moves()
    {
        PlaceSolo(Person("Bram", 4), SlotId);
        PlaceSolo(Person("Carl", 4), SlotId);
        Enrollment anna = Person("Anna", 2);
        ScheduleAssignment moving = PlaceSolo(anna, OtherSlotId);
        _assignmentRepo.Setup(r => r.GetByIdAsync(moving.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(moving);

        Result<bool> result = await _service.UpdateAssignmentAsync(
            SeriesId, moving.Id,
            new UpdateAssignmentRequest { WeeklyTemplateEntryId = SlotId, AcknowledgeExceedsMaxGroupSize = true }, OrgId);

        result.IsSuccess.Should().BeTrue();
        moving.WeeklyTemplateEntryId.Should().Be(SlotId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Update_MovingWithinTheLimit_NeedsNoConfirmation()
    {
        PlaceSolo(Person("Bram", 4), SlotId);
        Enrollment anna = Person("Anna", 2);
        ScheduleAssignment moving = PlaceSolo(anna, OtherSlotId);
        _assignmentRepo.Setup(r => r.GetByIdAsync(moving.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(moving);

        Result<bool> result = await _service.UpdateAssignmentAsync(
            SeriesId, moving.Id, new UpdateAssignmentRequest { WeeklyTemplateEntryId = SlotId }, OrgId);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Update_TheMovedAssignmentItselfDoesNotCountInItsOldSlot()
    {
        // Anna staat al in het doelslot (bv. een tweede toewijzing aanpassen): ze telt niet dubbel.
        Enrollment anna = Person("Anna", 2);
        ScheduleAssignment moving = PlaceSolo(anna, SlotId);
        PlaceSolo(Person("Bram", 4), SlotId);
        _assignmentRepo.Setup(r => r.GetByIdAsync(moving.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(moving);

        Result<bool> result = await _service.UpdateAssignmentAsync(
            SeriesId, moving.Id, new UpdateAssignmentRequest { WeeklyTemplateEntryId = SlotId }, OrgId);

        result.IsSuccess.Should().BeTrue();
    }
}
