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
