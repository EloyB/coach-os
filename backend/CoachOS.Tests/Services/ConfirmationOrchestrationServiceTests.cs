using CoachOS.Application.Configuration;
using CoachOS.Application.Planning;
using CoachOS.Application.Pricing;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
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
    private Mock<IPricingService> _pricingService = null!;
    private Mock<IOptions<AppOptions>> _appOptions = null!;
    private Mock<ILogger<ConfirmationOrchestrationService>> _logger = null!;
    private ConfirmationOrchestrationService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotId = Guid.NewGuid();

    /// <summary>Totaal uit de prijsmatrix — bewust géén veelvoud van de legacy serieprijs.</summary>
    private const decimal MatrixTotal = 137.50m;

    private void SetupPrice(decimal total, int groupSize)
    {
        _pricingService
            .Setup(p => p.CalculateForGroupAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Enrollment>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PriceBreakdown>.Ok(new PriceBreakdown
            {
                Total = total,
                GroupSize = groupSize,
                UsedLegacyPrice = false,
            }));
    }

    [SetUp]
    public void SetUp()
    {
        _seriesRepo = new Mock<ILessonSerieRepository>();
        _assignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _tokenRepo = new Mock<IAssignmentConfirmationTokenRepository>();
        _paymentRepo = new Mock<IPaymentRepository>();
        _emailService = new Mock<IEmailService>();
        _pricingService = new Mock<IPricingService>();
        SetupPrice(MatrixTotal, groupSize: 1);
        _appOptions = new Mock<IOptions<AppOptions>>();
        _appOptions.Setup(o => o.Value).Returns(new AppOptions());
        _logger = new Mock<ILogger<ConfirmationOrchestrationService>>();

        _service = new ConfirmationOrchestrationService(
            _seriesRepo.Object,
            _assignmentRepo.Object,
            _tokenRepo.Object,
            _paymentRepo.Object,
            _emailService.Object,
            _pricingService.Object,
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

    // ── Admin cash-bevestiging: bedrag komt uit IPricingService ───────────────

    [Test]
    public async Task AdminConfirmAssignmentAsync_Group_BooksAmountFromPricingService_NotSeriesPriceTimesGroupSize()
    {
        // Arrange: groep van 3. Legacy formule zou 3 × 40 = 120 boeken; de
        // prijsmatrix geeft 137,50.
        const decimal seriesPrice = 40m;
        SetupPrice(MatrixTotal, groupSize: 3);

        LessonSerie series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.PlanningStatus = PlanningStatus.AwaitingConfirmation;
        series.Price = seriesPrice;

        Enrollment leader = PlanningServiceTests.BuildEnrollment("Alice", OrgId, SeriesId);
        Enrollment member1 = PlanningServiceTests.BuildEnrollment("Bob", OrgId, SeriesId);
        Enrollment member2 = PlanningServiceTests.BuildEnrollment("Cara", OrgId, SeriesId);
        EnrollmentGroup group = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            LeaderEnrollmentId = leader.Id,
            Members = [leader, member1, member2],
        };

        ScheduleAssignment assignment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentGroupId = group.Id,
            EnrollmentGroup = group,
            Status = ScheduleAssignmentStatus.AwaitingConfirmation,
        };

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _assignmentRepo.Setup(r => r.GetByIdAsync(assignment.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { assignment });
        _tokenRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssignmentConfirmationToken>());
        _tokenRepo.Setup(r => r.GetBySeriesAsNoTrackingAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssignmentConfirmationToken>());

        Payment? booked = null;
        _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => booked = p);

        // Act
        var result = await _service.AdminConfirmAssignmentAsync(SeriesId, assignment.Id, OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booked.Should().NotBeNull();
        booked!.Amount.Should().Be(MatrixTotal,
            "het admin cash-bedrag moet uit IPricingService komen");
        booked.Amount.Should().NotBe(seriesPrice * 3);
        booked.EnrollmentId.Should().Be(leader.Id, "de leider blijft de betaler");

        // Alle 3 deelnemers (leider inbegrepen) moeten aan de prijsberekening gevoerd zijn.
        _pricingService.Verify(p => p.CalculateForGroupAsync(
                SeriesId,
                It.Is<IReadOnlyList<Enrollment>>(l => l.Count == 3 && l.Any(e => e.Id == leader.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AdminConfirmAssignmentAsync_PricingFails_PropagatesErrorAndBooksNoPayment()
    {
        LessonSerie series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.PlanningStatus = PlanningStatus.AwaitingConfirmation;

        Enrollment enrollment = PlanningServiceTests.BuildEnrollment("Alice", OrgId, SeriesId);
        ScheduleAssignment assignment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentId = enrollment.Id,
            Enrollment = enrollment,
            Status = ScheduleAssignmentStatus.AwaitingConfirmation,
        };

        _pricingService
            .Setup(p => p.CalculateForGroupAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Enrollment>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PriceBreakdown>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden.")));

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _assignmentRepo.Setup(r => r.GetByIdAsync(assignment.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        var result = await _service.AdminConfirmAssignmentAsync(SeriesId, assignment.Id, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Message == "Lessenreeks niet gevonden.");
        _paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        assignment.Status.Should().Be(ScheduleAssignmentStatus.AwaitingConfirmation,
            "zonder geldige prijs mag de toewijzing niet bevestigd worden");
    }
}
