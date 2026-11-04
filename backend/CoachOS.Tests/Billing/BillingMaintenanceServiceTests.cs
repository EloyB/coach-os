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
public class BillingMaintenanceServiceTests
{
    private BillingTestContext _ctx = null!;
    private BillingService _billing = null!;
    private BillingMaintenanceService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BillingTestContext();
        _billing = _ctx.CreateBillingService();
        _sut = _ctx.CreateMaintenanceService();
        _ctx.Invoicing.Setup(i => i.CreateInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreatedInvoice>.Ok(new CreatedInvoice("inv_1", "2026-0001", "https://billit.test/1.pdf")));
    }

    [TearDown]
    public void TearDown() => _ctx.Dispose();

    /// <summary>Jaarabonnement Match per overschrijving, periode 05/11/2026 – 04/11/2027.</summary>
    private async Task<Organization> BankTransferYearlyAsync()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));
        await _billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Match, (int)BillingInterval.Yearly, (int)BillingPaymentMethod.BankTransfer));
        _ctx.Db.ChangeTracker.Clear();
        return org;
    }

    [Test]
    public async Task ProcessPendingInvoices_BankTransfer_CreatesUnpaidInvoiceWithDueDateOnce()
    {
        Organization org = await BankTransferYearlyAsync();

        int created = await _sut.ProcessPendingInvoicesAsync();
        _ctx.Db.ChangeTracker.Clear();
        int again = await _sut.ProcessPendingInvoicesAsync();

        created.Should().Be(1);
        again.Should().Be(0);
        SubscriptionPayment payment = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        payment.InvoiceStatus.Should().Be(InvoiceStatus.Created);
        payment.InvoiceNumber.Should().Be("2026-0001");
        payment.InvoicePdfUrl.Should().Be("https://billit.test/1.pdf");
        _ctx.Invoicing.Verify(i => i.CreateInvoiceAsync(It.Is<InvoiceRequest>(r =>
                r.PaymentId == payment.Id
                && !r.IsPaid
                && r.DueDate == BillingTestContext.Today.AddDays(30)
                && r.AmountExclVat == 890m
                && r.VatRate == 0.21m
                && r.PeriodStart == new DateOnly(2026, 11, 5)
                && r.Customer.Name == "TC Brederode vzw"
                && r.Customer.InvoiceEmail == "penningmeester@brederode.be"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProcessPendingInvoices_PaidMolliePayment_CreatesPaidInvoice()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync();
        await _billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Game, (int)BillingInterval.Monthly, (int)BillingPaymentMethod.Mollie));
        SubscriptionPayment first = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        _ctx.FakeMollie.SetFakeStatus(first.MolliePaymentId!, "paid");
        await _ctx.CreateSyncService(_billing).SyncAsync(first.MolliePaymentId!);
        _ctx.Db.ChangeTracker.Clear();

        await _sut.ProcessPendingInvoicesAsync();

        _ctx.Invoicing.Verify(i => i.CreateInvoiceAsync(It.Is<InvoiceRequest>(r => r.IsPaid && r.AmountExclVat == 25m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProcessPendingInvoices_Failure_RecordsErrorAndRetriesAfterBackoff()
    {
        Organization org = await BankTransferYearlyAsync();
        _ctx.Invoicing.Setup(i => i.CreateInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreatedInvoice>.Fail(new Error(ErrorCodes.ExternalService, "Billit plat")));

        await _sut.ProcessPendingInvoicesAsync();
        _ctx.Db.ChangeTracker.Clear();

        SubscriptionPayment failed = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        failed.InvoiceStatus.Should().Be(InvoiceStatus.Failed);
        failed.InvoiceAttempts.Should().Be(1);
        failed.InvoiceLastError.Should().Be("Billit plat");

        // Onmiddellijk opnieuw: backoff nog niet voorbij → geen nieuwe poging.
        await _sut.ProcessPendingInvoicesAsync();
        _ctx.Invoicing.Verify(i => i.CreateInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        // Na de backoff wel.
        _ctx.Time.Advance(TimeSpan.FromMinutes(5));
        _ctx.Db.ChangeTracker.Clear();
        await _sut.ProcessPendingInvoicesAsync();
        _ctx.Invoicing.Verify(i => i.CreateInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task ProcessPendingInvoices_EighthFailure_NotifiesSuperAdminOnce()
    {
        Organization org = await BankTransferYearlyAsync();
        _ctx.Invoicing.Setup(i => i.CreateInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CreatedInvoice>.Fail(new Error(ErrorCodes.ExternalService, "Billit plat")));

        for (int attempt = 0; attempt < 10; attempt++)
        {
            await _sut.ProcessPendingInvoicesAsync();
            _ctx.Time.Advance(TimeSpan.FromDays(1));
            _ctx.Db.ChangeTracker.Clear();
        }

        (await _ctx.ReloadPaymentsAsync(org.Id)).Single().InvoiceAttempts.Should().Be(BillingMaintenanceService.MaxInvoiceAttempts);
        _ctx.Email.Verify(e => e.SendBillingSuperAdminNoticeAsync(BillingTestContext.SuperAdminEmail,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RunDaily_BankTransfer30DaysBeforeEnd_CreatesRenewalPaymentOnce()
    {
        Organization org = await BankTransferYearlyAsync();
        DateOnly day = new(2027, 10, 5); // 30 dagen voor 04/11/2027

        await _sut.RunDailyAsync(day);
        _ctx.Db.ChangeTracker.Clear();
        await _sut.RunDailyAsync(day);

        List<SubscriptionPayment> all = await _ctx.ReloadPaymentsAsync(org.Id);
        all.Should().HaveCount(2);
        SubscriptionPayment renewal = all[1];
        renewal.Kind.Should().Be(SubscriptionPaymentKind.BankTransfer);
        renewal.PeriodStart.Should().Be(new DateOnly(2027, 11, 5));
        renewal.PeriodEnd.Should().Be(new DateOnly(2028, 11, 4));
        renewal.DueDate.Should().Be(new DateOnly(2027, 11, 5));
        renewal.AmountInclVat.Should().Be(1076.90m);
        renewal.InvoiceStatus.Should().Be(InvoiceStatus.Pending);
    }

    [Test]
    public async Task RunDaily_MoreThan30DaysBeforeEnd_DoesNothing()
    {
        Organization org = await BankTransferYearlyAsync();

        await _sut.RunDailyAsync(new DateOnly(2027, 10, 4));

        (await _ctx.ReloadPaymentsAsync(org.Id)).Should().ContainSingle();
    }

    [Test]
    public async Task RunDaily_BankTransferWithUnpaidPrevious_NotifiesSuperAdmin()
    {
        await BankTransferYearlyAsync(); // eerste factuur blijft open

        await _sut.RunDailyAsync(new DateOnly(2027, 10, 5));

        _ctx.Email.Verify(e => e.SendBillingSuperAdminNoticeAsync(BillingTestContext.SuperAdminEmail,
            It.IsAny<string>(), It.Is<string>(b => b.Contains("TC Brederode")), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RunDaily_CancelledBankTransfer_NoRenewal()
    {
        Organization org = await BankTransferYearlyAsync();
        Subscription tracked = (await _ctx.Subscriptions.GetByOrganizationAsync(org.Id, tracked: true))!;
        tracked.Status = SubscriptionStatus.Cancelled;
        tracked.CancelAt = tracked.CurrentPeriodEnd;
        await _ctx.Db.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        await _sut.RunDailyAsync(new DateOnly(2027, 10, 5));

        (await _ctx.ReloadPaymentsAsync(org.Id)).Should().ContainSingle();
    }

    [Test]
    public async Task RunDaily_PeriodRollsOverWhenNextPeriodStarts()
    {
        Organization org = await BankTransferYearlyAsync();
        await _sut.RunDailyAsync(new DateOnly(2027, 10, 5));
        _ctx.Db.ChangeTracker.Clear();

        await _sut.RunDailyAsync(new DateOnly(2027, 11, 5));

        Subscription subscription = await _ctx.ReloadSubscriptionAsync(org.Id);
        subscription.CurrentPeriodStart.Should().Be(new DateOnly(2027, 11, 5));
        subscription.CurrentPeriodEnd.Should().Be(new DateOnly(2028, 11, 4));
    }

    [Test]
    public async Task RunDaily_DealPastEndDate_BecomesCancelled()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Legacy);
        Subscription tracked = (await _ctx.Subscriptions.GetByOrganizationAsync(org.Id, tracked: true))!;
        tracked.Status = SubscriptionStatus.Active;
        tracked.IsDeal = true;
        tracked.Plan = SubscriptionPlan.Set;
        tracked.CurrentPeriodStart = new DateOnly(2026, 1, 1);
        tracked.CurrentPeriodEnd = new DateOnly(2026, 10, 16);
        await _ctx.Db.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        await _sut.RunDailyAsync(BillingTestContext.Today);

        Subscription reloaded = await _ctx.ReloadSubscriptionAsync(org.Id);
        reloaded.Status.Should().Be(SubscriptionStatus.Cancelled);
        reloaded.CancelAt.Should().Be(new DateOnly(2026, 10, 16));
    }
}
