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
public class BillingServiceCheckoutTests
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

    private static CheckoutRequest Checkout(SubscriptionPlan plan, BillingInterval interval, BillingPaymentMethod method)
        => new((int)plan, (int)interval, (int)method);

    [Test]
    public async Task StartCheckout_Mollie_CreatesOpenFirstPaymentWithGrossAmountAndPeriodAfterTrial()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));

        Result<CheckoutResultDto> result = await _sut.StartCheckoutAsync(
            org.Id, Checkout(SubscriptionPlan.Set, BillingInterval.Monthly, BillingPaymentMethod.Mollie));

        result.IsSuccess.Should().BeTrue();
        result.Value!.CheckoutUrl.Should().StartWith("https://app.test/dashboard/settings/subscription?fakePayment=tr_fake_");

        SubscriptionPayment payment = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        payment.Kind.Should().Be(SubscriptionPaymentKind.First);
        payment.Status.Should().Be(SubscriptionPaymentStatus.Open);
        payment.Plan.Should().Be(SubscriptionPlan.Set);
        payment.AmountExclVat.Should().Be(49m);
        payment.VatRate.Should().Be(0.21m);
        payment.AmountInclVat.Should().Be(59.29m);
        payment.PeriodStart.Should().Be(new DateOnly(2026, 11, 5));
        payment.PeriodEnd.Should().Be(new DateOnly(2026, 12, 4));
        payment.MolliePaymentId.Should().StartWith("tr_fake_");
        payment.InvoiceStatus.Should().Be(InvoiceStatus.NotNeeded);

        Subscription subscription = await _ctx.ReloadSubscriptionAsync(org.Id);
        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
        subscription.Plan.Should().BeNull();
        subscription.MollieCustomerId.Should().StartWith("cst_fake_");

        _ctx.Mollie.Verify(m => m.CreateFirstPaymentAsync(
            It.Is<BillingPaymentRequest>(r => r.Amount == 59.29m
                && r.WebhookUrl == "https://api.test/api/webhooks/mollie-billing"
                && r.RedirectUrl == "https://app.test/dashboard/settings/subscription?return=1"
                && r.Metadata!["subscriptionPaymentId"] == payment.Id.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StartCheckout_BankTransferYearly_ActivatesImmediatelyWithDueDate()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(trialEndsAt: new DateOnly(2026, 11, 4));

        Result<CheckoutResultDto> result = await _sut.StartCheckoutAsync(
            org.Id, Checkout(SubscriptionPlan.Match, BillingInterval.Yearly, BillingPaymentMethod.BankTransfer));

        result.IsSuccess.Should().BeTrue();
        result.Value!.CheckoutUrl.Should().BeNull();

        Subscription subscription = await _ctx.ReloadSubscriptionAsync(org.Id);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.Plan.Should().Be(SubscriptionPlan.Match);
        subscription.Interval.Should().Be(BillingInterval.Yearly);
        subscription.PaymentMethod.Should().Be(BillingPaymentMethod.BankTransfer);
        subscription.Price.Should().Be(890m);
        subscription.StudentLimit.Should().Be(500);
        subscription.OverageMarginPercent.Should().Be(10m);
        subscription.OveragePricePerStudent.Should().Be(3m);
        subscription.CurrentPeriodStart.Should().Be(new DateOnly(2026, 11, 5));
        subscription.CurrentPeriodEnd.Should().Be(new DateOnly(2027, 11, 4));

        SubscriptionPayment payment = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        payment.Kind.Should().Be(SubscriptionPaymentKind.BankTransfer);
        payment.Status.Should().Be(SubscriptionPaymentStatus.Open);
        payment.AmountInclVat.Should().Be(1076.90m);
        payment.DueDate.Should().Be(BillingTestContext.Today.AddDays(30));
        payment.InvoiceStatus.Should().Be(InvoiceStatus.Pending);

        _ctx.Mollie.Verify(m => m.CreateFirstPaymentAsync(It.IsAny<BillingPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _ctx.Email.Verify(e => e.SendSubscriptionStartedAsync(
            "admin@brederode.be", "TC Brederode", "Match", "jaar", 1076.90m, new DateOnly(2026, 11, 5), true,
            BillingTestContext.Today.AddDays(30), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StartCheckout_BankTransferMonthly_IsRejected()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync();

        Result<CheckoutResultDto> result = await _sut.StartCheckoutAsync(
            org.Id, Checkout(SubscriptionPlan.Game, BillingInterval.Monthly, BillingPaymentMethod.BankTransfer));

        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        (await _ctx.ReloadPaymentsAsync(org.Id)).Should().BeEmpty();
    }

    [TestCase(SubscriptionStatus.Active)]
    [TestCase(SubscriptionStatus.Cancelled)]
    public async Task StartCheckout_WhileSubscriptionRuns_ReturnsConflictAndCallsNoMollie(SubscriptionStatus status)
    {
        (Organization org, Subscription seeded) = await _ctx.SeedOrganizationAsync(status);
        Subscription tracked = (await _ctx.Subscriptions.GetByOrganizationAsync(org.Id, tracked: true))!;
        tracked.CurrentPeriodEnd = BillingTestContext.Today.AddDays(10);
        tracked.CancelAt = status == SubscriptionStatus.Cancelled ? BillingTestContext.Today.AddDays(10) : null;
        await _ctx.Db.SaveChangesAsync();

        Result<CheckoutResultDto> result = await _sut.StartCheckoutAsync(
            org.Id, Checkout(SubscriptionPlan.Game, BillingInterval.Monthly, BillingPaymentMethod.Mollie));

        result.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        _ctx.Mollie.Verify(m => m.CreateFirstPaymentAsync(It.IsAny<BillingPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task StartCheckout_RecentOpenFirstPayment_ReturnsConflict()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync();
        await _sut.StartCheckoutAsync(org.Id, Checkout(SubscriptionPlan.Game, BillingInterval.Monthly, BillingPaymentMethod.Mollie));

        Result<CheckoutResultDto> second = await _sut.StartCheckoutAsync(
            org.Id, Checkout(SubscriptionPlan.Set, BillingInterval.Monthly, BillingPaymentMethod.Mollie));

        second.Errors[0].Code.Should().Be(ErrorCodes.Conflict);
        (await _ctx.ReloadPaymentsAsync(org.Id)).Should().ContainSingle();
    }

    [Test]
    public async Task StartCheckout_WithoutProfile_ReturnsValidation()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(withProfile: false);

        Result<CheckoutResultDto> result = await _sut.StartCheckoutAsync(
            org.Id, Checkout(SubscriptionPlan.Game, BillingInterval.Monthly, BillingPaymentMethod.Mollie));

        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        result.Errors[0].Message.Should().Be("Vul eerst je facturatiegegevens in.");
    }

    [Test]
    public async Task StartCheckout_FromLocked_StartsToday()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Locked);

        await _sut.StartCheckoutAsync(org.Id, Checkout(SubscriptionPlan.Game, BillingInterval.Yearly, BillingPaymentMethod.Mollie));

        SubscriptionPayment payment = (await _ctx.ReloadPaymentsAsync(org.Id)).Single();
        payment.PeriodStart.Should().Be(BillingTestContext.Today);
        payment.PeriodEnd.Should().Be(new DateOnly(2027, 10, 16));
        payment.AmountInclVat.Should().Be(302.50m);
    }

    [Test]
    public async Task StartCheckout_ReusesExistingMollieCustomer()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(SubscriptionStatus.Locked);
        Subscription tracked = (await _ctx.Subscriptions.GetByOrganizationAsync(org.Id, tracked: true))!;
        tracked.MollieCustomerId = "cst_bestaand";
        await _ctx.Db.SaveChangesAsync();

        await _sut.StartCheckoutAsync(org.Id, Checkout(SubscriptionPlan.Game, BillingInterval.Monthly, BillingPaymentMethod.Mollie));

        _ctx.Mollie.Verify(m => m.CreateCustomerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _ctx.Mollie.Verify(m => m.CreateFirstPaymentAsync(
            It.Is<BillingPaymentRequest>(r => r.CustomerId == "cst_bestaand"), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SaveBillingProfileRequest Profile(string country = "BE", string? vat = "0123.456.789")
        => new("TC Brederode vzw", "Molenstraat 1", "2000", "Antwerpen", country, "f@brederode.be", vat);

    [Test]
    public async Task SaveProfile_CreatesProfileWithNormalizedVerifiedVat()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(withProfile: false);

        Result<BillingProfileDto> result = await _sut.SaveBillingProfileAsync(org.Id, Profile());

        result.IsSuccess.Should().BeTrue();
        result.Value!.VatNumber.Should().Be("BE0123456789");
        result.Value.VatVerified.Should().BeTrue();
    }

    [Test]
    public async Task SaveProfile_UpdatesExistingProfile()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync();

        await _sut.SaveBillingProfileAsync(org.Id, Profile(vat: null));

        _ctx.Db.BillingProfiles.Count(p => p.OrganizationId == org.Id).Should().Be(1);
        _ctx.Db.BillingProfiles.Single(p => p.OrganizationId == org.Id).InvoiceEmail.Should().Be("f@brederode.be");
    }

    [Test]
    public async Task SaveProfile_NonBelgianCountry_IsRejected()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(withProfile: false);

        Result<BillingProfileDto> result = await _sut.SaveBillingProfileAsync(org.Id, Profile(country: "NL"));

        result.Errors[0].Message.Should().Contain("enkel beschikbaar voor Belgische clubs");
    }

    [Test]
    public async Task SaveProfile_ViesUnavailable_SavesWithVatVerifiedNull()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(withProfile: false);
        _ctx.Vat.Setup(v => v.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(VatCheckResult.Unavailable);

        Result<BillingProfileDto> result = await _sut.SaveBillingProfileAsync(org.Id, Profile());

        result.IsSuccess.Should().BeTrue();
        result.Value!.VatVerified.Should().BeNull();
    }

    [Test]
    public async Task SaveProfile_ViesInvalid_IsRejected()
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync(withProfile: false);
        _ctx.Vat.Setup(v => v.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(VatCheckResult.Invalid);

        Result<BillingProfileDto> result = await _sut.SaveBillingProfileAsync(org.Id, Profile());

        result.Errors[0].Message.Should().Be("Dit btw-nummer is niet geldig volgens VIES.");
    }

    [Test]
    public async Task GetOverview_ListsMostRecentlyCreatedPaymentFirst()
    {
        // Eerst een overschrijving voor een LATERE periode, dan een betaling voor een vroegere:
        // de historiek (en de terugkeer-melding) moet de laatst aangemaakte eerst tonen.
        (Organization org, Subscription subscription) = await _ctx.SeedOrganizationAsync();
        _ctx.Db.SubscriptionPayments.Add(SubscriptionPayment.ForPeriod(subscription, SubscriptionPaymentKind.First,
            BillingPaymentMethod.Mollie, SubscriptionPlan.Set, BillingInterval.Monthly,
            new DateOnly(2026, 11, 5), new DateOnly(2026, 12, 4), 49m));
        await _ctx.Db.SaveChangesAsync();
        await Task.Delay(5);
        SubscriptionPayment newest = SubscriptionPayment.ForPeriod(subscription, SubscriptionPaymentKind.First,
            BillingPaymentMethod.Mollie, SubscriptionPlan.Game, BillingInterval.Monthly,
            BillingTestContext.Today, new DateOnly(2026, 11, 16), 25m);
        _ctx.Db.SubscriptionPayments.Add(newest);
        await _ctx.Db.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        BillingOverviewDto overview = (await _sut.GetOverviewAsync(org.Id)).Value!;

        overview.Payments[0].Id.Should().Be(newest.Id);
    }

    [TestCase(70, "Set")]
    [TestCase(0, "Game")]
    [TestCase(600, "Match")]
    public async Task GetOverview_SuggestsSmallestPlanCoveringActiveStudents(int students, string expected)
    {
        (Organization org, _) = await _ctx.SeedOrganizationAsync();
        _ctx.Students.Setup(s => s.CountAsync(org.Id, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(students);

        BillingOverviewDto overview = (await _sut.GetOverviewAsync(org.Id)).Value!;

        overview.ActiveStudentCount.Should().Be(students);
        overview.SuggestedPlan.Should().Be(expected);
        overview.Plans.Should().HaveCount(3);
        overview.CanCheckout.Should().BeTrue();
        overview.Profile.Should().NotBeNull();
    }
}
