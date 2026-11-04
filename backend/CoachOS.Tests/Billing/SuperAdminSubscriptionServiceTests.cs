using CoachOS.Application.Abstractions;
using CoachOS.Application.Billing;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Billing;

[TestFixture]
public class SuperAdminSubscriptionServiceTests
{
    private BillingTestContext _ctx = null!;
    private BillingService _billing = null!;
    private Mock<IUserLookupService> _users = null!;
    private SubscriptionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BillingTestContext();
        _billing = _ctx.CreateBillingService();
        _users = new Mock<IUserLookupService>();
        _sut = new SubscriptionService(
            _ctx.Subscriptions, _ctx.Payments, _ctx.Profiles, _ctx.Organizations, _billing, _ctx.Mollie.Object,
            _ctx.Invoicing.Object, _ctx.Email.Object, _users.Object, _ctx.UnitOfWork, _ctx.Time, _ctx.App,
            NullLogger<SubscriptionService>.Instance);
    }

    [TearDown]
    public void TearDown() => _ctx.Dispose();

    private static AssignDealRequest Deal(decimal price, string endDate = "2027-06-30")
        => new((int)SubscriptionPlan.Set, price, endDate);

    [Test]
    public async Task AssignDeal_PriceZero_ActivatesWithoutPayment()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Legacy, withProfile: false);

        Result<SubscriptionDto> result = await _sut.AssignDealAsync(org.Id, Deal(0m));

        result.IsSuccess.Should().BeTrue();
        Subscription subscription = await _ctx.ReloadSubscriptionAsync(org.Id);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.IsDeal.Should().BeTrue();
        subscription.Plan.Should().Be(SubscriptionPlan.Set);
        subscription.Price.Should().Be(0m);
        subscription.CurrentPeriodStart.Should().Be(BillingTestContext.Today);
        subscription.CurrentPeriodEnd.Should().Be(new DateOnly(2027, 6, 30));
        (await _ctx.ReloadPaymentsAsync(org.Id)).Should().BeEmpty();
    }

    [Test]
    public async Task AssignDeal_PricePositive_CreatesBankTransferPaymentAndInvoice()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Legacy);

        await _sut.AssignDealAsync(org.Id, Deal(300m));

        SubscriptionPayment payment = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        payment.Kind.Should().Be(SubscriptionPaymentKind.Deal);
        payment.Method.Should().Be(BillingPaymentMethod.BankTransfer);
        payment.AmountExclVat.Should().Be(300m);
        payment.AmountInclVat.Should().Be(363m);
        payment.PeriodEnd.Should().Be(new DateOnly(2027, 6, 30));
        payment.DueDate.Should().Be(BillingTestContext.Today.AddDays(30));
        payment.InvoiceStatus.Should().Be(InvoiceStatus.Pending);
    }

    [Test]
    public async Task AssignDeal_PricePositiveWithoutProfile_Validation()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Legacy, withProfile: false);

        (await _sut.AssignDealAsync(org.Id, Deal(300m))).Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public async Task AssignDeal_WithRunningMollieSubscription_Conflict()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Locked);
        await _billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Game, (int)BillingInterval.Monthly, (int)BillingPaymentMethod.Mollie));
        SubscriptionPayment first = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        _ctx.FakeMollie.SetFakeStatus(first.MolliePaymentId!, "paid");
        await _ctx.CreateSyncService(_billing).SyncAsync(first.MolliePaymentId!);
        _ctx.Db.ChangeTracker.Clear();

        (await _sut.AssignDealAsync(org.Id, Deal(0m))).Errors[0].Code.Should().Be(ErrorCodes.Conflict);
    }

    [Test]
    public async Task AssignDeal_EndDateNotInFuture_Validation()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Legacy);

        (await _sut.AssignDealAsync(org.Id, Deal(0m, "2026-10-17"))).Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public async Task MarkPaymentPaid_OpenBankTransfer_SetsPaidAndKeepsStatus()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync();
        await _billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Game, (int)BillingInterval.Yearly, (int)BillingPaymentMethod.BankTransfer));
        SubscriptionPayment payment = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        _ctx.Db.ChangeTracker.Clear();

        Result<SubscriptionDto> result = await _sut.MarkPaymentPaidAsync(org.Id, payment.Id);

        result.IsSuccess.Should().BeTrue();
        SubscriptionPayment paid = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        paid.Status.Should().Be(SubscriptionPaymentStatus.Paid);
        paid.PaidAt.Should().NotBeNull();
        (await _ctx.ReloadSubscriptionAsync(org.Id)).Status.Should().Be(SubscriptionStatus.Active);
    }

    [Test]
    public async Task MarkPaymentPaid_MolliePayment_Validation()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync();
        await _billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Game, (int)BillingInterval.Monthly, (int)BillingPaymentMethod.Mollie));
        SubscriptionPayment payment = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();

        (await _sut.MarkPaymentPaidAsync(org.Id, payment.Id)).Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public async Task MarkPaymentPaid_PaymentOfOtherOrganization_NotFound()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync();
        await _billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Game, (int)BillingInterval.Yearly, (int)BillingPaymentMethod.BankTransfer));
        SubscriptionPayment payment = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();

        (await _sut.MarkPaymentPaidAsync(Guid.NewGuid(), payment.Id)).Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public async Task SendPaymentInvitation_NonLegacy_Validation()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Trialing);

        (await _sut.SendPaymentInvitationAsync(org.Id)).Errors[0].Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public async Task SendPaymentInvitation_Legacy_MailsAllActiveAdmins()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Legacy);
        _users.Setup(u => u.GetActiveAdminEmailsAsync(org.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["a@club.be", "b@club.be"]);

        Result result = await _sut.SendPaymentInvitationAsync(org.Id);

        result.IsSuccess.Should().BeTrue();
        _ctx.Email.Verify(e => e.SendSubscriptionInvitationAsync(It.IsIn("a@club.be", "b@club.be"), "TC Brederode",
            "https://app.test/dashboard/settings/subscription", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task GetForOrganization_IncludesProfilePaymentsAndStatus()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync();
        await _billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Match, (int)BillingInterval.Yearly, (int)BillingPaymentMethod.BankTransfer));
        _ctx.Db.ChangeTracker.Clear();

        SubscriptionDto dto = (await _sut.GetForOrganizationAsync(org.Id)).Value!;

        dto.Status.Should().Be("Active");
        dto.Plan.Should().Be("Match");
        dto.PaymentMethod.Should().Be("BankTransfer");
        dto.PriceInclVat.Should().Be(1076.90m);
        dto.Profile!.Name.Should().Be("TC Brederode vzw");
        dto.Payments.Should().ContainSingle().Which.Status.Should().Be("Open");
    }

    // ── Deal opzeggen: de openstaande dealbetaling mag de volledige periode niet meer aanrekenen ──

    private async Task<(Organization Org, SubscriptionPayment Deal)> ActiveDealAsync()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Legacy);
        await _sut.AssignDealAsync(org.Id, Deal(300m));
        _ctx.Db.ChangeTracker.Clear();
        return (org, (await _ctx.ReloadPaymentsAsync(org.Id)).Single());
    }

    [Test]
    public async Task CancelDeal_OpenPaymentNotYetInvoiced_IsCanceledWithoutInvoice()
    {
        (Organization org, _) = await ActiveDealAsync();

        (await _sut.CancelAsync(org.Id)).IsSuccess.Should().BeTrue();

        SubscriptionPayment deal = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        deal.Status.Should().Be(SubscriptionPaymentStatus.Canceled);
        deal.InvoiceStatus.Should().Be(InvoiceStatus.NotNeeded);
        (await _ctx.ReloadSubscriptionAsync(org.Id)).Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Test]
    public async Task CancelDeal_InvoiceAlreadyCreated_CancelsPaymentAndAsksForCreditNote()
    {
        (Organization org, SubscriptionPayment deal) = await ActiveDealAsync();
        SubscriptionPayment stored = _ctx.Db.SubscriptionPayments.Single(p => p.Id == deal.Id);
        stored.InvoiceStatus = InvoiceStatus.Created;
        stored.InvoiceNumber = "FAKE-2026-0042";
        _ctx.Db.SaveChanges();
        _ctx.Db.ChangeTracker.Clear();

        await _sut.CancelAsync(org.Id);

        SubscriptionPayment reloaded = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        reloaded.Status.Should().Be(SubscriptionPaymentStatus.Canceled);
        reloaded.InvoiceStatus.Should().Be(InvoiceStatus.Created);
        _ctx.Email.Verify(e => e.SendBillingSuperAdminNoticeAsync(BillingTestContext.SuperAdminEmail,
            It.IsAny<string>(), It.Is<string>(b => b.Contains("FAKE-2026-0042") && b.Contains("creditnota")),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CancelDeal_AlreadyPaid_KeepsPaymentAndAsksForManualSettlement()
    {
        (Organization org, SubscriptionPayment deal) = await ActiveDealAsync();
        await _sut.MarkPaymentPaidAsync(org.Id, deal.Id);
        _ctx.Db.ChangeTracker.Clear();

        await _sut.CancelAsync(org.Id);

        (await _ctx.ReloadPaymentsAsync(org.Id)).Single().Status.Should().Be(SubscriptionPaymentStatus.Paid);
        _ctx.Email.Verify(e => e.SendBillingSuperAdminNoticeAsync(BillingTestContext.SuperAdminEmail,
            It.IsAny<string>(), It.Is<string>(b => b.Contains("al betaald")),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
