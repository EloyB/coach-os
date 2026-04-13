using CoachOS.Application.Configuration;
using CoachOS.Application.Planning;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class ConfirmationOrchestrationServiceTests
{
    private Mock<ILessonSerieRepository> _seriesRepo = null!;
    private Mock<IScheduleAssignmentRepository> _assignmentRepo = null!;
    private Mock<IAssignmentConfirmationTokenRepository> _tokenRepo = null!;
    private Mock<IPaymentRepository> _paymentRepo = null!;
    private Mock<IEmailService> _emailService = null!;
    private Mock<IOptions<AppOptions>> _appOptions = null!;
    private Mock<ILogger<ConfirmationOrchestrationService>> _logger = null!;
    private ConfirmationOrchestrationService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _seriesRepo = new Mock<ILessonSerieRepository>();
        _assignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _tokenRepo = new Mock<IAssignmentConfirmationTokenRepository>();
        _paymentRepo = new Mock<IPaymentRepository>();
        _emailService = new Mock<IEmailService>();
        _appOptions = new Mock<IOptions<AppOptions>>();
        _appOptions.Setup(o => o.Value).Returns(new AppOptions());
        _logger = new Mock<ILogger<ConfirmationOrchestrationService>>();

        _service = new ConfirmationOrchestrationService(
            _seriesRepo.Object,
            _assignmentRepo.Object,
            _tokenRepo.Object,
            _paymentRepo.Object,
            _emailService.Object,
            _appOptions.Object,
            _logger.Object);
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
        var series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.PlanningStatus = PlanningStatus.Enrollment;

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        var result = await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("validation");
    }

    [Test]
    public async Task ConfirmScheduleAsync_NoProposedAssignments_ReturnsValidationError()
    {
        var series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
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
    public async Task ConfirmScheduleAsync_ValidState_ConfirmsAssignmentsAndSetsStatus()
    {
        var series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.PlanningStatus = PlanningStatus.Planning;

        var assignment = new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentId = Guid.NewGuid(),
            Status = ScheduleAssignmentStatus.Proposed,
        };

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { assignment });

        var result = await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeTrue();
        series.PlanningStatus.Should().Be(PlanningStatus.AwaitingConfirmation);
        assignment.Status.Should().Be(ScheduleAssignmentStatus.AwaitingConfirmation);
    }
}
