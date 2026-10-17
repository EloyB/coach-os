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

/// <summary>Regressietests voor de bevindingen van de afsluitende review (C1, C2, I3–I6).</summary>
[TestFixture]
public class BillingReviewFixTests
{
    private BillingTestContext _ctx = null!;
    private BillingService _billing = null!;
    private BillingPaymentSyncService _sync = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BillingTestContext();
        _billing = _ctx.CreateBillingService();
        _sync = _ctx.CreateSyncService(_billing);
    }

    [TearDown]
    public void TearDown() => _ctx.Dispose();

    private static CheckoutRequest Mollie(SubscriptionPlan plan, BillingInterval interval)
        => new((int)plan, (int)interval, (int)BillingPaymentMethod.Mollie);

    private async Task<SubscriptionPayment> CheckoutAsync(Guid orgId, CheckoutRequest request)
    {
        Result<CheckoutResultDto> result = await _billing.StartCheckoutAsync(orgId, request);
        result.IsSuccess.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.Message)));
        _ctx.Db.ChangeTracker.Clear();
        return (await _ctx.ReloadPaymentsAsync(orgId)).OrderBy(p => p.CreatedAt).Last();
    }

    private async Task PayAsync(SubscriptionPayment payment)
    {
        _ctx.FakeMollie.SetFakeStatus(payment.MolliePaymentId!, "paid");
        await _sync.SyncAsync(payment.MolliePaymentId!);
        _ctx.Db.ChangeTracker.Clear();
    }

    private void ExpirePendingCheckouts()
    {
        foreach (SubscriptionPayment open in _ctx.Db.SubscriptionPayments.Where(p => p.Status == SubscriptionPaymentStatus.Open))
            open.Status = SubscriptionPaymentStatus.Expired;
        _ctx.Db.SaveChanges();
        _ctx.Db.ChangeTracker.Clear();
    }

    // ── C1: verlengingsperiode enkel op basis van echt aangerekende periodes ─────────────

    [Test]
    public async Task C1_RecurringAfterRefundedFirstAndNewCheckout_StartsAfterThePaidPeriod()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));
        await PayAsync(await CheckoutAsync(org.Id, Mollie(SubscriptionPlan.Set, BillingInterval.Monthly)));
        await _billing.CancelAsync(org.Id); // terugbetaling + locked
        _ctx.Db.ChangeTracker.Clear();
        await PayAsync(await CheckoutAsync(org.Id, Mollie(SubscriptionPlan.Set, BillingInterval.Monthly))); // 17/10 – 16/11
        Subscription active = await _ctx.ReloadSubscriptionAsync(org.Id);

        string recurringId = _ctx.FakeMollie.AddFakeRecurringPayment(active.MollieSubscriptionId!, active.MollieCustomerId!, 59.29m, "paid");
        await _sync.SyncAsync(recurringId);

        SubscriptionPayment renewal = (await _ctx.ReloadPaymentsAsync(org.Id)).Single(p => p.Kind == SubscriptionPaymentKind.Recurring);
        renewal.PeriodStart.Should().Be(new DateOnly(2026, 11, 17));
    }

    [Test]
    public async Task C1_RecurringAfterAbandonedYearlyCheckout_UsesThePaidMonthlyPeriod()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Locked);
        await CheckoutAsync(org.Id, Mollie(SubscriptionPlan.Set, BillingInterval.Yearly));
        ExpirePendingCheckouts();
        await PayAsync(await CheckoutAsync(org.Id, Mollie(SubscriptionPlan.Set, BillingInterval.Monthly))); // 17/10 – 16/11
        Subscription active = await _ctx.ReloadSubscriptionAsync(org.Id);

        string recurringId = _ctx.FakeMollie.AddFakeRecurringPayment(active.MollieSubscriptionId!, active.MollieCustomerId!, 59.29m, "paid");
        await _sync.SyncAsync(recurringId);

        SubscriptionPayment renewal = (await _ctx.ReloadPaymentsAsync(org.Id)).Single(p => p.Kind == SubscriptionPaymentKind.Recurring);
        renewal.PeriodStart.Should().Be(new DateOnly(2026, 11, 17));
        renewal.PeriodEnd.Should().Be(new DateOnly(2026, 12, 16));
    }

    // ── C2: nooit een tweede Mollie Subscription ─────────────────────────────────────────

    [Test]
    public async Task C2_SecondFirstPaymentPaidWhileRunning_IsRefundedWithoutSecondSubscription()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Locked);
        SubscriptionPayment first = await CheckoutAsync(org.Id, Mollie(SubscriptionPlan.Set, BillingInterval.Monthly));
        // Eerste betaling blijft open maar is ouder dan het blokkeervenster van een uur.
        SubscriptionPayment old = _ctx.Db.SubscriptionPayments.Single(p => p.Id == first.Id);
        old.CreatedAt = _ctx.Time.GetUtcNow().UtcDateTime.AddHours(-3);
        await _ctx.Db.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();
        SubscriptionPayment second = await CheckoutAsync(org.Id, Mollie(SubscriptionPlan.Game, BillingInterval.Monthly));
        await PayAsync(first);

        await PayAsync(second);

        _ctx.Mollie.Verify(m => m.CreateSubscriptionAsync(It.IsAny<BillingSubscriptionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _ctx.Mollie.Verify(m => m.RefundPaymentAsync(second.MolliePaymentId!, second.AmountInclVat, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        (await _ctx.ReloadPaymentsAsync(org.Id)).Single(p => p.Id == second.Id).Status.Should().Be(SubscriptionPaymentStatus.Refunded);
        (await _ctx.ReloadSubscriptionAsync(org.Id)).Plan.Should().Be(SubscriptionPlan.Set);
        _ctx.Email.Verify(e => e.SendBillingSuperAdminNoticeAsync(BillingTestContext.SuperAdminEmail,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── I3: automatische verlenging herstelbaar + webhooks niet verloren ────────────────

    [Test]
    public async Task I3_PaidWithoutMollieSubscription_ReconcileCreatesItWithIdempotencyKey()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Locked);
        SubscriptionPayment first = await CheckoutAsync(org.Id, Mollie(SubscriptionPlan.Set, BillingInterval.Monthly));
        _ctx.Mollie.SetupSequence(m => m.CreateSubscriptionAsync(It.IsAny<BillingSubscriptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Fail(new Error(ErrorCodes.ExternalService, "Mollie plat")))
            .ReturnsAsync(Result<string>.Ok("sub_hersteld"));
        await PayAsync(first);
        (await _ctx.ReloadSubscriptionAsync(org.Id)).MollieSubscriptionId.Should().BeNull();

        int repaired = await _sync.ReconcileMissingRenewalsAsync(BillingTestContext.Today.AddDays(1));

        repaired.Should().Be(1);
        (await _ctx.ReloadSubscriptionAsync(org.Id)).MollieSubscriptionId.Should().Be("sub_hersteld");
        _ctx.Mollie.Verify(m => m.CreateSubscriptionAsync(It.Is<BillingSubscriptionRequest>(r =>
                r.IdempotencyKey == $"subscription-{first.Id}" && r.StartDate == new DateOnly(2026, 11, 17)),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task I3_PaidRecurringForUnknownMollieSubscription_NotifiesSuperAdmin()
    {
        string orphan = _ctx.FakeMollie.AddFakeRecurringPayment("sub_onbekend", "cst_x", 30.25m, "paid");

        await _sync.SyncAsync(orphan);

        _ctx.Email.Verify(e => e.SendBillingSuperAdminNoticeAsync(BillingTestContext.SuperAdminEmail,
            It.IsAny<string>(), It.Is<string>(b => b.Contains("sub_onbekend")), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task I3_MollieUnreachable_SyncReturnsExternalServiceSoWebhookCanRetry()
    {
        _ctx.Mollie.Setup(m => m.GetPaymentDetailsAsync("tr_tijdelijk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BillingPaymentDetails>.Fail(new Error(ErrorCodes.ExternalService, "time-out")));

        Result result = await _sync.SyncAsync("tr_tijdelijk");

        result.Errors[0].Code.Should().Be(ErrorCodes.ExternalService);
        BillingWebhookResponses.StatusCodeFor(result).Should().Be(503);
        BillingWebhookResponses.StatusCodeFor(Result.Ok()).Should().Be(200);
        BillingWebhookResponses.StatusCodeFor(Result.Fail(new Error(ErrorCodes.NotFound, "x"))).Should().Be(200);
    }

    // ── I4: opzeggen idempotent na een gedeeltelijke fout ───────────────────────────────

    [Test]
    public async Task I4_RefundFailsAfterMollieCancel_RetryDoesNotCancelAtMollieAgain()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));
        await PayAsync(await CheckoutAsync(org.Id, Mollie(SubscriptionPlan.Set, BillingInterval.Monthly)));
        _ctx.Mollie.SetupSequence(m => m.RefundPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(new Error(ErrorCodes.ExternalService, "refund plat")))
            .ReturnsAsync(Result.Ok());

        (await _billing.CancelAsync(org.Id)).IsSuccess.Should().BeFalse();
        _ctx.Db.ChangeTracker.Clear();
        Result<BillingOverviewDto> retry = await _billing.CancelAsync(org.Id);

        retry.IsSuccess.Should().BeTrue();
        retry.Value!.Status.Should().Be("Locked");
        _ctx.Mollie.Verify(m => m.CancelSubscriptionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── I5: opzeggen na de verlengingsfactuur bij overschrijving ────────────────────────

    private async Task<Organization> BankTransferYearlyWithRenewalAsync()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Locked);
        await _billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Game, (int)BillingInterval.Yearly, (int)BillingPaymentMethod.BankTransfer)); // 17/10/2026 – 16/10/2027
        _ctx.Db.ChangeTracker.Clear();
        await _ctx.CreateMaintenanceService().RunDailyAsync(new DateOnly(2027, 9, 20)); // verlengingsfactuur volgend jaar
        _ctx.Db.ChangeTracker.Clear();
        return org;
    }

    [Test]
    public async Task I5_CancelBankTransferAfterRenewalInvoice_CancelsNextPeriodAndAsksCreditNote()
    {
        Organization org = await BankTransferYearlyWithRenewalAsync();
        SubscriptionPayment renewal = (await _ctx.ReloadPaymentsAsync(org.Id)).Single(p => p.PeriodStart == new DateOnly(2027, 10, 17));
        SubscriptionPayment tracked = _ctx.Db.SubscriptionPayments.Single(p => p.Id == renewal.Id);
        tracked.InvoiceStatus = InvoiceStatus.Created;
        tracked.InvoiceNumber = "2027-0042";
        await _ctx.Db.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        await _billing.CancelAsync(org.Id);

        SubscriptionPayment after = (await _ctx.ReloadPaymentsAsync(org.Id)).Single(p => p.Id == renewal.Id);
        after.Status.Should().Be(SubscriptionPaymentStatus.Canceled);
        _ctx.Email.Verify(e => e.SendBillingSuperAdminNoticeAsync(BillingTestContext.SuperAdminEmail,
            It.IsAny<string>(), It.Is<string>(b => b.Contains("2027-0042")), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task I5_CancelledBankTransfer_DoesNotRollIntoNextPeriod()
    {
        Organization org = await BankTransferYearlyWithRenewalAsync();
        await _billing.CancelAsync(org.Id);
        _ctx.Db.ChangeTracker.Clear();

        await _ctx.CreateMaintenanceService().RunDailyAsync(new DateOnly(2027, 10, 20));

        Subscription subscription = await _ctx.ReloadSubscriptionAsync(org.Id);
        subscription.CurrentPeriodEnd.Should().Be(new DateOnly(2027, 10, 16));
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    // ── I6: geen echte klanten op fake-integraties ──────────────────────────────────────

    [Test]
    public async Task I6_FakesNotAllowed_CheckoutIsUnavailable()
    {
        _ctx.BillingOptions.Value.AllowFakeIntegrations = false;
        (Organization org, _) = await _ctx.SeedOrganizationAsync();

        Result<CheckoutResultDto> result = await _billing.StartCheckoutAsync(org.Id, Mollie(SubscriptionPlan.Game, BillingInterval.Monthly));
        BillingOverviewDto overview = (await _billing.GetOverviewAsync(org.Id)).Value!;

        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        overview.CheckoutAvailable.Should().BeFalse();
        _ctx.Mollie.Verify(m => m.CreateFirstPaymentAsync(It.IsAny<BillingPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task I6_FakeInvoicingNotAllowed_InvoicesStayPending()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync();
        await _billing.StartCheckoutAsync(org.Id, new CheckoutRequest(
            (int)SubscriptionPlan.Game, (int)BillingInterval.Yearly, (int)BillingPaymentMethod.BankTransfer));
        _ctx.Db.ChangeTracker.Clear();
        _ctx.BillingOptions.Value.AllowFakeIntegrations = false;

        int created = await _ctx.CreateMaintenanceService().ProcessPendingInvoicesAsync();

        created.Should().Be(0);
        (await _ctx.ReloadPaymentsAsync(org.Id)).Single().InvoiceStatus.Should().Be(InvoiceStatus.Pending);
        _ctx.Invoicing.Verify(i => i.CreateInvoiceAsync(It.IsAny<InvoiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Hermes: dev-endpoint enkel voor betalingen van de eigen organisatie ─────────────

    [Test]
    public async Task IsOwnPayment_OnlyForPaymentsOfTheCallerOrganization()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Locked);
        SubscriptionPayment payment = await CheckoutAsync(org.Id, Mollie(SubscriptionPlan.Game, BillingInterval.Monthly));

        (await _sync.IsOwnPaymentAsync(org.Id, payment.MolliePaymentId!)).Should().BeTrue();
        (await _sync.IsOwnPaymentAsync(Guid.NewGuid(), payment.MolliePaymentId!)).Should().BeFalse();
        (await _sync.IsOwnPaymentAsync(org.Id, "tr_onbekend")).Should().BeFalse();
    }

    // ── Hermes-review 2: webhook vóór lokale koppeling, verlenging valideren ───────────

    private async Task<(Organization Org, Subscription Active)> ActiveMollieAsync()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));
        await PayAsync(await CheckoutAsync(org.Id, Mollie(SubscriptionPlan.Set, BillingInterval.Monthly)));
        return (org, await _ctx.ReloadSubscriptionAsync(org.Id));
    }

    [Test]
    public async Task H1_OneOffWebhookArrivesBeforeLocalSave_StillAdvancesPeriod()
    {
        (Organization org, Subscription active) = await ActiveMollieAsync();
        _ctx.Mollie.Setup(m => m.CreatePaymentAsync(It.IsAny<BillingPaymentRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (BillingPaymentRequest request, CancellationToken ct) =>
            {
                Result<BillingPaymentResult> created = await _ctx.FakeMollie.CreatePaymentAsync(request, ct);
                _ctx.FakeMollie.SetFakeStatus(created.Value!.Id, "paid");
                await _sync.SyncAsync(created.Value.Id, ct); // klant betaalt meteen, webhook komt eerst
                return created;
            });
        string recurringId = _ctx.FakeMollie.AddFakeRecurringPayment(
            active.MollieSubscriptionId!, active.MollieCustomerId!, 59.29m, "failed");

        await _sync.SyncAsync(recurringId);
        _ctx.Db.ChangeTracker.Clear();

        SubscriptionPayment oneOff = (await _ctx.ReloadPaymentsAsync(org.Id)).Single(p => p.Kind == SubscriptionPaymentKind.OneOff);
        oneOff.Status.Should().Be(SubscriptionPaymentStatus.Paid);
        oneOff.MolliePaymentId.Should().NotBeNull();
        (await _ctx.ReloadSubscriptionAsync(org.Id)).CurrentPeriodEnd.Should().Be(oneOff.PeriodEnd);
    }

    [Test]
    public async Task H1_FirstPaymentWebhookBeforeMollieIdSaved_ActivatesViaMetadata()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));
        SubscriptionPayment first = await CheckoutAsync(org.Id, Mollie(SubscriptionPlan.Set, BillingInterval.Monthly));
        string molliePaymentId = first.MolliePaymentId!;
        SubscriptionPayment stored = _ctx.Db.SubscriptionPayments.Single(p => p.Id == first.Id);
        stored.MolliePaymentId = null; // crash/race: Mollie kent de betaling, wij nog niet
        _ctx.Db.SaveChanges();
        _ctx.Db.ChangeTracker.Clear();
        _ctx.FakeMollie.SetFakeStatus(molliePaymentId, "paid");

        await _sync.SyncAsync(molliePaymentId);
        _ctx.Db.ChangeTracker.Clear();

        SubscriptionPayment paid = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        paid.Status.Should().Be(SubscriptionPaymentStatus.Paid);
        paid.MolliePaymentId.Should().Be(molliePaymentId);
        (await _ctx.ReloadSubscriptionAsync(org.Id)).Status.Should().Be(SubscriptionStatus.Active);
    }

    [Test]
    public async Task H2_RecurringWithWrongAmount_IsNotRegisteredAndSuperAdminIsNotified()
    {
        (Organization org, Subscription active) = await ActiveMollieAsync();
        DateOnly? periodEnd = active.CurrentPeriodEnd;
        _ctx.Email.Invocations.Clear();
        string recurringId = _ctx.FakeMollie.AddFakeRecurringPayment(
            active.MollieSubscriptionId!, active.MollieCustomerId!, 1.00m, "paid");

        await _sync.SyncAsync(recurringId);

        (await _ctx.ReloadPaymentsAsync(org.Id)).Should().NotContain(p => p.Kind == SubscriptionPaymentKind.Recurring);
        (await _ctx.ReloadSubscriptionAsync(org.Id)).CurrentPeriodEnd.Should().Be(periodEnd);
        _ctx.Email.Verify(e => e.SendBillingSuperAdminNoticeAsync(BillingTestContext.SuperAdminEmail,
            It.IsAny<string>(), It.Is<string>(b => b.Contains(recurringId)), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task H2_RecurringForOtherCustomer_IsNotRegistered()
    {
        (Organization org, Subscription active) = await ActiveMollieAsync();
        DateOnly? periodEnd = active.CurrentPeriodEnd;
        string recurringId = _ctx.FakeMollie.AddFakeRecurringPayment(
            active.MollieSubscriptionId!, "cst_iemand_anders", 59.29m, "paid");

        await _sync.SyncAsync(recurringId);

        (await _ctx.ReloadPaymentsAsync(org.Id)).Should().NotContain(p => p.Kind == SubscriptionPaymentKind.Recurring);
        (await _ctx.ReloadSubscriptionAsync(org.Id)).CurrentPeriodEnd.Should().Be(periodEnd);
    }
}
