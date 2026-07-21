using CoachOS.Application.Configuration;
using CoachOS.Application.MollieConnect;
using CoachOS.Application.Payments;
using CoachOS.Application.Payments.DTOs;
using CoachOS.Application.Pricing;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class PaymentServiceTests
{
    private Mock<IPaymentRepository> _payments = null!;
    private Mock<IEnrollmentRepository> _enrollments = null!;
    private Mock<ILessonSerieRepository> _lessonSeries = null!;
    private Mock<ICampRepository> _camps = null!;
    private Mock<ICampEnrollmentRepository> _campEnrollments = null!;
    private Mock<IOrganizationSettingsRepository> _orgSettings = null!;
    private Mock<IMollieClient> _mollie = null!;
    private Mock<IMollieConnectService> _connect = null!;
    private Mock<IEmailService> _email = null!;
    private Mock<IPricingService> _pricing = null!;
    private PaymentService _sut = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid EnrollmentId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private const string MolliePaymentId = "tr_test_123";

    /// <summary>Totaal uit de prijsmatrix — bewust géén veelvoud van de legacy serieprijs.</summary>
    private const decimal MatrixTotal = 137.50m;

    /// <summary>
    /// Wat IPricingService teruggeeft. De Moq-setup leest dit veld lui uit, zodat een
    /// test de prijs kan herdefiniëren nadat ArrangeEnrollment al een default zette.
    /// </summary>
    private Result<PriceBreakdown> _priceResult = null!;

    private void SetupPrice(decimal total, int groupSize)
        => _priceResult = Result<PriceBreakdown>.Ok(new PriceBreakdown
        {
            Total = total,
            GroupSize = groupSize,
            UsedLegacyPrice = false,
        });

    private void SetupPriceFailure(Error error)
        => _priceResult = Result<PriceBreakdown>.Fail(error);

    [SetUp]
    public void SetUp()
    {
        _payments = new Mock<IPaymentRepository>();
        _enrollments = new Mock<IEnrollmentRepository>();
        _lessonSeries = new Mock<ILessonSerieRepository>();
        _camps = new Mock<ICampRepository>();
        _campEnrollments = new Mock<ICampEnrollmentRepository>();
        _orgSettings = new Mock<IOrganizationSettingsRepository>();
        _mollie = new Mock<IMollieClient>();
        _connect = new Mock<IMollieConnectService>();
        _email = new Mock<IEmailService>();
        _pricing = new Mock<IPricingService>();
        SetupPrice(MatrixTotal, groupSize: 1);
        _pricing
            .Setup(p => p.CalculateForGroupAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Enrollment>>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(_priceResult));

        IOptions<MollieOptions> mollieOptions = Options.Create(new MollieOptions
        {
            WebhookBaseUrl = "https://app.example",
        });
        IOptions<AppOptions> appOptions = Options.Create(new AppOptions
        {
            FrontendBaseUrl = "https://app.example",
        });

        _sut = new PaymentService(
            _payments.Object,
            _enrollments.Object,
            _lessonSeries.Object,
            _camps.Object,
            _campEnrollments.Object,
            _orgSettings.Object,
            _mollie.Object,
            _connect.Object,
            _email.Object,
            _pricing.Object,
            mollieOptions,
            appOptions,
            NullLogger<PaymentService>.Instance);
    }

    // ── CreatePaymentForEnrollmentAsync ──────────────────────────────────────

    [Test]
    public async Task CreatePayment_ZeroPriceSeries_FailsValidation()
    {
        ArrangeEnrollment(price: 0m);

        Result<CreatePaymentResultDto> result = await _sut.CreatePaymentForEnrollmentAsync(EnrollmentId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _mollie.Verify(m => m.CreatePaymentAsync(It.IsAny<string>(), It.IsAny<MolliePaymentRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreatePayment_NoMollieConnection_PropagatesError()
    {
        ArrangeEnrollment(price: 50m);
        _connect.Setup(c => c.GetValidAccessTokenAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Fail(new Error(ErrorCodes.NotFound, "niet gekoppeld")));

        Result<CreatePaymentResultDto> result = await _sut.CreatePaymentForEnrollmentAsync(EnrollmentId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public async Task CreatePayment_HappyPath_SavesPaymentAndReturnsCheckoutUrl()
    {
        ArrangeEnrollment(price: 50m);
        _connect.Setup(c => c.GetValidAccessTokenAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("access-token"));
        _mollie.Setup(m => m.CreatePaymentAsync("access-token", It.IsAny<MolliePaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MolliePaymentCreatedResponse>.Ok(new MolliePaymentCreatedResponse(
                "tr_xxx", "open", "https://www.mollie.com/checkout/xxx")));

        Payment? saved = null;
        _payments.Setup(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        Result<CreatePaymentResultDto> result = await _sut.CreatePaymentForEnrollmentAsync(EnrollmentId, OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CheckoutUrl.Should().Be("https://www.mollie.com/checkout/xxx");
        saved.Should().NotBeNull();
        saved!.Amount.Should().Be(50m);
        saved.Currency.Should().Be("EUR");
        saved.MolliePaymentId.Should().Be("tr_xxx");
        saved.Status.Should().Be(PaymentStatus.Pending);
        saved.PlatformFee.Should().BeNull();
    }

    [Test]
    public async Task CreatePayment_GroupOfFour_ChargesPricePerParticipant()
    {
        // Regressie: online betalingen rekenden enkel series.Price aan, terwijl de
        // cash-paden Price * groepsgrootte boeken. Een groep van 4 betaalde dus 1/4.
        ArrangeEnrollment(price: 50m, groupSize: 4);
        _connect.Setup(c => c.GetValidAccessTokenAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("access-token"));

        MolliePaymentRequest? captured = null;
        _mollie.Setup(m => m.CreatePaymentAsync("access-token", It.IsAny<MolliePaymentRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, MolliePaymentRequest, CancellationToken>((_, r, _) => captured = r)
            .ReturnsAsync(Result<MolliePaymentCreatedResponse>.Ok(new MolliePaymentCreatedResponse(
                "tr_xxx", "open", "https://www.mollie.com/checkout/xxx")));

        Payment? saved = null;
        _payments.Setup(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        Result<CreatePaymentResultDto> result = await _sut.CreatePaymentForEnrollmentAsync(EnrollmentId, OrgId);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(200m);
        saved.Should().NotBeNull();
        saved!.Amount.Should().Be(200m);
    }

    [Test]
    public async Task CreatePayment_PriceMatrix_ChargesMatrixTotal_NotSeriesPriceTimesParticipants()
    {
        // Arrange: groep van 4, legacy prijs 50/persoon → oude formule = 200.
        // De prijsmatrix geeft een groepstotaal van 137,50; dat moet leidend zijn.
        ArrangeEnrollment(price: 50m, groupSize: 4);
        SetupPrice(MatrixTotal, groupSize: 4);

        _connect.Setup(c => c.GetValidAccessTokenAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("access-token"));

        MolliePaymentRequest? captured = null;
        _mollie.Setup(m => m.CreatePaymentAsync("access-token", It.IsAny<MolliePaymentRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, MolliePaymentRequest, CancellationToken>((_, r, _) => captured = r)
            .ReturnsAsync(Result<MolliePaymentCreatedResponse>.Ok(new MolliePaymentCreatedResponse(
                "tr_xxx", "open", "https://www.mollie.com/checkout/xxx")));

        Payment? saved = null;
        _payments.Setup(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        // Act
        Result<CreatePaymentResultDto> result = await _sut.CreatePaymentForEnrollmentAsync(EnrollmentId, OrgId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        captured!.Amount.Should().Be(MatrixTotal);
        saved!.Amount.Should().Be(MatrixTotal);
        saved.Amount.Should().NotBe(200m, "series.Price * deelnemers mag niet meer gebruikt worden");

        // Alle 4 deelnemers (leider inbegrepen) gaan naar de prijsberekening.
        _pricing.Verify(p => p.CalculateForGroupAsync(
                SeriesId,
                It.Is<IReadOnlyList<Enrollment>>(l => l.Count == 4 && l.Any(e => e.Id == EnrollmentId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreatePayment_ZeroLegacyPriceButPricedMatrix_Succeeds()
    {
        // Regressie: de oude guard keek naar series.Price. Een reeks met alleen een
        // prijsmatrix (Price = 0) moet gewoon online betaalbaar zijn.
        ArrangeEnrollment(price: 0m, groupSize: 2);
        SetupPrice(MatrixTotal, groupSize: 2);

        _connect.Setup(c => c.GetValidAccessTokenAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("access-token"));
        _mollie.Setup(m => m.CreatePaymentAsync("access-token", It.IsAny<MolliePaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MolliePaymentCreatedResponse>.Ok(new MolliePaymentCreatedResponse(
                "tr_xxx", "open", "https://www.mollie.com/checkout/xxx")));

        Payment? saved = null;
        _payments.Setup(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        Result<CreatePaymentResultDto> result = await _sut.CreatePaymentForEnrollmentAsync(EnrollmentId, OrgId);

        result.IsSuccess.Should().BeTrue();
        saved!.Amount.Should().Be(MatrixTotal);
    }

    [Test]
    public async Task CreatePayment_PricingFails_PropagatesErrorAndCallsNoMollie()
    {
        ArrangeEnrollment(price: 50m);
        SetupPriceFailure(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        Result<CreatePaymentResultDto> result = await _sut.CreatePaymentForEnrollmentAsync(EnrollmentId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
        _mollie.Verify(m => m.CreatePaymentAsync(
                It.IsAny<string>(), It.IsAny<MolliePaymentRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreatePayment_GroupOfFourWithPlatformFee_CalculatesFeeOverGroupTotal()
    {
        ArrangeEnrollment(price: 100m, groupSize: 4);
        _orgSettings.Setup(o => o.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationSettings
            {
                OrganizationId = OrgId,
                PlatformFeePercentage = 2.5m,
                PaymentCurrency = "EUR",
            });
        _connect.Setup(c => c.GetValidAccessTokenAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("access-token"));

        MolliePaymentRequest? captured = null;
        _mollie.Setup(m => m.CreatePaymentAsync("access-token", It.IsAny<MolliePaymentRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, MolliePaymentRequest, CancellationToken>((_, r, _) => captured = r)
            .ReturnsAsync(Result<MolliePaymentCreatedResponse>.Ok(new MolliePaymentCreatedResponse(
                "tr_xxx", "open", "https://www.mollie.com/checkout/xxx")));

        Result<CreatePaymentResultDto> result = await _sut.CreatePaymentForEnrollmentAsync(EnrollmentId, OrgId);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        // 4 × €100 = €400; 2,5% platform fee = €10 (niet €2,50 over één deelnemer).
        captured!.Amount.Should().Be(400m);
        captured.ApplicationFee.Should().Be(10m);
    }

    [Test]
    public async Task CreatePayment_SoloEnrollment_ChargesSinglePrice()
    {
        ArrangeEnrollment(price: 50m, groupSize: 1);
        _connect.Setup(c => c.GetValidAccessTokenAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("access-token"));

        MolliePaymentRequest? captured = null;
        _mollie.Setup(m => m.CreatePaymentAsync("access-token", It.IsAny<MolliePaymentRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, MolliePaymentRequest, CancellationToken>((_, r, _) => captured = r)
            .ReturnsAsync(Result<MolliePaymentCreatedResponse>.Ok(new MolliePaymentCreatedResponse(
                "tr_xxx", "open", "https://www.mollie.com/checkout/xxx")));

        Result<CreatePaymentResultDto> result = await _sut.CreatePaymentForEnrollmentAsync(EnrollmentId, OrgId);

        result.IsSuccess.Should().BeTrue();
        captured!.Amount.Should().Be(50m);
    }

    [Test]
    public async Task CreatePayment_WithPlatformFee_PassesApplicationFeeAndStoresSnapshot()
    {
        ArrangeEnrollment(price: 100m);
        _orgSettings.Setup(o => o.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationSettings
            {
                OrganizationId = OrgId,
                PlatformFeePercentage = 2.5m,
                PaymentCurrency = "EUR",
            });
        _connect.Setup(c => c.GetValidAccessTokenAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("access-token"));

        MolliePaymentRequest? captured = null;
        _mollie.Setup(m => m.CreatePaymentAsync("access-token", It.IsAny<MolliePaymentRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, MolliePaymentRequest, CancellationToken>((_, r, _2) => captured = r)
            .ReturnsAsync(Result<MolliePaymentCreatedResponse>.Ok(new MolliePaymentCreatedResponse(
                "tr_y", "open", "https://checkout")));

        Payment? saved = null;
        _payments.Setup(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        Result<CreatePaymentResultDto> result = await _sut.CreatePaymentForEnrollmentAsync(EnrollmentId, OrgId);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.ApplicationFee.Should().Be(2.50m);
        captured.WebhookUrl.Should().Be("https://app.example/api/webhooks/mollie");
        saved!.PlatformFee.Should().Be(2.50m);
    }

    // ── SyncPaymentFromMollieAsync ────────────────────────────────────────────

    [Test]
    public async Task Sync_UnknownPaymentId_ReturnsOkAndDoesNothing()
    {
        _payments.Setup(p => p.GetByMolliePaymentIdAsync("nope", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        Result result = await _sut.SyncPaymentFromMollieAsync("nope");

        result.IsSuccess.Should().BeTrue();
        _mollie.Verify(m => m.GetPaymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Sync_AlreadyPaid_IsIdempotentNoop()
    {
        Payment existing = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            EnrollmentId = EnrollmentId,
            Status = PaymentStatus.Paid,
            MolliePaymentId = MolliePaymentId,
        };
        _payments.Setup(p => p.GetByMolliePaymentIdAsync(MolliePaymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        Result result = await _sut.SyncPaymentFromMollieAsync(MolliePaymentId);

        result.IsSuccess.Should().BeTrue();
        _mollie.Verify(m => m.GetPaymentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _payments.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Sync_PaidTransition_UpdatesPaymentAndConfirmsEnrollment()
    {
        Payment payment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            EnrollmentId = EnrollmentId,
            Status = PaymentStatus.Pending,
            MolliePaymentId = MolliePaymentId,
            Amount = 50m,
        };
        Enrollment enrollment = new()
        {
            Id = EnrollmentId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            StudentEmail = "student@example.com",
            StudentName = "Test Student",
            Status = EnrollmentStatus.PendingPayment,
        };
        _payments.Setup(p => p.GetByMolliePaymentIdAsync(MolliePaymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _connect.Setup(c => c.GetValidAccessTokenAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("access-token"));
        _mollie.Setup(m => m.GetPaymentAsync("access-token", MolliePaymentId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MolliePaymentSnapshot>.Ok(new MolliePaymentSnapshot(
                MolliePaymentId, "paid", 50m, "EUR", DateTime.UtcNow, "bancontact", null)));
        _enrollments.Setup(e => e.GetByIdWithGroupAsync(EnrollmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
        _lessonSeries.Setup(l => l.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LessonSerie { Id = SeriesId, Name = "Voorjaar 2026" });

        Result result = await _sut.SyncPaymentFromMollieAsync(MolliePaymentId);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.PaidAt.Should().NotBeNull();
        enrollment.Status.Should().Be(EnrollmentStatus.Confirmed);
        _email.Verify(e => e.SendEnrollmentConfirmationAsync(
            "student@example.com", "Test Student", "Voorjaar 2026", string.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Sync_PaidTransition_GroupEnrollment_ConfirmsAllMembers()
    {
        // Regressie: na een geslaagde groepsbetaling werd enkel de betalende leider
        // bevestigd; de overige leden (die de confirm-flow op PendingPayment zette)
        // bleven hangen. Alle leden moeten mee naar Confirmed.
        Guid leaderId = EnrollmentId;
        Guid memberId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();

        Enrollment leader = new()
        {
            Id = leaderId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            StudentEmail = "leider@example.com",
            StudentName = "De Leider",
            Status = EnrollmentStatus.PendingPayment,
            EnrollmentGroupId = groupId,
        };
        Enrollment member = new()
        {
            Id = memberId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            StudentEmail = "lid@example.com",
            StudentName = "Het Lid",
            Status = EnrollmentStatus.PendingPayment,
            EnrollmentGroupId = groupId,
        };
        EnrollmentGroup group = new()
        {
            Id = groupId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            LeaderEnrollmentId = leaderId,
            Members = [leader, member],
        };
        leader.EnrollmentGroup = group;

        Payment payment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            EnrollmentId = leaderId,
            Status = PaymentStatus.Pending,
            MolliePaymentId = MolliePaymentId,
            Amount = 100m,
        };
        _payments.Setup(p => p.GetByMolliePaymentIdAsync(MolliePaymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _connect.Setup(c => c.GetValidAccessTokenAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("access-token"));
        _mollie.Setup(m => m.GetPaymentAsync("access-token", MolliePaymentId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MolliePaymentSnapshot>.Ok(new MolliePaymentSnapshot(
                MolliePaymentId, "paid", 100m, "EUR", DateTime.UtcNow, "ideal", null)));
        _enrollments.Setup(e => e.GetByIdWithGroupAsync(leaderId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leader);
        _lessonSeries.Setup(l => l.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LessonSerie { Id = SeriesId, Name = "Voorjaar 2026" });

        Result result = await _sut.SyncPaymentFromMollieAsync(MolliePaymentId);

        result.IsSuccess.Should().BeTrue();
        leader.Status.Should().Be(EnrollmentStatus.Confirmed);
        member.Status.Should().Be(EnrollmentStatus.Confirmed, "een groepslid mag niet blijven hangen op PendingPayment");
    }

    [Test]
    public async Task Sync_FailedTransition_UpdatesPaymentDoesNotConfirm()
    {
        Payment payment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            EnrollmentId = EnrollmentId,
            Status = PaymentStatus.Pending,
            MolliePaymentId = MolliePaymentId,
        };
        _payments.Setup(p => p.GetByMolliePaymentIdAsync(MolliePaymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _connect.Setup(c => c.GetValidAccessTokenAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("access-token"));
        _mollie.Setup(m => m.GetPaymentAsync("access-token", MolliePaymentId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MolliePaymentSnapshot>.Ok(new MolliePaymentSnapshot(
                MolliePaymentId, "failed", 50m, "EUR", null, null, "insufficient_funds")));

        Result result = await _sut.SyncPaymentFromMollieAsync(MolliePaymentId);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be("insufficient_funds");
        _enrollments.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── RecordCampCashPaymentAsync ────────────────────────────────────────────

    [Test]
    public async Task RecordCampCash_PaidCamp_AddsPendingCashPaymentLeavesEnrollmentPendingPayment()
    {
        Guid campEnrollmentId = Guid.NewGuid();
        Guid campId = Guid.NewGuid();
        CampEnrollment enrollment = new()
        {
            Id = campEnrollmentId,
            OrganizationId = OrgId,
            CampId = campId,
            Status = EnrollmentStatus.PendingPayment,
        };
        _campEnrollments.Setup(c => c.GetByIdWithGroupAsync(campEnrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
        _camps.Setup(c => c.GetByIdPublicAsync(campId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Camp { Id = campId, OrganizationId = OrgId, Name = "Paaskamp", Price = 120m });

        Payment? saved = null;
        _payments.Setup(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        Result result = await _sut.RecordCampCashPaymentAsync(campEnrollmentId, OrgId);

        result.IsSuccess.Should().BeTrue();
        saved.Should().NotBeNull();
        saved!.Method.Should().Be(PaymentMethod.Cash);
        saved.Status.Should().Be(PaymentStatus.Pending);
        saved.Amount.Should().Be(120m);
        saved.CampEnrollmentId.Should().Be(campEnrollmentId);
        enrollment.Status.Should().Be(EnrollmentStatus.PendingPayment);
    }

    [Test]
    public async Task RecordCampCash_FreeCamp_FailsValidation()
    {
        Guid campEnrollmentId = Guid.NewGuid();
        Guid campId = Guid.NewGuid();
        _campEnrollments.Setup(c => c.GetByIdWithGroupAsync(campEnrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampEnrollment { Id = campEnrollmentId, OrganizationId = OrgId, CampId = campId });
        _camps.Setup(c => c.GetByIdPublicAsync(campId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Camp { Id = campId, OrganizationId = OrgId, Name = "Gratis", Price = 0m });

        Result result = await _sut.RecordCampCashPaymentAsync(campEnrollmentId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _payments.Verify(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── MarkCampCashPaidAsync ─────────────────────────────────────────────────

    [Test]
    public async Task MarkCampCashPaid_PendingCashExists_MarksPaidAndConfirmsEnrollment()
    {
        Guid campEnrollmentId = Guid.NewGuid();
        Guid campId = Guid.NewGuid();
        Payment cash = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            CampEnrollmentId = campEnrollmentId,
            Method = PaymentMethod.Cash,
            Status = PaymentStatus.Pending,
        };
        CampEnrollment enrollment = new()
        {
            Id = campEnrollmentId,
            OrganizationId = OrgId,
            CampId = campId,
            ParticipantEmail = "emma@example.com",
            ParticipantName = "Emma",
            Status = EnrollmentStatus.PendingPayment,
            Camp = new Camp { Id = campId, Name = "Paaskamp" },
        };
        _payments.Setup(p => p.GetLatestPendingCashByCampEnrollmentIdAsync(campEnrollmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cash);
        _campEnrollments.Setup(c => c.GetByIdWithGroupAsync(campEnrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        Result result = await _sut.MarkCampCashPaidAsync(campId, campEnrollmentId, OrgId);

        result.IsSuccess.Should().BeTrue();
        cash.Status.Should().Be(PaymentStatus.Paid);
        cash.PaidAt.Should().NotBeNull();
        enrollment.Status.Should().Be(EnrollmentStatus.Confirmed);
        _email.Verify(e => e.SendCampEnrollmentConfirmedAsync(
            "emma@example.com", "Emma", "Paaskamp", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task MarkCampCashPaid_WrongCamp_ReturnsNotFoundAndDoesNotConfirm()
    {
        // De route bevat het kamp-id; een inschrijving van een ánder kamp (zelfde org)
        // mag niet bevestigd worden via een verkeerd kamp-id.
        Guid campEnrollmentId = Guid.NewGuid();
        Guid routeCampId = Guid.NewGuid();
        Guid actualCampId = Guid.NewGuid();
        CampEnrollment enrollment = new()
        {
            Id = campEnrollmentId,
            OrganizationId = OrgId,
            CampId = actualCampId,
            ParticipantEmail = "emma@example.com",
            ParticipantName = "Emma",
            Status = EnrollmentStatus.PendingPayment,
        };
        _campEnrollments.Setup(c => c.GetByIdWithGroupAsync(campEnrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);

        Result result = await _sut.MarkCampCashPaidAsync(routeCampId, campEnrollmentId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
        enrollment.Status.Should().Be(EnrollmentStatus.PendingPayment);
        _email.Verify(e => e.SendCampEnrollmentConfirmedAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task MarkCampCashPaid_NoPendingCash_ReturnsNotFound()
    {
        Guid campEnrollmentId = Guid.NewGuid();
        Guid campId = Guid.NewGuid();
        _campEnrollments.Setup(c => c.GetByIdWithGroupAsync(campEnrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampEnrollment
            {
                Id = campEnrollmentId,
                OrganizationId = OrgId,
                CampId = campId,
            });
        _payments.Setup(p => p.GetLatestPendingCashByCampEnrollmentIdAsync(campEnrollmentId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        Result result = await _sut.MarkCampCashPaidAsync(campId, campEnrollmentId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ArrangeEnrollment(decimal price, int groupSize = 1)
    {
        _mollie.Setup(m => m.GetFirstProfileIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Ok("pfl_test"));

        Enrollment enrollment = new()
        {
            Id = EnrollmentId,
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            StudentEmail = "x@y.com",
            StudentName = "X",
        };

        if (groupSize > 1)
        {
            // Group.Members bevat de leider zelf, dus Count == totaal aantal deelnemers.
            Guid groupId = Guid.NewGuid();
            EnrollmentGroup group = new()
            {
                Id = groupId,
                OrganizationId = OrgId,
                LessonSerieId = SeriesId,
                LeaderEnrollmentId = EnrollmentId,
                Members = [enrollment],
            };
            for (int i = 1; i < groupSize; i++)
            {
                group.Members.Add(new Enrollment
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = OrgId,
                    LessonSerieId = SeriesId,
                    StudentEmail = $"lid{i}@y.com",
                    StudentName = $"Lid {i}",
                    EnrollmentGroupId = groupId,
                });
            }
            enrollment.EnrollmentGroupId = groupId;
            enrollment.EnrollmentGroup = group;
        }

        // Standaard: prijsmatrix ontbreekt, dus IPricingService valt terug op de
        // legacy prijs per persoon. Zo blijven de bestaande verwachtingen geldig
        // terwijl het bedrag wél via de service loopt.
        SetupPrice(price * groupSize, groupSize);

        _enrollments.Setup(e => e.GetByIdWithGroupAsync(EnrollmentId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
        _lessonSeries.Setup(l => l.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LessonSerie
            {
                Id = SeriesId,
                Name = "Test Reeks",
                Price = price,
                OrganizationId = OrgId,
            });
        _orgSettings.Setup(o => o.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationSettings
            {
                OrganizationId = OrgId,
                PlatformFeePercentage = 0m,
                PaymentCurrency = "EUR",
            });
    }
}
