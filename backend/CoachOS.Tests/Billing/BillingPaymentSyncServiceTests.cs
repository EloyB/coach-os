using CoachOS.Application.Abstractions;
using CoachOS.Application.Billing;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Billing;

[TestFixture]
public class BillingPaymentSyncServiceTests
{
    private BillingTestContext _ctx = null!;
    private BillingService _billing = null!;
    private BillingPaymentSyncService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BillingTestContext();
        _billing = _ctx.CreateBillingService();
        _sut = _ctx.CreateSyncService(_billing);
    }

    [TearDown]
    public void TearDown() => _ctx.Dispose();

    /// <summary>Proef tot 04/11/2026, Set per maand via Mollie → open eerste betaling.</summary>
    private async Task<(Organization Org, SubscriptionPayment First)> StartMollieCheckoutAsync()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));
        await _billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Set, (int)BillingInterval.Monthly, (int)BillingPaymentMethod.Mollie));
        _ctx.Db.ChangeTracker.Clear();
        return (org, (await _ctx.ReloadPaymentsAsync(org.Id)).Single());
    }

    [Test]
    public async Task Sync_FirstPaid_ActivatesSubscriptionAndCreatesMollieSubscriptionStartingAfterFirstPeriod()
    {
        (Organization org, SubscriptionPayment first) = await StartMollieCheckoutAsync();
        _ctx.FakeMollie.SetFakeStatus(first.MolliePaymentId!, "paid");

        Result result = await _sut.SyncAsync(first.MolliePaymentId!);

        result.IsSuccess.Should().BeTrue();
        Subscription subscription = await _ctx.ReloadSubscriptionAsync(org.Id);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.Plan.Should().Be(SubscriptionPlan.Set);
        subscription.PaymentMethod.Should().Be(BillingPaymentMethod.Mollie);
        subscription.CurrentPeriodStart.Should().Be(new DateOnly(2026, 11, 5));
        subscription.CurrentPeriodEnd.Should().Be(new DateOnly(2026, 12, 4));
        subscription.MollieSubscriptionId.Should().StartWith("sub_fake_");

        SubscriptionPayment paid = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        paid.Status.Should().Be(SubscriptionPaymentStatus.Paid);
        paid.PaidAt.Should().NotBeNull();
        paid.InvoiceStatus.Should().Be(InvoiceStatus.Pending);

        _ctx.Mollie.Verify(m => m.CreateSubscriptionAsync(It.Is<BillingSubscriptionRequest>(r =>
                r.StartDate == new DateOnly(2026, 12, 5)
                && r.Amount == 59.29m
                && r.Interval == "1 month"
                && r.CustomerId == subscription.MollieCustomerId
                && r.WebhookUrl == "https://api.test/api/webhooks/mollie-billing"),
            It.IsAny<CancellationToken>()), Times.Once);
        _ctx.Email.Verify(e => e.SendSubscriptionStartedAsync(
            "admin@brederode.be", "TC Brederode", "Set", "maand", 59.29m, new DateOnly(2026, 11, 5), false, null,
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Sync_FirstPaid_Twice_CreatesOneMollieSubscription()
    {
        (_, SubscriptionPayment first) = await StartMollieCheckoutAsync();
        _ctx.FakeMollie.SetFakeStatus(first.MolliePaymentId!, "paid");

        await _sut.SyncAsync(first.MolliePaymentId!);
        _ctx.Db.ChangeTracker.Clear();
        await _sut.SyncAsync(first.MolliePaymentId!);

        _ctx.Mollie.Verify(m => m.CreateSubscriptionAsync(It.IsAny<BillingSubscriptionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Sync_FirstFailed_LeavesSubscriptionUntouched()
    {
        (Organization org, SubscriptionPayment first) = await StartMollieCheckoutAsync();
        _ctx.FakeMollie.SetFakeStatus(first.MolliePaymentId!, "failed");

        await _sut.SyncAsync(first.MolliePaymentId!);

        (await _ctx.ReloadSubscriptionAsync(org.Id)).Status.Should().Be(SubscriptionStatus.Trialing);
        (await _ctx.ReloadPaymentsAsync(org.Id)).Single().Status.Should().Be(SubscriptionPaymentStatus.Failed);
        _ctx.Mollie.Verify(m => m.CreateSubscriptionAsync(It.IsAny<BillingSubscriptionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Sync_FirstStillOpen_ChangesNothing()
    {
        (Organization org, SubscriptionPayment first) = await StartMollieCheckoutAsync();

        await _sut.SyncAsync(first.MolliePaymentId!);

        (await _ctx.ReloadPaymentsAsync(org.Id)).Single().Status.Should().Be(SubscriptionPaymentStatus.Open);
    }

    private async Task<(Organization Org, Subscription Subscription)> ActiveMollieSubscriptionAsync()
    {
        (Organization org, SubscriptionPayment first) = await StartMollieCheckoutAsync();
        _ctx.FakeMollie.SetFakeStatus(first.MolliePaymentId!, "paid");
        await _sut.SyncAsync(first.MolliePaymentId!);
        _ctx.Db.ChangeTracker.Clear();
        return (org, await _ctx.ReloadSubscriptionAsync(org.Id));
    }

    [Test]
    public async Task Sync_RecurringPaid_AdvancesPeriodAndQueuesInvoice()
    {
        (Organization org, Subscription subscription) = await ActiveMollieSubscriptionAsync();
        string recurringId = _ctx.FakeMollie.AddFakeRecurringPayment(
            subscription.MollieSubscriptionId!, subscription.MollieCustomerId!, 59.29m, "paid");

        await _sut.SyncAsync(recurringId);

        List<SubscriptionPayment> all = await _ctx.ReloadPaymentsAsync(org.Id);
        SubscriptionPayment renewal = all.Single(p => p.Kind == SubscriptionPaymentKind.Recurring);
        renewal.PeriodStart.Should().Be(new DateOnly(2026, 12, 5));
        renewal.PeriodEnd.Should().Be(new DateOnly(2027, 1, 4));
        renewal.Status.Should().Be(SubscriptionPaymentStatus.Paid);
        renewal.AmountInclVat.Should().Be(59.29m);
        renewal.InvoiceStatus.Should().Be(InvoiceStatus.Pending);

        Subscription reloaded = await _ctx.ReloadSubscriptionAsync(org.Id);
        reloaded.CurrentPeriodStart.Should().Be(new DateOnly(2026, 12, 5));
        reloaded.CurrentPeriodEnd.Should().Be(new DateOnly(2027, 1, 4));
    }

    [Test]
    public async Task Sync_RecurringOpenThenPaid_RegistersOnceAndAdvancesOnPaid()
    {
        (Organization org, Subscription subscription) = await ActiveMollieSubscriptionAsync();
        string recurringId = _ctx.FakeMollie.AddFakeRecurringPayment(
            subscription.MollieSubscriptionId!, subscription.MollieCustomerId!, 59.29m, "open");

        await _sut.SyncAsync(recurringId);
        _ctx.Db.ChangeTracker.Clear();
        (await _ctx.ReloadSubscriptionAsync(org.Id)).CurrentPeriodEnd.Should().Be(new DateOnly(2026, 12, 4));

        _ctx.FakeMollie.SetFakeStatus(recurringId, "paid");
        await _sut.SyncAsync(recurringId);

        (await _ctx.ReloadPaymentsAsync(org.Id)).Count(p => p.Kind == SubscriptionPaymentKind.Recurring).Should().Be(1);
        (await _ctx.ReloadSubscriptionAsync(org.Id)).CurrentPeriodEnd.Should().Be(new DateOnly(2027, 1, 4));
    }

    [Test]
    public async Task Sync_RecurringPaid_SameIdTwice_OnePaymentRow()
    {
        (Organization org, Subscription subscription) = await ActiveMollieSubscriptionAsync();
        string recurringId = _ctx.FakeMollie.AddFakeRecurringPayment(
            subscription.MollieSubscriptionId!, subscription.MollieCustomerId!, 59.29m, "paid");

        await _sut.SyncAsync(recurringId);
        _ctx.Db.ChangeTracker.Clear();
        await _sut.SyncAsync(recurringId);

        (await _ctx.ReloadPaymentsAsync(org.Id)).Count(p => p.Kind == SubscriptionPaymentKind.Recurring).Should().Be(1);
    }

    [Test]
    public async Task Sync_RecurringFailed_CreatesOneOffLinkAndMailsAdminAndSuperAdmin()
    {
        (Organization org, Subscription subscription) = await ActiveMollieSubscriptionAsync();
        string recurringId = _ctx.FakeMollie.AddFakeRecurringPayment(
            subscription.MollieSubscriptionId!, subscription.MollieCustomerId!, 59.29m, "failed");

        await _sut.SyncAsync(recurringId);

        List<SubscriptionPayment> all = await _ctx.ReloadPaymentsAsync(org.Id);
        all.Single(p => p.Kind == SubscriptionPaymentKind.Recurring).Status.Should().Be(SubscriptionPaymentStatus.Failed);
        SubscriptionPayment oneOff = all.Single(p => p.Kind == SubscriptionPaymentKind.OneOff);
        oneOff.Status.Should().Be(SubscriptionPaymentStatus.Open);
        oneOff.PeriodStart.Should().Be(new DateOnly(2026, 12, 5));
        oneOff.CheckoutUrl.Should().NotBeNullOrEmpty();

        _ctx.Email.Verify(e => e.SendSubscriptionPaymentFailedAsync(
            "admin@brederode.be", "TC Brederode", 59.29m, oneOff.CheckoutUrl!, It.IsAny<CancellationToken>()), Times.Once);
        _ctx.Email.Verify(e => e.SendBillingSuperAdminNoticeAsync(
            BillingTestContext.SuperAdminEmail, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        (await _ctx.ReloadSubscriptionAsync(org.Id)).Status.Should().Be(SubscriptionStatus.Active);
    }

    [Test]
    public async Task Sync_OneOffPaidAfterFailure_AdvancesPeriod()
    {
        (Organization org, Subscription subscription) = await ActiveMollieSubscriptionAsync();
        string recurringId = _ctx.FakeMollie.AddFakeRecurringPayment(
            subscription.MollieSubscriptionId!, subscription.MollieCustomerId!, 59.29m, "failed");
        await _sut.SyncAsync(recurringId);
        _ctx.Db.ChangeTracker.Clear();
        SubscriptionPayment oneOff = (await _ctx.ReloadPaymentsAsync(org.Id)).Single(p => p.Kind == SubscriptionPaymentKind.OneOff);

        _ctx.FakeMollie.SetFakeStatus(oneOff.MolliePaymentId!, "paid");
        await _sut.SyncAsync(oneOff.MolliePaymentId!);

        SubscriptionPayment paid = (await _ctx.ReloadPaymentsAsync(org.Id)).Single(p => p.Kind == SubscriptionPaymentKind.OneOff);
        paid.Status.Should().Be(SubscriptionPaymentStatus.Paid);
        paid.InvoiceStatus.Should().Be(InvoiceStatus.Pending);
        (await _ctx.ReloadSubscriptionAsync(org.Id)).CurrentPeriodEnd.Should().Be(new DateOnly(2027, 1, 4));
    }

    [Test]
    public async Task Sync_UnknownPaymentWithoutSubscription_IsIgnored()
    {
        BillingPaymentResult other = (await _ctx.FakeMollie.CreatePaymentAsync(new BillingPaymentRequest(
            10m, "EUR", "x", "https://x", null, null, null))).Value!;
        _ctx.FakeMollie.SetFakeStatus(other.Id, "paid");

        Result result = await _sut.SyncAsync(other.Id);

        result.IsSuccess.Should().BeTrue();
        _ctx.Db.SubscriptionPayments.Should().BeEmpty();
    }

    [Test]
    public async Task SyncLatestCheckout_WithoutWebhook_ActivatesLikeWebhook()
    {
        (Organization org, SubscriptionPayment first) = await StartMollieCheckoutAsync();
        _ctx.FakeMollie.SetFakeStatus(first.MolliePaymentId!, "paid");

        Result<BillingOverviewDto> overview = await _sut.SyncLatestCheckoutAsync(org.Id);

        overview.Value!.Status.Should().Be("Active");
        overview.Value.Plan.Should().Be("Set");
        (await _ctx.ReloadSubscriptionAsync(org.Id)).MollieSubscriptionId.Should().NotBeNull();
    }
}
