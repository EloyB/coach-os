using CoachOS.Application.Abstractions;
using CoachOS.Application.Billing;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Billing;

[TestFixture]
public class BillingServiceCancelTests
{
    private BillingTestContext _ctx = null!;
    private BillingService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BillingTestContext();
        _sut = _ctx.CreateBillingService();
    }

    [TearDown]
    public void TearDown() => _ctx.Dispose();

    private async Task<SubscriptionPayment> PayMollieAsync(Organization org, SubscriptionPlan plan = SubscriptionPlan.Set)
    {
        await _sut.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)plan, (int)BillingInterval.Monthly, (int)BillingPaymentMethod.Mollie));
        SubscriptionPayment first = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        _ctx.FakeMollie.SetFakeStatus(first.MolliePaymentId!, "paid");
        await _ctx.CreateSyncService(_sut).SyncAsync(first.MolliePaymentId!);
        _ctx.Db.ChangeTracker.Clear();
        return first;
    }

    [Test]
    public async Task Cancel_ActiveRunningPeriod_CancelsMollieAndSetsCancelAtPeriodEnd()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Locked); // periode start vandaag
        await PayMollieAsync(org);
        Subscription before = await _ctx.ReloadSubscriptionAsync(org.Id);

        Result<BillingOverviewDto> result = await _sut.CancelAsync(org.Id);

        result.IsSuccess.Should().BeTrue();
        Subscription after = await _ctx.ReloadSubscriptionAsync(org.Id);
        after.Status.Should().Be(SubscriptionStatus.Cancelled);
        after.CancelAt.Should().Be(new DateOnly(2026, 11, 16));
        after.Plan.Should().Be(SubscriptionPlan.Set);
        _ctx.Mollie.Verify(m => m.CancelSubscriptionAsync(before.MollieCustomerId!, before.MollieSubscriptionId!,
            It.IsAny<CancellationToken>()), Times.Once);
        _ctx.Mollie.Verify(m => m.RefundPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Cancel_PaidDuringTrial_RefundsAndLocks()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));
        SubscriptionPayment first = await PayMollieAsync(org);

        Result<BillingOverviewDto> result = await _sut.CancelAsync(org.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("Locked");
        _ctx.Mollie.Verify(m => m.RefundPaymentAsync(first.MolliePaymentId!, 59.29m, It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _ctx.Mollie.Verify(m => m.CancelSubscriptionAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        Subscription after = await _ctx.ReloadSubscriptionAsync(org.Id);
        after.Status.Should().Be(SubscriptionStatus.Locked);
        after.TrialEndsAt.Should().Be(BillingTestContext.Today.AddDays(-1));
        after.Plan.Should().BeNull();
        after.CurrentPeriodStart.Should().BeNull();
        after.MollieSubscriptionId.Should().BeNull();
        after.MollieCustomerId.Should().NotBeNull();

        SubscriptionPayment refunded = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        refunded.Status.Should().Be(SubscriptionPaymentStatus.Refunded);
        refunded.InvoiceStatus.Should().Be(InvoiceStatus.NotNeeded);
        _ctx.Email.Verify(e => e.SendBillingSuperAdminNoticeAsync(BillingTestContext.SuperAdminEmail,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Cancel_BankTransferDuringTrial_CancelsOpenPaymentAndLocks()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));
        await _sut.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Game, (int)BillingInterval.Yearly, (int)BillingPaymentMethod.BankTransfer));
        _ctx.Db.ChangeTracker.Clear();

        await _sut.CancelAsync(org.Id);

        SubscriptionPayment payment = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        payment.Status.Should().Be(SubscriptionPaymentStatus.Canceled);
        payment.InvoiceStatus.Should().Be(InvoiceStatus.NotNeeded);
        (await _ctx.ReloadSubscriptionAsync(org.Id)).Status.Should().Be(SubscriptionStatus.Locked);
        _ctx.Mollie.Verify(m => m.RefundPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Cancel_BankTransferDuringTrialWithInvoice_AsksSuperAdminForCreditNote()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));
        await _sut.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Game, (int)BillingInterval.Yearly, (int)BillingPaymentMethod.BankTransfer));
        SubscriptionPayment invoiced = (await _ctx.Payments.GetLatestOpenFirstAsync(org.Id))
            ?? _ctx.Db.SubscriptionPayments.Single(p => p.OrganizationId == org.Id);
        invoiced.InvoiceStatus = InvoiceStatus.Created;
        invoiced.InvoiceNumber = "2026-0007";
        await _ctx.Db.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        await _sut.CancelAsync(org.Id);

        _ctx.Email.Verify(e => e.SendBillingSuperAdminNoticeAsync(BillingTestContext.SuperAdminEmail,
            It.IsAny<string>(), It.Is<string>(b => b.Contains("2026-0007") && b.Contains("creditnota")),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(SubscriptionStatus.Trialing)]
    [TestCase(SubscriptionStatus.Legacy)]
    [TestCase(SubscriptionStatus.Locked)]
    public async Task Cancel_NoRunningSubscription_ReturnsValidation(SubscriptionStatus status)
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(status);

        Result<BillingOverviewDto> result = await _sut.CancelAsync(org.Id);

        result.Errors[0].Message.Should().Be("Er is geen lopend abonnement om op te zeggen.");
    }

    [Test]
    public async Task Cancel_Twice_SecondIsValidation()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Locked);
        await PayMollieAsync(org);
        await _sut.CancelAsync(org.Id);
        _ctx.Db.ChangeTracker.Clear();

        (await _sut.CancelAsync(org.Id)).IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task Checkout_AfterCancelDuringTrial_StartsToday()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));
        await PayMollieAsync(org);
        await _sut.CancelAsync(org.Id);
        _ctx.Db.ChangeTracker.Clear();

        Result<CheckoutResultDto> result = await _sut.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Game, (int)BillingInterval.Monthly, (int)BillingPaymentMethod.Mollie));

        result.IsSuccess.Should().BeTrue();
        (await _ctx.ReloadPaymentsAsync(org.Id)).Should().Contain(p =>
            p.Status == SubscriptionPaymentStatus.Open && p.PeriodStart == BillingTestContext.Today);
    }
}
