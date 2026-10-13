using System.Globalization;
using CoachOS.Application.Abstractions;
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
    IOrganizationRepository organizations,
    IBillingMollieClient billing,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<AppOptions> appOptions,
    ILogger<SubscriptionService> logger) : ISubscriptionService
{
    private readonly AppOptions _app = appOptions.Value;

    public async Task<Result<SubscriptionDto?>> GetForOrganizationAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: false, ct);
        return Result<SubscriptionDto?>.Ok(subscription is null ? null : ToDto(subscription));
    }

    public async Task<Result<SubscriptionDto>> AssignPlanAsync(
        Guid organizationId, AssignSubscriptionRequest request, CancellationToken ct = default)
    {
        Organization? org = await organizations.GetByIdAsync(organizationId, tracked: false, ct);
        if (org is null)
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.NotFound, "Organisatie niet gevonden."));

        var plan = (SubscriptionPlan)request.Plan;
        var interval = (BillingInterval)request.Interval;
        decimal price = SubscriptionPlanCatalog.PriceFor(plan, interval);
        DateOnly startDate = ParseStartDate(request.StartDate);

        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: true, ct);
        if (subscription is null)
        {
            subscription = new Subscription
            {
                OrganizationId = organizationId,
                Plan = plan,
                Interval = interval,
                StartDate = startDate,
                Price = price,
                IsActive = true,
            };
            await subscriptions.AddAsync(subscription, ct);
        }
        else
        {
            subscription.Plan = plan;
            subscription.Interval = interval;
            subscription.StartDate = startDate;
            subscription.Price = price;
        }

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation(
            "SuperAdmin kende plan {Plan} ({Interval}) toe aan organisatie {OrgId}.",
            plan, interval, organizationId);
        return Result<SubscriptionDto>.Ok(ToDto(subscription));
    }

    public async Task<Result<SubscriptionDto>> SetActiveAsync(
        Guid organizationId, bool isActive, CancellationToken ct = default)
    {
        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: true, ct);
        if (subscription is null)
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.NotFound, "Geen abonnement gevonden."));

        subscription.IsActive = isActive;
        await unitOfWork.SaveChangesAsync(ct);
        return Result<SubscriptionDto>.Ok(ToDto(subscription));
    }

    public async Task<Result<SubscriptionDto>> CreatePaymentLinkAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: true, ct);
        if (subscription is null)
            return Result<SubscriptionDto>.Fail(new Error(
                ErrorCodes.Validation, "Ken eerst een plan toe voor je een betaallink maakt."));

        Organization? org = await organizations.GetByIdAsync(organizationId, tracked: false, ct);
        if (org is null)
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.NotFound, "Organisatie niet gevonden."));

        if (string.IsNullOrEmpty(subscription.MollieCustomerId))
        {
            Result<string> customer = await billing.CreateCustomerAsync(org.Name, org.Email, ct);
            if (!customer.IsSuccess)
                return Result<SubscriptionDto>.Fail(customer.Errors);
            subscription.MollieCustomerId = customer.Value;
        }

        SubscriptionPlanInfo info = SubscriptionPlanCatalog.Get(subscription.Plan);
        string intervalLabel = subscription.Interval == BillingInterval.Yearly ? "jaar" : "maand";
        var request = new BillingPaymentRequest(
            Amount: subscription.Price,
            Currency: "EUR",
            Description: $"CoachOS {info.Name}-abonnement ({intervalLabel}) - {org.Name}",
            RedirectUrl: $"{_app.FrontendBaseUrl.TrimEnd('/')}/super-admin/organizations/{organizationId}",
            WebhookUrl: null,
            CustomerId: subscription.MollieCustomerId,
            Metadata: new Dictionary<string, string>
            {
                ["organizationId"] = organizationId.ToString(),
                ["plan"] = info.Name,
            });

        Result<BillingPaymentResult> payment = await billing.CreatePaymentAsync(request, ct);
        if (!payment.IsSuccess)
            return Result<SubscriptionDto>.Fail(payment.Errors);

        subscription.LastPaymentId = payment.Value!.Id;
        subscription.LastCheckoutUrl = payment.Value.CheckoutUrl;
        subscription.LastPaymentStatus = payment.Value.Status;
        subscription.LastPaidAt = null;

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation(
            "SuperAdmin genereerde billing-betaallink {PaymentId} voor organisatie {OrgId} (configured={Configured}).",
            subscription.LastPaymentId, organizationId, billing.IsConfigured);
        return Result<SubscriptionDto>.Ok(ToDto(subscription));
    }

    public async Task<Result<SubscriptionDto>> RefreshPaymentStatusAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: true, ct);
        if (subscription is null)
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.NotFound, "Geen abonnement gevonden."));

        if (string.IsNullOrEmpty(subscription.LastPaymentId))
            return Result<SubscriptionDto>.Fail(new Error(
                ErrorCodes.Validation, "Er is nog geen betaallink om te verversen."));

        Result<BillingPaymentSnapshot> snapshot = await billing.GetPaymentAsync(subscription.LastPaymentId, ct);
        if (!snapshot.IsSuccess)
            return Result<SubscriptionDto>.Fail(snapshot.Errors);

        subscription.LastPaymentStatus = snapshot.Value!.Status;
        if (snapshot.Value.Status == "paid")
        {
            subscription.LastPaidAt = snapshot.Value.PaidAt ?? timeProvider.GetUtcNow().UtcDateTime;
            subscription.IsActive = true;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result<SubscriptionDto>.Ok(ToDto(subscription));
    }

    public async Task<Result<SubscriptionDto>> MarkPaidAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: true, ct);
        if (subscription is null)
            return Result<SubscriptionDto>.Fail(new Error(ErrorCodes.NotFound, "Geen abonnement gevonden."));

        subscription.LastPaymentStatus = "paid";
        subscription.LastPaidAt ??= timeProvider.GetUtcNow().UtcDateTime;
        subscription.IsActive = true;

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("SuperAdmin markeerde abonnement handmatig als betaald voor organisatie {OrgId}.", organizationId);
        return Result<SubscriptionDto>.Ok(ToDto(subscription));
    }

    public IReadOnlyList<SubscriptionPlanOptionDto> GetPlanCatalog()
        => SubscriptionPlanCatalog.Plans.Values
            .OrderBy(p => (int)p.Plan)
            .Select(p => new SubscriptionPlanOptionDto(
                (int)p.Plan, p.Name, p.MonthlyPrice, p.YearlyPrice, p.StudentLimit))
            .ToList();

    private DateOnly ParseStartDate(string? value)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
            ? parsed
            : timeProvider.GetBrusselsToday();

    private SubscriptionDto ToDto(Subscription s)
    {
        SubscriptionPlanInfo info = SubscriptionPlanCatalog.Get(s.Plan);
        return new SubscriptionDto
        {
            OrganizationId = s.OrganizationId,
            Plan = s.Plan.ToString(),
            Interval = s.Interval.ToString(),
            StudentLimit = info.StudentLimit,
            Price = s.Price,
            StartDate = s.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            EndDate = s.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IsActive = s.IsActive,
            MollieCustomerId = s.MollieCustomerId,
            LastPaymentId = s.LastPaymentId,
            LastCheckoutUrl = s.LastCheckoutUrl,
            LastPaymentStatus = s.LastPaymentStatus,
            LastPaidAt = s.LastPaidAt?.ToString("o", CultureInfo.InvariantCulture),
            BillingConfigured = billing.IsConfigured,
        };
    }
}
