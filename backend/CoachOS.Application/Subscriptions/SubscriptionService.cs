using System.Globalization;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Billing;
using CoachOS.Application.Billing.DTOs;
using CoachOS.Application.Configuration;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Subscriptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Application.Subscriptions;

public class SubscriptionService(
    ISubscriptionRepository subscriptions,
    ISubscriptionPaymentRepository payments,
    IBillingProfileRepository profiles,
    IOrganizationRepository organizations,
    IBillingService billing,
    IBillingMollieClient mollie,
    IInvoicingService invoicing,
    IEmailService email,
    IUserLookupService users,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<AppOptions> appOptions,
    ILogger<SubscriptionService> logger) : ISubscriptionService
{
    private const int DealPaymentTermDays = 30;

    public async Task<Result<SubscriptionDto?>> GetForOrganizationAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: false, ct);
        if (subscription is null)
            return Result<SubscriptionDto?>.Ok(null);

        BillingProfile? profile = await profiles.GetByOrganizationAsync(organizationId, tracked: false, ct);
        IReadOnlyList<SubscriptionPayment> history = await payments.ListByOrganizationAsync(organizationId, ct);
        return Result<SubscriptionDto?>.Ok(ToDto(subscription, profile, history));
    }

    public IReadOnlyList<SubscriptionPlanOptionDto> GetPlanCatalog()
        => SubscriptionPlanCatalog.Plans.Values
            .OrderBy(p => (int)p.Plan)
            .Select(p => new SubscriptionPlanOptionDto(
                (int)p.Plan, p.Name, p.MonthlyPrice, p.YearlyPrice, p.StudentLimit))
            .ToList();

    public async Task<Result<SubscriptionDto>> AssignDealAsync(
        Guid organizationId, AssignDealRequest request, CancellationToken ct = default)
    {
        DateOnly today = timeProvider.GetBrusselsToday();
        DateOnly endDate = DateOnly.ParseExact(request.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (endDate <= today)
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.Validation, "De einddatum moet in de toekomst liggen."));

        Organization? organization = await organizations.GetByIdAsync(organizationId, tracked: false, ct);
        if (organization is null)
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.NotFound, "Organisatie niet gevonden."));

        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: true, ct);
        if (subscription is null)
        {
            subscription = new Subscription { Id = Guid.NewGuid(), OrganizationId = organizationId, StartDate = today };
            await subscriptions.AddAsync(subscription, ct);
        }

        if (subscription.Status == SubscriptionStatus.Active && !string.IsNullOrEmpty(subscription.MollieSubscriptionId))
        {
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.Conflict,
                "Deze club heeft een lopend Mollie-abonnement. Zeg dat eerst op."));
        }

        SubscriptionPlan plan = (SubscriptionPlan)request.Plan;
        if (request.Price > 0 && await profiles.GetByOrganizationAsync(organizationId, tracked: false, ct) is null)
        {
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.Validation,
                "Deze club heeft nog geen facturatiegegevens; nodig voor een deal met een prijs."));
        }

        subscription.Status = SubscriptionStatus.Active;
        subscription.IsDeal = true;
        subscription.Plan = plan;
        subscription.Interval = null;
        subscription.PaymentMethod = request.Price > 0 ? BillingPaymentMethod.BankTransfer : null;
        subscription.Price = request.Price;
        subscription.StudentLimit = SubscriptionPlanCatalog.Get(plan).StudentLimit;
        subscription.OverageMarginPercent = SubscriptionPlanCatalog.OverageMarginPercent;
        subscription.OveragePricePerStudent = SubscriptionPlanCatalog.OveragePricePerStudent;
        subscription.CurrentPeriodStart = today;
        subscription.CurrentPeriodEnd = endDate;
        subscription.CancelAt = null;

        if (request.Price > 0)
        {
            SubscriptionPayment payment = SubscriptionPayment.ForPeriod(subscription, SubscriptionPaymentKind.Deal,
                BillingPaymentMethod.BankTransfer, plan, BillingInterval.Yearly, today, endDate, request.Price);
            payment.DueDate = today.AddDays(DealPaymentTermDays);
            payment.InvoiceStatus = InvoiceStatus.Pending;
            await payments.AddAsync(payment, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("SuperAdmin kende deal {Plan} ({Price} excl. btw) tot {EndDate} toe aan {OrgId}",
            plan, request.Price, endDate, organizationId);
        return await ReloadAsync(organizationId, ct);
    }

    public async Task<Result<SubscriptionDto>> MarkPaymentPaidAsync(
        Guid organizationId, Guid paymentId, CancellationToken ct = default)
    {
        SubscriptionPayment? payment = await payments.GetByIdAsync(paymentId, organizationId, ct);
        if (payment is null)
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.NotFound, "Betaling niet gevonden."));
        if (payment.Method != BillingPaymentMethod.BankTransfer)
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.Validation, "Enkel overschrijvingen kan je manueel als betaald markeren."));
        if (payment.Status != SubscriptionPaymentStatus.Open)
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.Validation, "Deze betaling staat niet open."));

        payment.Status = SubscriptionPaymentStatus.Paid;
        payment.PaidAt = timeProvider.GetUtcNow().UtcDateTime;
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("SuperAdmin markeerde betaling {PaymentId} van {OrgId} als betaald", paymentId, organizationId);
        return await ReloadAsync(organizationId, ct);
    }

    public async Task<Result<SubscriptionDto>> CancelAsync(Guid organizationId, CancellationToken ct = default)
    {
        Result<BillingOverviewDto> cancelled = await billing.CancelAsync(organizationId, ct);
        return cancelled.IsSuccess
            ? await ReloadAsync(organizationId, ct)
            : Result<SubscriptionDto>.Fail(cancelled.Errors);
    }

    public async Task<Result> SendPaymentInvitationAsync(Guid organizationId, CancellationToken ct = default)
    {
        Organization? organization = await organizations.GetByIdAsync(organizationId, tracked: false, ct);
        if (organization is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Organisatie niet gevonden."));

        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: false, ct);
        if (subscription is not null && subscription.Status != SubscriptionStatus.Legacy)
            return Result.Fail(new Error(ErrorCodes.Validation, "Enkel bestaande klanten (legacy) krijgen een uitnodiging."));

        IReadOnlyList<string> admins = await users.GetActiveAdminEmailsAsync(organizationId, ct);
        if (admins.Count == 0)
            return Result.Fail(new Error(ErrorCodes.Validation, "Deze organisatie heeft geen actieve beheerder."));

        string subscribeUrl = $"{appOptions.Value.FrontendBaseUrl.TrimEnd('/')}/dashboard/settings/subscription";
        foreach (string admin in admins)
            await email.SendSubscriptionInvitationAsync(admin, organization.Name, subscribeUrl, ct);

        logger.LogInformation("Uitnodiging tot betalen verstuurd naar {Count} beheerder(s) van {OrgId}", admins.Count, organizationId);
        return Result.Ok();
    }

    private async Task<Result<SubscriptionDto>> ReloadAsync(Guid organizationId, CancellationToken ct)
    {
        Result<SubscriptionDto?> reloaded = await GetForOrganizationAsync(organizationId, ct);
        return reloaded.Value is SubscriptionDto dto
            ? Result<SubscriptionDto>.Ok(dto)
            : Result<SubscriptionDto>.Fail(new Error(ErrorCodes.NotFound, "Abonnement niet gevonden."));
    }

    private SubscriptionDto ToDto(Subscription s, BillingProfile? profile, IReadOnlyList<SubscriptionPayment> history) => new()
    {
        OrganizationId = s.OrganizationId,
        Status = s.Status.ToString(),
        Plan = s.Plan?.ToString(),
        Interval = s.Interval?.ToString(),
        PaymentMethod = s.PaymentMethod?.ToString(),
        StudentLimit = s.StudentLimit,
        Price = s.Price,
        PriceInclVat = BelgianVat.Gross(s.Price),
        StartDate = s.StartDate,
        TrialEndsAt = s.TrialEndsAt,
        CurrentPeriodStart = s.CurrentPeriodStart,
        CurrentPeriodEnd = s.CurrentPeriodEnd,
        CancelAt = s.CancelAt,
        IsDeal = s.IsDeal,
        MollieCustomerId = s.MollieCustomerId,
        MollieSubscriptionId = s.MollieSubscriptionId,
        BillingConfigured = mollie.IsConfigured,
        InvoicingConfigured = invoicing.IsConfigured,
        Profile = profile is null ? null : BillingService.ToDto(profile),
        Payments = history.Select(BillingService.ToDto).ToList(),
    };
}
