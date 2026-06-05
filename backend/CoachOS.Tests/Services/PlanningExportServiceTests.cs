using CoachOS.Application.Export;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class PlanningExportServiceTests
{
    private Mock<ILessonSerieRepository> _seriesRepo = null!;
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<IEnrollmentGroupRepository> _groupRepo = null!;
    private Mock<IScheduleAssignmentRepository> _assignmentRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IPlanningWorkbookBuilder> _workbookBuilder = null!;
    private PlanningExportService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();

    // 2026-05-01 is a Friday; the month has 4 Mondays: 4, 11, 18, 25.
    private const int MondaysInMay2026 = 4;

    [SetUp]
    public void SetUp()
    {
        _seriesRepo = new Mock<ILessonSerieRepository>();
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _groupRepo = new Mock<IEnrollmentGroupRepository>();
        _assignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _workbookBuilder = new Mock<IPlanningWorkbookBuilder>();

        _userLookup
            .Setup(u => u.GetUserNamesByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [TrainerId] = "Jan Janssen" });

        _service = new PlanningExportService(
            _seriesRepo.Object,
            _enrollmentRepo.Object,
            _groupRepo.Object,
            _assignmentRepo.Object,
            _userLookup.Object,
            _workbookBuilder.Object,
            TimeProvider.System);
    }

    [Test]
    public async Task ExportSeriePlanningAsync_SeriesNotFound_ReturnsNotFound()
    {
        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        var result = await _service.ExportSeriePlanningAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("not_found");
        _workbookBuilder.Verify(b => b.Build(It.IsAny<PlanningExportModel>()), Times.Never);
    }

    [Test]
    public async Task ExportSeriePlanningAsync_ExpandsWeeklySlotIntoConcreteDatesWithTrainer()
    {
        ArrangeSeries();
        ArrangeEmptyEnrollmentData();
        PlanningExportModel model = await CaptureModelAsync();

        model.LessonMoments.Should().HaveCount(MondaysInMay2026);
        model.LessonMoments.Select(m => m.Date).Should().Equal(
            new DateOnly(2026, 5, 4), new DateOnly(2026, 5, 11),
            new DateOnly(2026, 5, 18), new DateOnly(2026, 5, 25));
        model.LessonMoments.Should().OnlyContain(m =>
            m.DayName == "Maandag" && m.TrainerName == "Jan Janssen" && m.CourtName == "Baan 1");
    }

    [Test]
    public async Task ExportSeriePlanningAsync_GroupAssignment_ExpandsMembersPerDate()
    {
        ArrangeSeries();

        Enrollment tom = BuildEnrollment("Tom");
        Enrollment lisa = BuildEnrollment("Lisa");
        Guid groupId = Guid.NewGuid();
        var group = new EnrollmentGroup
        {
            Id = groupId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            Name = "Groep A",
            LeaderEnrollmentId = tom.Id,
            Members = new List<Enrollment> { tom, lisa },
        };
        var assignment = new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentGroupId = groupId,
            Status = ScheduleAssignmentStatus.Confirmed,
        };

        _enrollmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { tom, lisa });
        _groupRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentGroup> { group });
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { assignment });

        PlanningExportModel model = await CaptureModelAsync();

        // 2 leden × 4 maandagen.
        model.ScheduledLessons.Should().HaveCount(2 * MondaysInMay2026);
        model.ScheduledLessons.Should().OnlyContain(s =>
            s.GroupName == "Groep A" && s.Status == "Bevestigd");
        model.ScheduledLessons.Select(s => s.StudentName).Distinct()
            .Should().BeEquivalentTo(new[] { "Tom", "Lisa" });
    }

    [Test]
    public async Task ExportSeriePlanningAsync_DeclinedAssignment_Excluded()
    {
        ArrangeSeries();

        Enrollment sara = BuildEnrollment("Sara");
        var declined = new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentId = sara.Id,
            Status = ScheduleAssignmentStatus.Declined,
        };

        _enrollmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { sara });
        _groupRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentGroup>());
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { declined });

        PlanningExportModel model = await CaptureModelAsync();

        model.ScheduledLessons.Should().BeEmpty();
    }

    [Test]
    public async Task ExportSeriePlanningAsync_MapsEnrollmentsWithPhoneAndFormResponses()
    {
        ArrangeSeries();

        var field = new FormField { Id = Guid.NewGuid(), Label = "Niveau", Order = 0 };
        Enrollment alice = BuildEnrollment("Alice");
        alice.StudentPhone = "0470123456";
        alice.FormResponses = new List<FormResponse>
        {
            new() { Id = Guid.NewGuid(), EnrollmentId = alice.Id, FormFieldId = field.Id, Value = "Gevorderd", FormField = field },
        };

        _enrollmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { alice });
        _groupRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentGroup>());
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment>());

        PlanningExportModel model = await CaptureModelAsync();

        model.FormFieldLabels.Should().ContainSingle().Which.Should().Be("Niveau");
        EnrollmentRow row = model.Enrollments.Should().ContainSingle().Subject;
        row.StudentName.Should().Be("Alice");
        row.StudentPhone.Should().Be("0470123456");
        row.Status.Should().Be("Bevestigd");
        row.FormResponses.Should().ContainKey("Niveau").WhoseValue.Should().Be("Gevorderd");
    }

    [Test]
    public async Task ExportSeriePlanningAsync_Success_ReturnsXlsxFileWithSeriesNameInFileName()
    {
        ArrangeSeries();
        ArrangeEmptyEnrollmentData();
        _workbookBuilder.Setup(b => b.Build(It.IsAny<PlanningExportModel>()))
            .Returns(new byte[] { 1, 2, 3 });

        var result = await _service.ExportSeriePlanningAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Equal(1, 2, 3);
        result.Value.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Value.FileName.Should().StartWith("Voorjaarsreeks-planning-").And.EndWith(".xlsx");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<PlanningExportModel> CaptureModelAsync()
    {
        PlanningExportModel? captured = null;
        _workbookBuilder.Setup(b => b.Build(It.IsAny<PlanningExportModel>()))
            .Callback<PlanningExportModel>(m => captured = m)
            .Returns(new byte[] { 0 });

        var result = await _service.ExportSeriePlanningAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        return captured!;
    }

    private void ArrangeSeries()
    {
        var series = new LessonSerie
        {
            Id = SeriesId,
            OrganizationId = OrgId,
            Name = "Voorjaarsreeks",
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2026, 5, 31),
            WeeklyTemplate = new List<WeeklyTemplateEntry>
            {
                new()
                {
                    Id = SlotId,
                    LessonSerieId = SeriesId,
                    DayOfWeek = 1, // Maandag
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 0),
                    CourtName = "Baan 1",
                    TrainerId = TrainerId,
                    MaxStudents = 4,
                },
            },
        };

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
    }

    private void ArrangeEmptyEnrollmentData()
    {
        _enrollmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());
        _groupRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EnrollmentGroup>());
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment>());
    }

    private static Enrollment BuildEnrollment(string name) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = OrgId,
        LessonSerieId = SeriesId,
        StudentName = name,
        StudentEmail = $"{name.ToLower()}@test.com",
        Status = EnrollmentStatus.Confirmed,
        EnrolledAt = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
    };
}
