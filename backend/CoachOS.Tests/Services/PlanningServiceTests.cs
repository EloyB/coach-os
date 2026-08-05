using CoachOS.Application.Planning;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using FluentAssertions;
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
    private Mock<IUserLookupService> _userLookup = null!;
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
        _userLookup = new Mock<IUserLookupService>();

        _service = new PlanningService(
            _seriesRepo.Object,
            _enrollmentRepo.Object,
            _groupRepo.Object,
            _prefRepo.Object,
            _assignmentRepo.Object,
            _userLookup.Object);
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

    // ── Guard tegen onbedoelde regenerate (Bug 1) ────────────────────────────

    [Test]
    public async Task GenerateProposalAsync_AwaitingConfirmation_NoForce_DoesNotRegenerate()
    {
        var series = BuildSeries(withSlots: true);
        series.PlanningStatus = PlanningStatus.AwaitingConfirmation;

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        // GetPlanningOverviewAsync wordt aangeroepen — minimal stubs
        _enrollmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());
        _groupRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentGroup>());
        _prefRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlotPreference>());
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment>());

        var result = await _service.GenerateProposalAsync(SeriesId, OrgId, force: false);

        result.IsSuccess.Should().BeTrue();
        _assignmentRepo.Verify(r => r.RemoveProposedBySeriesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _assignmentRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleAssignment>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GenerateProposalAsync_Scheduled_NoForce_DoesNotRegenerate()
    {
        var series = BuildSeries(withSlots: true);
        series.PlanningStatus = PlanningStatus.Scheduled;

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());
        _groupRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentGroup>());
        _prefRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlotPreference>());
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment>());

        var result = await _service.GenerateProposalAsync(SeriesId, OrgId, force: false);

        result.IsSuccess.Should().BeTrue();
        _assignmentRepo.Verify(r => r.RemoveProposedBySeriesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _assignmentRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleAssignment>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GenerateProposalAsync_Force_ConfirmedAssignmentTreatedAsLock_NotDuplicated()
    {
        // Series in AwaitingConfirmation; Anna heeft een Confirmed ScheduleAssignment
        // op slot 1; Bob heeft alleen een enrollment, geen assignment. force=true.
        // Verwacht: Anna's enrollment wordt gelockt (algoritme krijgt enkel Bob).
        var series = BuildSeries(withSlots: true);
        series.PlanningStatus = PlanningStatus.AwaitingConfirmation;

        var anna = BuildEnrollment("Anna");
        var bob = BuildEnrollment("Bob");
        var enrollments = new List<Enrollment> { anna, bob };

        var confirmedForAnna = new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentId = anna.Id,
            Status = ScheduleAssignmentStatus.Confirmed,
            IsLocked = false,
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
            .ReturnsAsync(new List<ScheduleAssignment> { confirmedForAnna });

        IEnumerable<ScheduleAssignment>? captured = null;
        _assignmentRepo
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleAssignment>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ScheduleAssignment>, CancellationToken>((a, _) => captured = a.ToList())
            .Returns(Task.CompletedTask);

        var result = await _service.GenerateProposalAsync(SeriesId, OrgId, force: true);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        // Geen nieuwe Proposed voor Anna (zou een duplicate worden t.o.v. haar Confirmed)
        captured!.Should().NotContain(a => a.EnrollmentId == anna.Id);
    }

    // ── Assignment locking ───────────────────────────────────────────────────

    [Test]
    public async Task SetAssignmentLockAsync_ProposedAssignment_LocksAndReturnsLockedDto()
    {
        var assignment = new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentId = Guid.NewGuid(),
            Status = ScheduleAssignmentStatus.Proposed,
            IsLocked = false,
        };

        _assignmentRepo.Setup(r => r.GetByIdAsync(assignment.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var result = await _service.SetAssignmentLockAsync(SeriesId, assignment.Id, OrgId, isLocked: true);

        result.IsSuccess.Should().BeTrue();
        assignment.IsLocked.Should().BeTrue();
        result.Value!.IsLocked.Should().BeTrue();
        result.Value.Status.Should().Be("Proposed");
        _assignmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetAssignmentLockAsync_NonProposedAssignment_ReturnsValidationError()
    {
        var assignment = new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentId = Guid.NewGuid(),
            Status = ScheduleAssignmentStatus.AwaitingConfirmation,
            IsLocked = false,
        };

        _assignmentRepo.Setup(r => r.GetByIdAsync(assignment.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var result = await _service.SetAssignmentLockAsync(SeriesId, assignment.Id, OrgId, isLocked: true);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("validation");
        assignment.IsLocked.Should().BeFalse();
        _assignmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    internal static LessonSerie BuildSeries(bool withSlots, Guid? seriesId = null, Guid? orgId = null, Guid? slotId = null)
    {
        var sid = seriesId ?? SeriesId;
        var oid = orgId ?? OrgId;
        var series = new LessonSerie
        {
            Id = sid,
            OrganizationId = oid,
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
                Id = slotId ?? SlotId,
                LessonSerieId = sid,
                DayOfWeek = 1, // Monday
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                CourtName = "Baan 1",
                MaxStudents = 4,
            });
        }

        return series;
    }

    internal static Enrollment BuildEnrollment(string name, Guid? orgId = null, Guid? seriesId = null)
    {
        return new Enrollment
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId ?? OrgId,
            LessonSerieId = seriesId ?? SeriesId,
            StudentName = name,
            StudentEmail = $"{name.ToLower()}@test.com",
            Status = EnrollmentStatus.Confirmed,
            EnrolledAt = DateTime.UtcNow,
        };
    }
}
