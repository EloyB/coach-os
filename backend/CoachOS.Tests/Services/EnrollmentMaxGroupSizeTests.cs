using AwesomeAssertions;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Enrollments;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Het maximum aantal personen per tijdslot (#387) bij het bewerken van een inschrijving, het toevoegen van een
/// lid, de handmatige inschrijving en de publieke reeks.
/// </summary>
[TestFixture]
public class EnrollmentMaxGroupSizeTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotA = Guid.NewGuid();
    private static readonly Guid SlotB = Guid.NewGuid();

    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<ILessonSerieRepository> _seriesRepo = null!;
    private Mock<IEnrollmentGroupRepository> _groupRepo = null!;
    private Mock<IScheduleAssignmentRepository> _assignmentRepo = null!;
    private Mock<IEnrollmentFormRepository> _formRepo = null!;
    private Mock<IOrganizationSettingsRepository> _settingsRepo = null!;
    private Mock<IEmailOutboxRepository> _outboxRepo = null!;
    private EnrollmentService _service = null!;
    private LessonSerie _series = null!;
    private List<ScheduleAssignment> _assignments = null!;

    [SetUp]
    public void SetUp()
    {
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _seriesRepo = new Mock<ILessonSerieRepository>();
        _groupRepo = new Mock<IEnrollmentGroupRepository>();
        _assignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _formRepo = new Mock<IEnrollmentFormRepository>();
        _settingsRepo = new Mock<IOrganizationSettingsRepository>();
        _outboxRepo = new Mock<IEmailOutboxRepository>();
        _assignments = [];

        _series = new LessonSerie
        {
            Id = SeriesId, OrganizationId = OrgId, Name = "Reeks", MinAge = 3, MaxAge = 99,
            StartDate = new DateOnly(2026, 9, 1), MaxRegistrations = 100,
            WeeklyTemplate =
            [
                new WeeklyTemplateEntry { Id = SlotA, LessonSerieId = SeriesId, MaxStudents = 4 },
                new WeeklyTemplateEntry { Id = SlotB, LessonSerieId = SeriesId, MaxStudents = 4 },
            ],
        };
        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(() => _series);
        _seriesRepo.Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync(() => _series);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(() => _assignments);
        _assignmentRepo.Setup(r => r.GetAwaitingConfirmationParticipantIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _formRepo.Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync((EnrollmentForm?)null);
        _settingsRepo.Setup(r => r.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>())).ReturnsAsync((OrganizationSettings?)null);
        _enrollmentRepo.Setup(r => r.IsDuplicateParticipantExceptAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _enrollmentRepo.Setup(r => r.IsDuplicateParticipantAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _enrollmentRepo.Setup(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _outboxRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EmailOutboxMessage>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _service = new EnrollmentService(
            _enrollmentRepo.Object, _formRepo.Object, _seriesRepo.Object, _groupRepo.Object,
            new Mock<IPaymentRepository>().Object, new Mock<ITimeSlotPreferenceRepository>().Object, _settingsRepo.Object,
            _outboxRepo.Object, new Mock<ILessonSeriePriceRepository>().Object,
            _assignmentRepo.Object, new Mock<IUnitOfWork>().Object, new ApplicationMapper(),
            NullLogger<EnrollmentService>.Instance, TimeProvider.System);
    }

    private static Enrollment Person(string name, int max, EnrollmentStatus status = EnrollmentStatus.Pending, Guid? groupId = null) => new()
    {
        Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId, StudentName = name,
        ContactEmail = $"{name.ToLowerInvariant()}@test.be", Status = status, MaxGroupSize = max, EnrollmentGroupId = groupId,
    };

    private static UpdateBasicEnrollmentRequest Edit(Enrollment e, int? max) => new()
    {
        StudentName = e.StudentName, ContactEmail = e.ContactEmail, DateOfBirth = "2000-01-01", MaxGroupSize = max,
    };

    private void ArrangeEnrollment(Enrollment enrollment)
        => _enrollmentRepo.Setup(r => r.GetByIdAsync(enrollment.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);

    private (Enrollment Leader, Enrollment Mate) ArrangeGroup(int max, int members = 2)
    {
        Guid groupId = Guid.NewGuid();
        Enrollment leader = Person("Lead", max, groupId: groupId);
        List<Enrollment> all = [leader];
        for (int i = 1; i < members; i++) all.Add(Person($"Mate{i}", max, groupId: groupId));
        EnrollmentGroup group = new()
        {
            Id = groupId, OrganizationId = OrgId, LessonSerieId = SeriesId, Name = "Groep A", LeaderEnrollmentId = leader.Id, Members = all,
        };
        foreach (Enrollment e in all) e.EnrollmentGroup = group;
        ArrangeEnrollment(leader);
        _enrollmentRepo.Setup(r => r.GetByIdWithGroupAsync(leader.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(leader);
        _groupRepo.Setup(r => r.GetByIdAsync(groupId, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        return (leader, all[1]);
    }

    private void PlaceInSlot(Enrollment enrollment, Guid slotId, ScheduleAssignmentStatus status = ScheduleAssignmentStatus.Proposed)
        => _assignments.Add(new ScheduleAssignment
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId, WeeklyTemplateEntryId = slotId,
            EnrollmentId = enrollment.Id, Enrollment = enrollment, Status = status,
        });

    // ── Bewerken ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Edit_Solo_ChangesTheMaximum()
    {
        Enrollment anna = Person("Anna", 1);
        ArrangeEnrollment(anna);

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(SeriesId, anna.Id, OrgId, Edit(anna, 3));

        result.IsSuccess.Should().BeTrue();
        anna.MaxGroupSize.Should().Be(3);
        result.Value!.MaxGroupSize.Should().Be(3);
    }

    [Test]
    public async Task Edit_WithoutMaximum_LeavesItAlone()
    {
        Enrollment anna = Person("Anna", 3);
        ArrangeEnrollment(anna);

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(SeriesId, anna.Id, OrgId, Edit(anna, null));

        result.IsSuccess.Should().BeTrue();
        anna.MaxGroupSize.Should().Be(3);
    }

    [TestCase(0)]
    [TestCase(5)]
    public async Task Edit_OutsideOneToFour_IsRejected(int max)
    {
        Enrollment anna = Person("Anna", 1);
        ArrangeEnrollment(anna);

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(SeriesId, anna.Id, OrgId, Edit(anna, max));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        anna.MaxGroupSize.Should().Be(1);
    }

    [Test]
    public async Task Edit_AboveWhatTheSeriesAllows_IsRejected()
    {
        foreach (WeeklyTemplateEntry slot in _series.WeeklyTemplate) slot.MaxStudents = 2;
        Enrollment anna = Person("Anna", 1);
        ArrangeEnrollment(anna);

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(SeriesId, anna.Id, OrgId, Edit(anna, 3));

        result.Errors[0].Message.Should().Contain("hoger dan wat deze reeks toelaat (2)");
        anna.MaxGroupSize.Should().Be(1);
    }

    [Test]
    public async Task Edit_Group_CannotGoBelowTheGroupSize()
    {
        (Enrollment leader, _) = ArrangeGroup(max: 3, members: 3);

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(SeriesId, leader.Id, OrgId, Edit(leader, 2));

        result.Errors[0].Message.Should().Contain("eigen groepsgrootte (3)");
        leader.MaxGroupSize.Should().Be(3);
    }

    [Test]
    public async Task Edit_Group_AppliesToEveryMember()
    {
        (Enrollment leader, Enrollment mate) = ArrangeGroup(max: 2, members: 2);

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(SeriesId, leader.Id, OrgId, Edit(leader, 4));

        result.IsSuccess.Should().BeTrue();
        leader.MaxGroupSize.Should().Be(4);
        mate.MaxGroupSize.Should().Be(4);
    }

    [Test]
    public async Task Edit_LoweringBelowTheSizeOfThePlannedSlot_IsBlockedUntilRemovedFromThatSlot()
    {
        Enrollment anna = Person("Anna", 4);
        ArrangeEnrollment(anna);
        PlaceInSlot(anna, SlotA);
        PlaceInSlot(Person("Bram", 4), SlotA);
        PlaceInSlot(Person("Carl", 4), SlotA);

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(SeriesId, anna.Id, OrgId, Edit(anna, 2));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        result.Errors[0].Message.Should().Contain("3 personen").And.Contain("eerst uit dat tijdslot");
        anna.MaxGroupSize.Should().Be(4);

        // Tot de grootte van het slot mag wel.
        (await _service.UpdateBasicEnrollmentAsync(SeriesId, anna.Id, OrgId, Edit(anna, 3))).IsSuccess.Should().BeTrue();
        anna.MaxGroupSize.Should().Be(3);
    }

    [Test]
    public async Task Edit_DeclinedAssignmentDoesNotBlockLowering()
    {
        Enrollment anna = Person("Anna", 4);
        ArrangeEnrollment(anna);
        PlaceInSlot(anna, SlotA, ScheduleAssignmentStatus.Declined);
        PlaceInSlot(Person("Bram", 4), SlotA);
        PlaceInSlot(Person("Carl", 4), SlotA);

        (await _service.UpdateBasicEnrollmentAsync(SeriesId, anna.Id, OrgId, Edit(anna, 1))).IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Edit_GroupAssignment_AlsoBlocksLowering()
    {
        (Enrollment leader, _) = ArrangeGroup(max: 4, members: 2);
        _assignments.Add(new ScheduleAssignment
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId, WeeklyTemplateEntryId = SlotA,
            EnrollmentGroupId = leader.EnrollmentGroupId, EnrollmentGroup = leader.EnrollmentGroup, Status = ScheduleAssignmentStatus.Proposed,
        });
        PlaceInSlot(Person("Bram", 4), SlotA);

        Result<LessonSerieEnrollmentDto> result = await _service.UpdateBasicEnrollmentAsync(SeriesId, leader.Id, OrgId, Edit(leader, 2));

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Message.Should().Contain("3 personen");
    }

    // ── Lid toevoegen aan een groep ──────────────────────────────────────────

    private static CreateManualEnrollmentRequest MemberRequest(string name) => new()
    {
        StudentName = name, ContactEmail = $"{name.ToLowerInvariant()}@test.be", DateOfBirth = "2010-01-01",
    };

    [Test]
    public async Task AddMember_InheritsTheGroupsMaximum()
    {
        (Enrollment leader, _) = ArrangeGroup(max: 4, members: 2);
        Enrollment? added = null;
        _enrollmentRepo.Setup(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
            .Callback<Enrollment, CancellationToken>((e, _) => added = e).Returns(Task.CompletedTask);

        Result<Guid> result = await _service.AddGroupMemberAsync(SeriesId, leader.EnrollmentGroupId!.Value, MemberRequest("Nieuw"), OrgId);

        result.IsSuccess.Should().BeTrue();
        added!.MaxGroupSize.Should().Be(4);
        leader.MaxGroupSize.Should().Be(4);
    }

    [Test]
    public async Task AddMember_BeyondTheGroupsMaximum_RaisesItForEveryone()
    {
        (Enrollment leader, Enrollment mate) = ArrangeGroup(max: 2, members: 2);
        Enrollment? added = null;
        _enrollmentRepo.Setup(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
            .Callback<Enrollment, CancellationToken>((e, _) => added = e).Returns(Task.CompletedTask);

        Result<Guid> result = await _service.AddGroupMemberAsync(SeriesId, leader.EnrollmentGroupId!.Value, MemberRequest("Nieuw"), OrgId);

        result.IsSuccess.Should().BeTrue();
        added!.MaxGroupSize.Should().Be(3);
        leader.MaxGroupSize.Should().Be(3);
        mate.MaxGroupSize.Should().Be(3);
    }

    // ── Handmatige inschrijving ──────────────────────────────────────────────

    [Test]
    public async Task ManualEnrollment_DefaultsToSolo()
    {
        Enrollment? added = null;
        _enrollmentRepo.Setup(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
            .Callback<Enrollment, CancellationToken>((e, _) => added = e).Returns(Task.CompletedTask);

        Result<Guid> result = await _service.CreateManualEnrollmentAsync(SeriesId, MemberRequest("Solo"), OrgId);

        result.IsSuccess.Should().BeTrue();
        added!.MaxGroupSize.Should().Be(1);
    }

    [Test]
    public async Task ManualEnrollment_AdminCanPickAHigherMaximum()
    {
        Enrollment? added = null;
        _enrollmentRepo.Setup(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
            .Callback<Enrollment, CancellationToken>((e, _) => added = e).Returns(Task.CompletedTask);

        Result<Guid> result = await _service.CreateManualEnrollmentAsync(
            SeriesId, MemberRequest("Duo") with { MaxGroupSize = 2 }, OrgId);

        result.IsSuccess.Should().BeTrue();
        added!.MaxGroupSize.Should().Be(2);
    }

    [TestCase(0)]
    [TestCase(5)]
    public async Task ManualEnrollment_OutsideOneToFour_IsRejected(int max)
    {
        Result<Guid> result = await _service.CreateManualEnrollmentAsync(
            SeriesId, MemberRequest("Fout") with { MaxGroupSize = max }, OrgId);

        result.IsSuccess.Should().BeFalse();
        _enrollmentRepo.Verify(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ManualEnrollment_AboveSeriesMaximum_IsRejected()
    {
        foreach (WeeklyTemplateEntry slot in _series.WeeklyTemplate) slot.MaxStudents = 1;

        Result<Guid> result = await _service.CreateManualEnrollmentAsync(
            SeriesId, MemberRequest("Veel") with { MaxGroupSize = 2 }, OrgId);

        result.Errors[0].Message.Should().Contain("hoger dan wat deze reeks toelaat (1)");
    }

    // ── Publieke reeks ───────────────────────────────────────────────────────

    [TestCase(new[] { 2, 3 }, 3)]
    [TestCase(new[] { 1 }, 1)]
    [TestCase(new[] { 10 }, 4)]
    [TestCase(new int[0], 4)]
    [TestCase(new[] { 0 }, 4)]
    public async Task PublicSeries_ExposesTheHighestMaximumAParticipantCanChoose(int[] slotCapacities, int expected)
    {
        _series.WeeklyTemplate = slotCapacities
            .Select(c => new WeeklyTemplateEntry { Id = Guid.NewGuid(), LessonSerieId = SeriesId, MaxStudents = c })
            .ToList();

        Result<PublicLessonSerieDto> result = await _service.GetPublicLessonSerieAsync(SeriesId);

        result.Value!.MaxGroupSize.Should().Be(expected);
    }
}
