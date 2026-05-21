using System.Security.Cryptography;
using System.Text;
using CoachOS.Application.StudentConfirmation;
using CoachOS.Application.StudentConfirmation.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
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
    private Mock<ILogger<StudentConfirmationService>> _logger = null!;
    private StudentConfirmationService _sut = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _tokenRepo = new Mock<IAssignmentConfirmationTokenRepository>();
        _assignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _seriesRepo = new Mock<ILessonSerieRepository>();
        _paymentRepo = new Mock<IPaymentRepository>();
        _paymentService = new Mock<CoachOS.Application.Payments.IPaymentService>();
        _logger = new Mock<ILogger<StudentConfirmationService>>();

        _sut = new StudentConfirmationService(
            _tokenRepo.Object,
            _assignmentRepo.Object,
            _seriesRepo.Object,
            _paymentRepo.Object,
            _paymentService.Object,
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

    // ── Helpers ──────────────────────────────────────────────────────────────

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
