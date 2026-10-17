using CoachOS.Application.Abstractions;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.Billing;

public class BillingService(
    ISubscriptionRepository subscriptions,
    ISubscriptionPaymentRepository payments,
    IBillingProfileRepository profiles,
    IOrganizationRepository organizations,
    IActiveStudentCounter students,
    IBillingMollieClient mollie,
    IVatNumberValidator vatValidator,
    IEmailService email,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<AppOptions> appOptions,
    IOptions<MollieBillingOptions> mollieOptions,
    IOptions<SuperAdminOptions> superAdminOptions,
    ILogger<BillingService> logger) : IBillingService
{
    /// <summary>Een openstaande eerste betaling blokkeert een nieuwe checkout zo lang.</summary>
    private static readonly TimeSpan OpenCheckoutWindow = TimeSpan.FromHours(1);
    private const int BankTransferTermDays = 30;

    private readonly string _frontendBaseUrl = appOptions.Value.FrontendBaseUrl.TrimEnd('/');

    public async Task<Result<BillingOverviewDto>> GetOverviewAsync(Guid organizationId, CancellationToken ct = default)
    {
        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: false, ct);
        BillingProfile? profile = await profiles.GetByOrganizationAsync(organizationId, tracked: false, ct);
        IReadOnlyList<SubscriptionPayment> history = await payments.ListByOrganizationAsync(organizationId, ct);
        DateOnly today = timeProvider.GetBrusselsToday();

        (DateOnly from, DateOnly to) =
            subscription?.CurrentPeriodStart is DateOnly start && subscription.CurrentPeriodEnd is DateOnly end && start <= today
                ? (start, end)
                : (today.AddDays(-365), today);
        int activeStudents = await students.CountAsync(organizationId, from, to, ct);

        return Result<BillingOverviewDto>.Ok(new BillingOverviewDto
        {
            Status = (subscription?.Status ?? SubscriptionStatus.Legacy).ToString(),
            Plan = subscription?.Plan?.ToString(),
            Interval = subscription?.Interval?.ToString(),
            PaymentMethod = subscription?.PaymentMethod?.ToString(),
            PriceExclVat = subscription?.Price ?? 0m,
            PriceInclVat = BelgianVat.Gross(subscription?.Price ?? 0m),
            StudentLimit = subscription?.StudentLimit,
            CurrentPeriodStart = subscription?.CurrentPeriodStart,
            CurrentPeriodEnd = subscription?.CurrentPeriodEnd,
            CancelAt = subscription?.CancelAt,
            TrialEndsAt = subscription?.TrialEndsAt,
            IsDeal = subscription?.IsDeal ?? false,
            ActiveStudentCount = activeStudents,
            SuggestedPlan = SubscriptionPlanCatalog.Suggest(activeStudents).Name,
            CanCheckout = subscription is null || !subscription.IsRunning(today),
            BillingConfigured = mollie.IsConfigured,
            OverageMarginPercent = SubscriptionPlanCatalog.OverageMarginPercent,
            OveragePricePerStudent = SubscriptionPlanCatalog.OveragePricePerStudent,
            Plans = SubscriptionPlanCatalog.Plans.Values
                .OrderBy(p => (int)p.Plan)
                .Select(p => new PlanOptionDto((int)p.Plan, p.Name, p.MonthlyPrice, p.YearlyPrice, p.StudentLimit))
                .ToList(),
            Profile = profile is null ? null : ToDto(profile),
            Payments = history.Select(ToDto).ToList(),
        });
    }

    public async Task<Result<BillingProfileDto>> SaveBillingProfileAsync(
        Guid organizationId, SaveBillingProfileRequest request, CancellationToken ct = default)
    {
        if (!string.Equals(request.Country, BillingProfile.BelgiumCountryCode, StringComparison.OrdinalIgnoreCase))
        {
            return Result<BillingProfileDto>.Fail(new Error(ErrorCodes.Validation,
                "CoachOS is voorlopig enkel beschikbaar voor Belgische clubs. Neem contact op via info@coach-os.be."));
        }

        string? vatNumber = string.IsNullOrWhiteSpace(request.VatNumber) ? null : BelgianVat.Normalize(request.VatNumber);
        bool? vatVerified = null;
        if (vatNumber is not null)
        {
            VatCheckResult check = await vatValidator.CheckAsync(vatNumber, ct);
            if (check == VatCheckResult.Invalid)
                return Result<BillingProfileDto>.Fail(new Error(ErrorCodes.Validation, "Dit btw-nummer is niet geldig volgens VIES."));
            vatVerified = check == VatCheckResult.Valid ? true : null;
        }

        BillingProfile? profile = await profiles.GetByOrganizationAsync(organizationId, tracked: true, ct);
        if (profile is null)
        {
            profile = new BillingProfile { Id = Guid.NewGuid(), OrganizationId = organizationId };
            await profiles.AddAsync(profile, ct);
        }

        profile.Name = request.Name.Trim();
        profile.Street = request.Street.Trim();
        profile.PostalCode = request.PostalCode.Trim();
        profile.City = request.City.Trim();
        profile.Country = BillingProfile.BelgiumCountryCode;
        profile.InvoiceEmail = request.InvoiceEmail.Trim();
        profile.VatNumber = vatNumber;
        profile.VatVerified = vatVerified;

        await unitOfWork.SaveChangesAsync(ct);
        return Result<BillingProfileDto>.Ok(ToDto(profile));
    }

    public async Task<Result<CheckoutResultDto>> StartCheckoutAsync(
        Guid organizationId, CheckoutRequest request, CancellationToken ct = default)
    {
        SubscriptionPlan plan = (SubscriptionPlan)request.Plan;
        BillingInterval interval = (BillingInterval)request.Interval;
        BillingPaymentMethod method = (BillingPaymentMethod)request.Method;
        DateOnly today = timeProvider.GetBrusselsToday();

        if (method == BillingPaymentMethod.BankTransfer && interval != BillingInterval.Yearly)
        {
            return Result<CheckoutResultDto>.Fail(new Error(ErrorCodes.Validation,
                "Betalen per overschrijving kan enkel met een jaarabonnement."));
        }

        Organization? organization = await organizations.GetByIdAsync(organizationId, tracked: false, ct);
        if (organization is null)
            return Result<CheckoutResultDto>.Fail(new Error(ErrorCodes.NotFound, "Organisatie niet gevonden."));

        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: true, ct);
        if (subscription is null)
        {
            subscription = new Subscription { Id = Guid.NewGuid(), OrganizationId = organizationId, StartDate = today };
            await subscriptions.AddAsync(subscription, ct);
        }

        if (subscription.IsRunning(today))
            return Result<CheckoutResultDto>.Fail(new Error(ErrorCodes.Conflict, "Er loopt al een abonnement."));

        BillingProfile? profile = await profiles.GetByOrganizationAsync(organizationId, tracked: false, ct);
        if (profile is null)
            return Result<CheckoutResultDto>.Fail(new Error(ErrorCodes.Validation, "Vul eerst je facturatiegegevens in."));

        SubscriptionPayment? open = await payments.GetLatestOpenFirstAsync(organizationId, ct);
        if (open is not null && open.CreatedAt > timeProvider.GetUtcNow().UtcDateTime - OpenCheckoutWindow)
        {
            return Result<CheckoutResultDto>.Fail(new Error(ErrorCodes.Conflict,
                "Er staat al een betaling open. Rond die eerst af of probeer het binnen een uur opnieuw."));
        }

        decimal priceExclVat = SubscriptionPlanCatalog.PriceFor(plan, interval);
        DateOnly periodStart = BillingPeriods.FirstPeriodStart(subscription, today);
        DateOnly periodEnd = BillingPeriods.PeriodEnd(periodStart, interval);

        return method == BillingPaymentMethod.BankTransfer
            ? await StartBankTransferAsync(organization, subscription, plan, interval, periodStart, periodEnd, priceExclVat, today, ct)
            : await StartMollieAsync(organization, profile, subscription, plan, interval, periodStart, periodEnd, priceExclVat, ct);
    }

    public async Task<Result<BillingOverviewDto>> CancelAsync(Guid organizationId, CancellationToken ct = default)
    {
        DateOnly today = timeProvider.GetBrusselsToday();
        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: true, ct);
        if (subscription is null || subscription.Status != SubscriptionStatus.Active)
            return Result<BillingOverviewDto>.Fail(new Error(ErrorCodes.Validation, "Er is geen lopend abonnement om op te zeggen."));

        if (!string.IsNullOrEmpty(subscription.MollieSubscriptionId) && !string.IsNullOrEmpty(subscription.MollieCustomerId))
        {
            Result stopped = await mollie.CancelSubscriptionAsync(subscription.MollieCustomerId, subscription.MollieSubscriptionId, ct);
            if (!stopped.IsSuccess)
                return Result<BillingOverviewDto>.Fail(stopped.Errors);
        }

        bool paidPeriodStarted = subscription.CurrentPeriodStart is not DateOnly start || today >= start;
        if (subscription.IsDeal)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelAt = today;
            subscription.CurrentPeriodEnd = today;
        }
        else if (paidPeriodStarted)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelAt = subscription.CurrentPeriodEnd;
        }
        else
        {
            Result undone = await UndoPrepaidPeriodAsync(subscription, ct);
            if (!undone.IsSuccess)
                return Result<BillingOverviewDto>.Fail(undone.Errors);
            subscription.LockAfterTrialCancellation(today);
        }

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Abonnement van {OrgId} opgezegd (status {Status})", organizationId, subscription.Status);
        return await GetOverviewAsync(organizationId, ct);
    }

    /// <summary>
    /// Betaald tijdens de proef en opgezegd voor de periode begon: Mollie-betaling terugstorten,
    /// openstaande factuur schrappen. Een al gemaakte factuur vraagt een creditnota (superadmin).
    /// </summary>
    private async Task<Result> UndoPrepaidPeriodAsync(Subscription subscription, CancellationToken ct)
    {
        IReadOnlyList<SubscriptionPayment> history = await payments.ListBySubscriptionAsync(subscription.Id, ct);
        List<string> invoiceNumbers = [];
        bool manualRefund = false;

        foreach (SubscriptionPayment snapshot in history.Where(p => p.PeriodStart == subscription.CurrentPeriodStart))
        {
            SubscriptionPayment? payment = await payments.GetByIdAsync(snapshot.Id, subscription.OrganizationId, ct);
            if (payment is null)
                continue;

            if (payment.Status == SubscriptionPaymentStatus.Paid && payment.Method == BillingPaymentMethod.Mollie
                && payment.MolliePaymentId is string molliePaymentId)
            {
                Result refund = await mollie.RefundPaymentAsync(molliePaymentId, payment.AmountInclVat,
                    "Opzegging CoachOS-abonnement voor de start", ct);
                if (!refund.IsSuccess)
                    return refund;
                payment.Status = SubscriptionPaymentStatus.Refunded;
            }
            else if (payment.Status == SubscriptionPaymentStatus.Paid)
            {
                manualRefund = true;
            }
            else if (payment.Status == SubscriptionPaymentStatus.Open)
            {
                payment.Status = SubscriptionPaymentStatus.Canceled;
            }

            if (payment.InvoiceStatus == InvoiceStatus.Created)
                invoiceNumbers.Add(payment.InvoiceNumber ?? payment.Id.ToString());
            else
                payment.InvoiceStatus = InvoiceStatus.NotNeeded;
        }

        Organization? organization = await organizations.GetByIdAsync(subscription.OrganizationId, tracked: false, ct);
        string name = organization?.Name ?? subscription.OrganizationId.ToString();
        string body = $"{name} zegde het abonnement op voor de betaalde periode begon. Het account is vergrendeld tot de club een plan kiest.";
        if (invoiceNumbers.Count > 0)
            body += $" Maak een creditnota in Billit voor factuur {string.Join(", ", invoiceNumbers)}.";
        if (manualRefund)
            body += " Er was al per overschrijving betaald: stort dat bedrag manueel terug.";
        await NotifySuperAdminAsync("Opzegging tijdens de proef", body, ct);

        return Result.Ok();
    }

    private async Task NotifySuperAdminAsync(string subject, string body, CancellationToken ct)
    {
        string to = superAdminOptions.Value.Email;
        if (string.IsNullOrWhiteSpace(to))
            return;
        try
        {
            await email.SendBillingSuperAdminNoticeAsync(to, subject, body, $"{_frontendBaseUrl}/super-admin", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Superadmin-melding '{Subject}' versturen mislukt", subject);
        }
    }

    private async Task<Result<CheckoutResultDto>> StartBankTransferAsync(
        Organization organization, Subscription subscription, SubscriptionPlan plan, BillingInterval interval,
        DateOnly periodStart, DateOnly periodEnd, decimal priceExclVat, DateOnly today, CancellationToken ct)
    {
        subscription.ActivatePaidPlan(plan, interval, BillingPaymentMethod.BankTransfer, periodStart, periodEnd, priceExclVat);

        SubscriptionPayment payment = SubscriptionPayment.ForPeriod(
            subscription, SubscriptionPaymentKind.BankTransfer, BillingPaymentMethod.BankTransfer,
            plan, interval, periodStart, periodEnd, priceExclVat);
        payment.DueDate = today.AddDays(BankTransferTermDays);
        payment.InvoiceStatus = InvoiceStatus.Pending;
        await payments.AddAsync(payment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Organisatie {OrgId} sloot {Plan} ({Interval}) af per overschrijving", organization.Id, plan, interval);
        await TrySendStartedMailAsync(organization, payment, ct);
        return Result<CheckoutResultDto>.Ok(new CheckoutResultDto(null));
    }

    private async Task<Result<CheckoutResultDto>> StartMollieAsync(
        Organization organization, BillingProfile profile, Subscription subscription, SubscriptionPlan plan,
        BillingInterval interval, DateOnly periodStart, DateOnly periodEnd, decimal priceExclVat, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(subscription.MollieCustomerId))
        {
            Result<string> customer = await mollie.CreateCustomerAsync(profile.Name, profile.InvoiceEmail, ct);
            if (!customer.IsSuccess)
                return Result<CheckoutResultDto>.Fail(customer.Errors);
            subscription.MollieCustomerId = customer.Value;
        }

        SubscriptionPayment payment = SubscriptionPayment.ForPeriod(
            subscription, SubscriptionPaymentKind.First, BillingPaymentMethod.Mollie,
            plan, interval, periodStart, periodEnd, priceExclVat);
        await payments.AddAsync(payment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        Result<BillingPaymentResult> created = await mollie.CreateFirstPaymentAsync(new BillingPaymentRequest(
            Amount: payment.AmountInclVat,
            Currency: "EUR",
            Description: Description(plan, interval, organization.Name),
            RedirectUrl: $"{_frontendBaseUrl}/dashboard/settings/subscription?return=1",
            WebhookUrl: WebhookUrl(),
            CustomerId: subscription.MollieCustomerId,
            Metadata: new Dictionary<string, string>
            {
                ["subscriptionPaymentId"] = payment.Id.ToString(),
                ["organizationId"] = organization.Id.ToString(),
            }), ct);

        if (!created.IsSuccess)
        {
            payment.Status = SubscriptionPaymentStatus.Failed;
            await unitOfWork.SaveChangesAsync(ct);
            return Result<CheckoutResultDto>.Fail(created.Errors);
        }

        payment.MolliePaymentId = created.Value!.Id;
        payment.CheckoutUrl = created.Value.CheckoutUrl;
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Organisatie {OrgId} startte Mollie-checkout {PaymentId} voor {Plan} ({Interval})",
            organization.Id, payment.MolliePaymentId, plan, interval);
        return Result<CheckoutResultDto>.Ok(new CheckoutResultDto(created.Value.CheckoutUrl));
    }

    private async Task TrySendStartedMailAsync(Organization organization, SubscriptionPayment payment, CancellationToken ct)
    {
        try
        {
            await email.SendSubscriptionStartedAsync(
                organization.Email, organization.Name, payment.Plan.ToString(), IntervalLabel(payment.Interval),
                payment.AmountInclVat, payment.PeriodStart, payment.Method == BillingPaymentMethod.BankTransfer,
                payment.DueDate, $"{_frontendBaseUrl}/dashboard/settings/subscription", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Bevestigingsmail abonnement mislukt voor organisatie {OrgId}", organization.Id);
        }
    }

    private string? WebhookUrl()
    {
        string baseUrl = mollieOptions.Value.WebhookBaseUrl;
        return string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl.TrimEnd('/')}/api/webhooks/mollie-billing";
    }

    internal static string IntervalLabel(BillingInterval interval) => interval == BillingInterval.Yearly ? "jaar" : "maand";

    internal static string Description(SubscriptionPlan plan, BillingInterval interval, string organizationName)
        => $"CoachOS {plan} ({IntervalLabel(interval)}) - {organizationName}";

    internal static BillingProfileDto ToDto(BillingProfile p)
        => new(p.Name, p.Street, p.PostalCode, p.City, p.Country, p.InvoiceEmail, p.VatNumber, p.VatVerified);

    internal static SubscriptionPaymentDto ToDto(SubscriptionPayment p) => new(
        p.Id, p.Kind.ToString(), p.Method.ToString(), p.Status.ToString(), p.Plan.ToString(), p.Interval.ToString(),
        p.PeriodStart, p.PeriodEnd, p.AmountExclVat, p.AmountInclVat, p.PaidAt, p.DueDate,
        p.Status == SubscriptionPaymentStatus.Open ? p.CheckoutUrl : null,
        p.InvoiceStatus.ToString(), p.InvoiceNumber, p.InvoicePdfUrl, p.InvoiceLastError);
}
