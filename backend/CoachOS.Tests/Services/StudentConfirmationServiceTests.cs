using System.Security.Cryptography;
using System.Text;
using CoachOS.Application.Pricing;
using CoachOS.Application.StudentConfirmation;
using CoachOS.Application.StudentConfirmation.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Regression tests voor de staleness-bug in <see cref="StudentConfirmationService.TryFinalizeSeriesAsync"/>.
/// <para>
/// Context: <see cref="IAssignmentConfirmationTokenRepository.TryClaimResponseAsync"/> muteert de DB via
/// <c>ExecuteUpdateAsync</c>, wat de EF change tracker bypasst. Een opvolgende tracking-read via
/// <c>GetBySeriesAsync</c> zou via identity resolution een stale in-memory token teruggeven
/// (Response=Pending), waardoor de anyPending-guard de serie nooit zou finaliseren op het student-pad.
/// </para>
/// </summary>
[TestFixture]
public class StudentConfirmationServiceTests
{
    private Mock<IAssignmentConfirmationTokenRepository> _tokenRepo = null!;
    private Mock<IScheduleAssignmentRepository> _assignmentRepo = null!;
    private Mock<ILessonSerieRepository> _seriesRepo = null!;
    private Mock<IPaymentRepository> _paymentRepo = null!;
    private Mock<CoachOS.Application.Payments.IPaymentService> _paymentService = null!;
    private Mock<IPricingService> _pricingService = null!;
    private Mock<ILogger<StudentConfirmationService>> _logger = null!;
    private StudentConfirmationService _sut = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotId = Guid.NewGuid();

    /// <summary>Totaal uit de prijsmatrix — bewust géén veelvoud van SeriesPrice.</summary>
    private const decimal MatrixTotal = 137.50m;

    /// <summary>Legacy prijs per persoon op LessonSerie; mag nooit meer gebruikt worden.</summary>
    private const decimal SeriesPrice = 40m;

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
        _tokenRepo = new Mock<IAssignmentConfirmationTokenRepository>();
        _assignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _seriesRepo = new Mock<ILessonSerieRepository>();
        _paymentRepo = new Mock<IPaymentRepository>();
        _paymentService = new Mock<CoachOS.Application.Payments.IPaymentService>();
        _pricingService = new Mock<IPricingService>();
        _logger = new Mock<ILogger<StudentConfirmationService>>();

        // Default: prijsmatrix levert een vast totaal dat losstaat van
        // LessonSerie.Price, zodat tests kunnen bewijzen dat het bedrag uit
        // IPricingService komt en niet uit de oude Price * n formule.
        SetupPrice(MatrixTotal, groupSize: 1);

