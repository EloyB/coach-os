using CoachOS.Application.Abstractions;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.MollieConnect;
using CoachOS.Application.MollieConnect.DTOs;
using CoachOS.Application.Payments;
using CoachOS.Application.Payments.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class CampEnrollmentServiceTests
{
    private Mock<ICampRepository> _camps = null!;
    private Mock<ICampEnrollmentRepository> _enrollments = null!;
    private Mock<ICampEnrollmentFormRepository> _forms = null!;
    private Mock<IPaymentService> _payments = null!;
    private Mock<IPaymentRepository> _paymentRepo = null!;
    private Mock<IMollieConnectService> _mollieConnect = null!;
    private Mock<IEmailService> _email = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private CampEnrollmentService _sut = null!;

    private readonly Guid _campId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _camps = new Mock<ICampRepository>();
        _enrollments = new Mock<ICampEnrollmentRepository>();
        _forms = new Mock<ICampEnrollmentFormRepository>();
        _payments = new Mock<IPaymentService>();
        _paymentRepo = new Mock<IPaymentRepository>();
        _mollieConnect = new Mock<IMollieConnectService>();
        _email = new Mock<IEmailService>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _sut = new CampEnrollmentService(_camps.Object, _enrollments.Object, _forms.Object,
            _payments.Object, _paymentRepo.Object,
            _unitOfWork.Object, _mollieConnect.Object, _email.Object,
            NullLogger<CampEnrollmentService>.Instance);
    }

    private Camp BuildCamp(decimal price) => new()
    {
        Id = _campId, OrganizationId = _orgId, Name = "Paaskamp", Price = price,
        StartDate = new DateOnly(2026, 4, 14), EndDate = new DateOnly(2026, 4, 16),
        RegistrationDeadline = DateTime.UtcNow.AddDays(10), MaxParticipants = 20, IsActive = true,
    };

    private SubmitCampEnrollmentRequest Req() => new()
    {
        ParticipantName = "Emma", ParticipantEmail = "emma@example.com", EnrollmentType = "solo",
    };

    private void Happy(decimal price)
    {
        _camps.Setup(r => r.GetByIdPublicAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(BuildCamp(price));
        _forms.Setup(r => r.GetByCampIdReadOnlyAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync((CampEnrollmentForm?)null);
        _enrollments.Setup(r => r.CountActiveByCampAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _enrollments.Setup(r => r.IsDuplicateAsync(_campId, "emma@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
    }

    [Test]
    public async Task Submit_PaidCamp_CreatesPendingPaymentEnrollmentWithoutPayment()
    {
        Happy(120m);

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_campId, Req(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RequiresPayment.Should().BeTrue();
        _enrollments.Verify(r => r.AddAsync(It.Is<CampEnrollment>(e => e.Status == EnrollmentStatus.PendingPayment), It.IsAny<CancellationToken>()), Times.Once);
        // De betaling wordt nu pas op de betaalpagina aangemaakt (ChoosePaymentAsync),
        // niet meer bij submit. En geen pay-link mail.
        _payments.Verify(p => p.CreatePaymentForCampEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(e => e.SendCampEnrollmentPaymentLinkAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Submit_FreeCamp_ConfirmsImmediatelyNoPayment()
    {
        Happy(0m);

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_campId, Req(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RequiresPayment.Should().BeFalse();
        _enrollments.Verify(r => r.AddAsync(It.Is<CampEnrollment>(e => e.Status == EnrollmentStatus.Confirmed), It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(p => p.CreatePaymentForCampEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(e => e.SendCampEnrollmentConfirmedAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── ChoosePaymentAsync ───────────────────────────────────────────────────

    private CampEnrollment BuildPendingEnrollment(Guid id) => new()
    {
        Id = id, OrganizationId = _orgId, CampId = _campId,
        ParticipantName = "Emma", ParticipantEmail = "emma@example.com",
        Status = EnrollmentStatus.PendingPayment,
    };

    [Test]
    public async Task ChoosePayment_Cash_RecordsCashPaymentAndReturnsNullCheckoutUrl()
    {
        Guid enrollmentId = Guid.NewGuid();
        _enrollments.Setup(r => r.GetByIdWithGroupAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPendingEnrollment(enrollmentId));
        _paymentRepo.Setup(r => r.GetLatestByCampEnrollmentIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);
        _payments.Setup(p => p.RecordCampCashPaymentAsync(enrollmentId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        Result<ChooseCampPaymentResultDto> result = await _sut.ChoosePaymentAsync(
            enrollmentId, (int)PaymentMethod.Cash, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CheckoutUrl.Should().BeNull();
        _payments.Verify(p => p.RecordCampCashPaymentAsync(enrollmentId, _orgId, It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(p => p.CreatePaymentForCampEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ChoosePayment_Online_CreatesMolliePaymentAndReturnsCheckoutUrl()
    {
        Guid enrollmentId = Guid.NewGuid();
        _enrollments.Setup(r => r.GetByIdWithGroupAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPendingEnrollment(enrollmentId));
        _paymentRepo.Setup(r => r.GetLatestByCampEnrollmentIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);
        _payments.Setup(p => p.CreatePaymentForCampEnrollmentAsync(enrollmentId, _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreatePaymentResultDto>.Ok(new CreatePaymentResultDto(Guid.NewGuid(), "https://mollie/checkout/abc")));

        Result<ChooseCampPaymentResultDto> result = await _sut.ChoosePaymentAsync(
            enrollmentId, (int)PaymentMethod.Online, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CheckoutUrl.Should().Be("https://mollie/checkout/abc");
        _payments.Verify(p => p.CreatePaymentForCampEnrollmentAsync(enrollmentId, _orgId, It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(p => p.RecordCampCashPaymentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ChoosePayment_AlreadyConfirmed_ReturnsConflict()
    {
        Guid enrollmentId = Guid.NewGuid();
        CampEnrollment confirmed = BuildPendingEnrollment(enrollmentId);
        confirmed.Status = EnrollmentStatus.Confirmed;
        _enrollments.Setup(r => r.GetByIdWithGroupAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmed);

        Result<ChooseCampPaymentResultDto> result = await _sut.ChoosePaymentAsync(
            enrollmentId, (int)PaymentMethod.Cash, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        _payments.Verify(p => p.RecordCampCashPaymentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ChoosePayment_ExistingPendingPayment_ReturnsConflict()
    {
        Guid enrollmentId = Guid.NewGuid();
        _enrollments.Setup(r => r.GetByIdWithGroupAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPendingEnrollment(enrollmentId));
        _paymentRepo.Setup(r => r.GetLatestByCampEnrollmentIdAsync(enrollmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Payment { Id = Guid.NewGuid(), Status = PaymentStatus.Pending });

        Result<ChooseCampPaymentResultDto> result = await _sut.ChoosePaymentAsync(
            enrollmentId, (int)PaymentMethod.Online, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        _payments.Verify(p => p.CreatePaymentForCampEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Submit_DeadlinePassed_ReturnsValidation()
    {
        Camp camp = BuildCamp(120m);
        camp.RegistrationDeadline = DateTime.UtcNow.AddDays(-1);
        _camps.Setup(r => r.GetByIdPublicAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(camp);

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_campId, Req(), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Validation);
    }

    [Test]
    public async Task Submit_Full_ReturnsConflict()
    {
        Happy(120m);
        _enrollments.Setup(r => r.CountActiveByCampAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(20);

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_campId, Req(), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task Submit_GroupMemberAlreadyEnrolled_ReturnsConflict()
    {
        _camps.Setup(r => r.GetByIdPublicAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(BuildCamp(120m));
        _forms.Setup(r => r.GetByCampIdReadOnlyAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync((CampEnrollmentForm?)null);
        _enrollments.Setup(r => r.CountActiveByCampAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _enrollments.Setup(r => r.IsDuplicateAsync(_campId, "emma@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _enrollments.Setup(r => r.IsDuplicateAsync(_campId, "lars@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        SubmitCampEnrollmentRequest req = new()
        {
            ParticipantName = "Emma", ParticipantEmail = "emma@example.com", EnrollmentType = "group",
            GroupMembers = new List<CampGroupMemberDto>
            {
                new() { ParticipantName = "Lars", ParticipantEmail = "lars@example.com" },
            },
        };

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_campId, req, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        _payments.Verify(p => p.CreatePaymentForCampEnrollmentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── CreateManualAsync ────────────────────────────────────────────────────

    private static CreateManualCampEnrollmentRequest ManualReq(bool paid = false) => new()
    {
        ParticipantName = "Emma", ParticipantEmail = "emma@example.com", Paid = paid,
    };

    private void ArrangeManual(decimal price, int active = 0, int? max = 20)
    {
        Camp camp = BuildCamp(price);
        camp.MaxParticipants = max;
        _camps.Setup(r => r.GetByIdPublicAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(camp);
        _forms.Setup(r => r.GetByCampIdReadOnlyAsync(_campId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CampEnrollmentForm?)null);
        _enrollments.Setup(r => r.CountActiveByCampAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(active);
        _payments.Setup(p => p.RecordCampCashPaymentAsync(It.IsAny<Guid>(), _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
        _payments.Setup(p => p.MarkCampCashPaidAsync(_campId, It.IsAny<Guid>(), _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());
    }

    [Test]
    public async Task CreateManual_FreeCamp_ConfirmsAndSendsMailWithoutPayment()
    {
        ArrangeManual(0m);

        Result<Guid> result = await _sut.CreateManualAsync(_campId, _orgId, ManualReq());

        result.IsSuccess.Should().BeTrue();
        _enrollments.Verify(r => r.AddAsync(It.Is<CampEnrollment>(e => e.Status == EnrollmentStatus.Confirmed),
            It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(p => p.RecordCampCashPaymentAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(e => e.SendCampEnrollmentConfirmedAsync(
            "emma@example.com", "Emma", "Paaskamp", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreateManual_FreeMainPriceWithPaidOption_IsPendingPayment()
    {
        ArrangeManual(0m);
        Camp camp = BuildCamp(0m);
        CampPrice option = new() { Id = Guid.NewGuid(), Label = "Leden", Amount = 90m };
        camp.Prices.Add(option);
        _camps.Setup(r => r.GetByIdPublicAsync(_campId, It.IsAny<CancellationToken>())).ReturnsAsync(camp);
        CreateManualCampEnrollmentRequest request = new()
        {
            ParticipantName = "Emma", ParticipantEmail = "emma@example.com", CampPriceId = option.Id,
        };

        Result<Guid> result = await _sut.CreateManualAsync(_campId, _orgId, request);

        result.IsSuccess.Should().BeTrue();
        _enrollments.Verify(r => r.AddAsync(It.Is<CampEnrollment>(e => e.Status == EnrollmentStatus.PendingPayment),
            It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(p => p.RecordCampCashPaymentAsync(It.IsAny<Guid>(), _orgId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateManual_PaidCampNotYetPaid_PendingPaymentWithCashRecordedNotMarked()
    {
        ArrangeManual(150m);

        Result<Guid> result = await _sut.CreateManualAsync(_campId, _orgId, ManualReq(paid: false));

        result.IsSuccess.Should().BeTrue();
        _enrollments.Verify(r => r.AddAsync(It.Is<CampEnrollment>(e => e.Status == EnrollmentStatus.PendingPayment),
            It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(p => p.RecordCampCashPaymentAsync(It.IsAny<Guid>(), _orgId, It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(p => p.MarkCampCashPaidAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(e => e.SendCampEnrollmentConfirmedAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateManual_PaidCampCashReceived_RecordsAndMarksPaid()
    {
        ArrangeManual(150m);

        Result<Guid> result = await _sut.CreateManualAsync(_campId, _orgId, ManualReq(paid: true));

        result.IsSuccess.Should().BeTrue();
        _payments.Verify(p => p.RecordCampCashPaymentAsync(result.Value, _orgId, It.IsAny<CancellationToken>()), Times.Once);
        _payments.Verify(p => p.MarkCampCashPaidAsync(_campId, result.Value, _orgId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateManual_CampOfOtherOrganization_ReturnsNotFound()
    {
        ArrangeManual(0m);

        Result<Guid> result = await _sut.CreateManualAsync(_campId, Guid.NewGuid(), ManualReq());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
        _enrollments.Verify(r => r.AddAsync(It.IsAny<CampEnrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateManual_CampFull_ReturnsConflictAndRollsBack()
    {
        ArrangeManual(0m, active: 20, max: 20);

        Result<Guid> result = await _sut.CreateManualAsync(_campId, _orgId, ManualReq());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateManual_DuplicateEmail_ReturnsConflict()
    {
        ArrangeManual(0m);
        _enrollments.Setup(r => r.IsDuplicateAsync(_campId, "emma@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<Guid> result = await _sut.CreateManualAsync(_campId, _orgId, ManualReq());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
    }

    [Test]
    public async Task CreateManual_MissingRequiredField_ReturnsValidation()
    {
        ArrangeManual(0m);
        _forms.Setup(r => r.GetByCampIdReadOnlyAsync(_campId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampEnrollmentForm
            {
                Fields = [new CampFormField { Id = Guid.NewGuid(), Label = "Shirtmaat", IsRequired = true }],
            });

        Result<Guid> result = await _sut.CreateManualAsync(_campId, _orgId, ManualReq());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public async Task CreateManual_PaymentRecordFails_RollsBackAndFails()
    {
        ArrangeManual(150m);
        _payments.Setup(p => p.RecordCampCashPaymentAsync(It.IsAny<Guid>(), _orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden.")));

        Result<Guid> result = await _sut.CreateManualAsync(_campId, _orgId, ManualReq());

        result.IsSuccess.Should().BeFalse();
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Antwoorden zonder (passend) formulier ────────────────────────────────

    [Test]
    public async Task CreateManual_NoFormButResponseGiven_ReturnsValidationAndStoresNothing()
    {
        ArrangeManual(0m); // kamp zonder formulier
        CreateManualCampEnrollmentRequest request = new()
        {
            ParticipantName = "Emma", ParticipantEmail = "emma@example.com",
            Responses = [new CampFormResponseValueDto { CampFormFieldId = Guid.NewGuid(), Value = "x" }],
        };

        Result<Guid> result = await _sut.CreateManualAsync(_campId, _orgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _enrollments.Verify(r => r.AddFormResponseAsync(It.IsAny<CampFormResponse>(), It.IsAny<CancellationToken>()), Times.Never);
        _enrollments.Verify(r => r.AddAsync(It.IsAny<CampEnrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateManual_ResponseForFieldOfOtherCamp_ReturnsValidation()
    {
        ArrangeManual(0m);
        _forms.Setup(r => r.GetByCampIdReadOnlyAsync(_campId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampEnrollmentForm
            {
                Fields = [new CampFormField { Id = Guid.NewGuid(), Label = "Shirtmaat", IsRequired = false }],
            });
        CreateManualCampEnrollmentRequest request = new()
        {
            ParticipantName = "Emma", ParticipantEmail = "emma@example.com",
            Responses = [new CampFormResponseValueDto { CampFormFieldId = Guid.NewGuid(), Value = "x" }],
        };

        Result<Guid> result = await _sut.CreateManualAsync(_campId, _orgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public async Task Submit_NoFormButResponseGiven_ReturnsValidation()
    {
        Happy(0m); // geen formulier
        SubmitCampEnrollmentRequest request = new()
        {
            ParticipantName = "Emma", ParticipantEmail = "emma@example.com", EnrollmentType = "solo",
            Responses = [new CampFormResponseValueDto { CampFormFieldId = Guid.NewGuid(), Value = "x" }],
        };

        Result<SubmitCampEnrollmentResultDto> result = await _sut.SubmitAsync(_campId, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _enrollments.Verify(r => r.AddAsync(It.IsAny<CampEnrollment>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