        _sut = new StudentConfirmationService(
            _tokenRepo.Object,
            _assignmentRepo.Object,
            _seriesRepo.Object,
            _paymentRepo.Object,
            _paymentService.Object,
            _pricingService.Object,
            _logger.Object);
    }

    [Test]
    public async Task ConfirmAsync_LastPendingConfirmation_FlipsSeriesToScheduled_UsingNoTrackingRead()
    {
        // Arrange: soloenrollment, 1 token, student is de laatste bevestiger.
        const string rawToken = "demo-raw-token";
        string hash = HashToken(rawToken);

        LessonSerie series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.PlanningStatus = PlanningStatus.AwaitingConfirmation;
        series.Price = 80m;

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

        AssignmentConfirmationToken token = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            ScheduleAssignmentId = assignment.Id,
            EnrollmentId = enrollment.Id,
            TokenHash = hash,
            Response = ConfirmationResponse.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(72),
            ScheduleAssignment = assignment,
            Enrollment = enrollment,
        };

        _tokenRepo.Setup(r => r.GetByTokenHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        _tokenRepo.Setup(r => r.TryClaimResponseAsync(
                token.Id, ConfirmationResponse.Confirmed, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Bewust: tracking-variant levert STALE data (Pending). Als de service deze gebruikt,
        // zou anyPending true zijn en zou de serie in AwaitingConfirmation blijven → test faalt.
        _tokenRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssignmentConfirmationToken>
            {
                StaleCopyOf(token), // Response nog Pending (stale)
            });

        // No-tracking variant levert FRESH data (Confirmed) — zoals DB werkelijk is na ExecuteUpdate.
        _tokenRepo.Setup(r => r.GetBySeriesAsNoTrackingAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssignmentConfirmationToken>
            {
                FreshCopyOf(token, ConfirmationResponse.Confirmed),
            });

        assignment.Status = ScheduleAssignmentStatus.Confirmed;
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { assignment });

        // Act
        var result = await _sut.ConfirmAsync(
            rawToken,
            new ConfirmRequest { PaymentMethod = (int)PaymentMethod.Cash },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        series.PlanningStatus.Should().Be(PlanningStatus.Scheduled,
            "de serie moet finaliseren wanneer de no-tracking read aantoont dat alles bevestigd is");

        _tokenRepo.Verify(
            r => r.GetBySeriesAsNoTrackingAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "TryFinalizeSeriesAsync MOET de no-tracking variant gebruiken om de stale-read bug te vermijden");
    }

    [Test]
    public async Task ConfirmAsync_ParticipantWithDeclinedWithoutReplacement_DoesNotFinalize()
    {
        // Arrange: 2 deelnemers. Eén confirmed, één declined zonder vervanging.
        // Reeks mag NIET naar Scheduled flippen ondanks dat er geen pending tokens meer zijn.
        const string rawToken = "demo-raw-token-2";
        string hash = HashToken(rawToken);

        LessonSerie series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.PlanningStatus = PlanningStatus.AwaitingConfirmation;
        series.Price = 80m;

        Enrollment alice = PlanningServiceTests.BuildEnrollment("Alice", OrgId, SeriesId);
        Enrollment bob = PlanningServiceTests.BuildEnrollment("Bob", OrgId, SeriesId);

        ScheduleAssignment aliceAssignment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentId = alice.Id,
            Enrollment = alice,
            Status = ScheduleAssignmentStatus.AwaitingConfirmation,
        };
        ScheduleAssignment bobAssignment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentId = bob.Id,
            Enrollment = bob,
            Status = ScheduleAssignmentStatus.Declined, // Bob heeft eerder gedeclined zonder pick-alternative
        };

        AssignmentConfirmationToken aliceToken = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            ScheduleAssignmentId = aliceAssignment.Id,
            EnrollmentId = alice.Id,
            TokenHash = hash,
            Response = ConfirmationResponse.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(72),
            ScheduleAssignment = aliceAssignment,
            Enrollment = alice,
        };
        AssignmentConfirmationToken bobToken = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            ScheduleAssignmentId = bobAssignment.Id,
            EnrollmentId = bob.Id,
            TokenHash = "bob-hash",
            Response = ConfirmationResponse.Declined,
            ExpiresAt = DateTime.UtcNow.AddHours(72),
            ScheduleAssignment = bobAssignment,
            Enrollment = bob,
        };

        _tokenRepo.Setup(r => r.GetByTokenHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aliceToken);

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        _tokenRepo.Setup(r => r.TryClaimResponseAsync(
                aliceToken.Id, ConfirmationResponse.Confirmed, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        aliceAssignment.Status = ScheduleAssignmentStatus.Confirmed;

        _tokenRepo.Setup(r => r.GetBySeriesAsNoTrackingAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssignmentConfirmationToken>
            {
                FreshCopyOf(aliceToken, ConfirmationResponse.Confirmed),
                FreshCopyOf(bobToken, ConfirmationResponse.Declined),
            });

        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { aliceAssignment, bobAssignment });

        // Act
        var result = await _sut.ConfirmAsync(
            rawToken,
            new ConfirmRequest { PaymentMethod = (int)PaymentMethod.Cash },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        series.PlanningStatus.Should().Be(PlanningStatus.AwaitingConfirmation,
            "Bob heeft een Declined zonder Confirmed vervanging — de reeks is niet rond.");
    }

    [Test]
    public async Task ConfirmAsync_ExpiredNonResponder_DoesNotBlockFinalize()
    {
        // Regression guard voor docs/student-confirmation-cash-mvp.md:163:
        // "Expired tokens count as handled — so one non-responder doesn't block the series forever."
        //
        // Scenario: Alice bevestigt als laatste actieve deelnemer. Charlie is een niet-
        // responder: zijn token is verlopen (Pending + ExpiresAt in het verleden) en zijn
        // ScheduleAssignment staat nog op AwaitingConfirmation (er is geen sweeper).
        // De reeks MOET alsnog naar Scheduled flippen.
        const string rawToken = "demo-raw-token-3";
        string hash = HashToken(rawToken);

        LessonSerie series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.PlanningStatus = PlanningStatus.AwaitingConfirmation;
        series.Price = 80m;

        Enrollment alice = PlanningServiceTests.BuildEnrollment("Alice", OrgId, SeriesId);
        Enrollment charlie = PlanningServiceTests.BuildEnrollment("Charlie", OrgId, SeriesId);

        ScheduleAssignment aliceAssignment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentId = alice.Id,
            Enrollment = alice,
            Status = ScheduleAssignmentStatus.AwaitingConfirmation,
        };
        ScheduleAssignment charlieAssignment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentId = charlie.Id,
            Enrollment = charlie,
            Status = ScheduleAssignmentStatus.AwaitingConfirmation, // non-responder blijft awaiting
        };

        AssignmentConfirmationToken aliceToken = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            ScheduleAssignmentId = aliceAssignment.Id,
            EnrollmentId = alice.Id,
            TokenHash = hash,
            Response = ConfirmationResponse.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(72),
            ScheduleAssignment = aliceAssignment,
            Enrollment = alice,
        };
        AssignmentConfirmationToken charlieTokenExpired = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            ScheduleAssignmentId = charlieAssignment.Id,
            EnrollmentId = charlie.Id,
            TokenHash = "charlie-hash",
            Response = ConfirmationResponse.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // verlopen — telt als "handled"
            ScheduleAssignment = charlieAssignment,
            Enrollment = charlie,
        };

        _tokenRepo.Setup(r => r.GetByTokenHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aliceToken);

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        _tokenRepo.Setup(r => r.TryClaimResponseAsync(
                aliceToken.Id, ConfirmationResponse.Confirmed, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        aliceAssignment.Status = ScheduleAssignmentStatus.Confirmed;

        _tokenRepo.Setup(r => r.GetBySeriesAsNoTrackingAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssignmentConfirmationToken>
            {
                FreshCopyOf(aliceToken, ConfirmationResponse.Confirmed),
                charlieTokenExpired, // Pending + expired
            });

        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { aliceAssignment, charlieAssignment });

        // Act
        var result = await _sut.ConfirmAsync(
            rawToken,
            new ConfirmRequest { PaymentMethod = (int)PaymentMethod.Cash },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        series.PlanningStatus.Should().Be(PlanningStatus.Scheduled,
            "expired non-responders tellen als 'handled' per MVP-contract — zij mogen de reeks niet blokkeren");
    }

    // ── Prijsberekening via IPricingService ──────────────────────────────────

    [Test]
    public async Task ConfirmAsync_CashGroup_BooksAmountFromPricingService_NotSeriesPriceTimesGroupSize()
    {
        // Arrange: groep van 3. Legacy formule zou 3 × 40 = 120 boeken; de
        // prijsmatrix geeft 137,50. Alleen dat laatste bedrag is correct.
        const string rawToken = "pricing-confirm-token";
        string hash = HashToken(rawToken);
        SetupPrice(MatrixTotal, groupSize: 3);

        LessonSerie series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.PlanningStatus = PlanningStatus.AwaitingConfirmation;
        series.Price = SeriesPrice;

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

        AssignmentConfirmationToken token = BuildToken(hash, assignment, leader);

        _tokenRepo.Setup(r => r.GetByTokenHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _tokenRepo.Setup(r => r.TryClaimResponseAsync(
                token.Id, ConfirmationResponse.Confirmed, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tokenRepo.Setup(r => r.GetBySeriesAsNoTrackingAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssignmentConfirmationToken>());

        Payment? booked = null;
        _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => booked = p);

        // Act
        var result = await _sut.ConfirmAsync(
            rawToken,
            new ConfirmRequest { PaymentMethod = (int)PaymentMethod.Cash },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booked.Should().NotBeNull();
        booked!.Amount.Should().Be(MatrixTotal,
            "het bedrag moet uit IPricingService komen, niet uit series.Price * groepsgrootte");
        booked.Amount.Should().NotBe(SeriesPrice * 3);

        // De volledige groep (leider inbegrepen) moet aan de prijsberekening gevoerd zijn.
        _pricingService.Verify(p => p.CalculateForGroupAsync(
                SeriesId,
                It.Is<IReadOnlyList<Enrollment>>(l => l.Count == 3 && l.Any(e => e.Id == leader.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ConfirmAsync_PricingFails_PropagatesErrorAndDoesNotClaimToken()
    {
        // Arrange
        const string rawToken = "pricing-fail-token";
        string hash = HashToken(rawToken);

        _pricingService
            .Setup(p => p.CalculateForGroupAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Enrollment>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PriceBreakdown>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden.")));

        LessonSerie series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
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

        _tokenRepo.Setup(r => r.GetByTokenHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildToken(hash, assignment, enrollment));
        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        // Act
        var result = await _sut.ConfirmAsync(
            rawToken,
            new ConfirmRequest { PaymentMethod = (int)PaymentMethod.Cash },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Message == "Lessenreeks niet gevonden.");

        _tokenRepo.Verify(r => r.TryClaimResponseAsync(
                It.IsAny<Guid>(), It.IsAny<ConfirmationResponse>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "een mislukte prijsberekening mag de bevestiging niet verbruiken");
        _paymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PickAlternativeAsync_CashGroup_BooksAmountFromPricingService()
    {
        // Arrange: groep van 2 die een alternatief tijdslot kiest en cash betaalt.
        const string rawToken = "pricing-alt-token";
        string hash = HashToken(rawToken);
        SetupPrice(MatrixTotal, groupSize: 2);

        LessonSerie series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.PlanningStatus = PlanningStatus.AwaitingConfirmation;
        series.Price = SeriesPrice;
        series.WeeklyTemplate.First().MaxStudents = 10;

        Enrollment leader = PlanningServiceTests.BuildEnrollment("Alice", OrgId, SeriesId);
        Enrollment member = PlanningServiceTests.BuildEnrollment("Bob", OrgId, SeriesId);
        EnrollmentGroup group = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            LeaderEnrollmentId = leader.Id,
            Members = [leader, member],
        };

        ScheduleAssignment oldAssignment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            WeeklyTemplateEntryId = SlotId,
            EnrollmentGroupId = group.Id,
            EnrollmentGroup = group,
            Status = ScheduleAssignmentStatus.Declined,
        };

        AssignmentConfirmationToken token = BuildToken(hash, oldAssignment, leader);
        token.Response = ConfirmationResponse.Declined;

        _tokenRepo.Setup(r => r.GetByTokenHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment>());
        _tokenRepo.Setup(r => r.TryTransitionResponseAsync(
                token.Id, ConfirmationResponse.Declined, ConfirmationResponse.Confirmed,
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tokenRepo.Setup(r => r.GetBySeriesAsNoTrackingAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssignmentConfirmationToken>());

        Payment? booked = null;
        _paymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => booked = p);

        // Act
        var result = await _sut.PickAlternativeAsync(
            rawToken,
            new PickAlternativeRequest
            {
                WeeklyTemplateEntryId = SlotId,
                PaymentMethod = (int)PaymentMethod.Cash,
            },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        booked.Should().NotBeNull();
        booked!.Amount.Should().Be(MatrixTotal);
        booked.Amount.Should().NotBe(SeriesPrice * 2,
            "de legacy formule series.Price * groepsgrootte mag niet meer gebruikt worden");
    }

    [Test]
    public async Task GetByTokenAsync_ReturnsTotalAndPerPersonPriceFromPricingService()
    {
        // Arrange: groep van 3, matrixtotaal 137,50 → 45,83 per persoon (afgerond).
        const string rawToken = "pricing-details-token";
        string hash = HashToken(rawToken);
        SetupPrice(MatrixTotal, groupSize: 3);

        LessonSerie series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.Price = SeriesPrice;

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

        _tokenRepo.Setup(r => r.GetByTokenHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildToken(hash, assignment, leader));
        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        // Act
        var result = await _sut.GetByTokenAsync(rawToken, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalPrice.Should().Be(MatrixTotal);
        result.Value.PricePerPerson.Should().Be(45.83m,
            "per persoon = totaal / groepsgrootte, afgerond op 2 decimalen");
        result.Value.PricePerPerson.Should().NotBe(SeriesPrice,
            "het legacy veld LessonSerie.Price mag niet meer getoond worden");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AssignmentConfirmationToken BuildToken(
        string hash, ScheduleAssignment assignment, Enrollment enrollment)
    {
        return new AssignmentConfirmationToken
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            ScheduleAssignmentId = assignment.Id,
            EnrollmentId = enrollment.Id,
            TokenHash = hash,
            Response = ConfirmationResponse.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(72),
            ScheduleAssignment = assignment,
            Enrollment = enrollment,
        };
    }

    private static string HashToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static AssignmentConfirmationToken StaleCopyOf(AssignmentConfirmationToken source)
    {
        return new AssignmentConfirmationToken
        {
            Id = source.Id,
            OrganizationId = source.OrganizationId,
            ScheduleAssignmentId = source.ScheduleAssignmentId,
            EnrollmentId = source.EnrollmentId,
            TokenHash = source.TokenHash,
            Response = ConfirmationResponse.Pending, // stale in-memory waarde
            ExpiresAt = source.ExpiresAt,
            ScheduleAssignment = source.ScheduleAssignment,
            Enrollment = source.Enrollment,
        };
    }

    private static AssignmentConfirmationToken FreshCopyOf(
        AssignmentConfirmationToken source, ConfirmationResponse freshResponse)
    {
        return new AssignmentConfirmationToken
        {
            Id = source.Id,
            OrganizationId = source.OrganizationId,
            ScheduleAssignmentId = source.ScheduleAssignmentId,
            EnrollmentId = source.EnrollmentId,
            TokenHash = source.TokenHash,
            Response = freshResponse,
            ExpiresAt = source.ExpiresAt,
            RespondedAt = DateTime.UtcNow,
            ScheduleAssignment = source.ScheduleAssignment,
            Enrollment = source.Enrollment,
        };
    }
}
