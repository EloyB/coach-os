using System.Globalization;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Subscriptions;

namespace CoachOS.Application.Subscriptions;

public class SubscriptionService(
    ISubscriptionRepository subscriptions,
    IBillingMollieClient billing) : ISubscriptionService
{
    public async Task<Result<SubscriptionDto?>> GetForOrganizationAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        Subscription? subscription = await subscriptions.GetByOrganizationAsync(organizationId, tracked: false, ct);
        return Result<SubscriptionDto?>.Ok(subscription is null ? null : ToDto(subscription));
    }

    public IReadOnlyList<SubscriptionPlanOptionDto> GetPlanCatalog()
        => SubscriptionPlanCatalog.Plans.Values
            .OrderBy(p => (int)p.Plan)
            .Select(p => new SubscriptionPlanOptionDto(
                (int)p.Plan, p.Name, p.MonthlyPrice, p.YearlyPrice, p.StudentLimit))
            .ToList();

    private SubscriptionDto ToDto(Subscription s)
    {
        int studentLimit = s.Plan is SubscriptionPlan plan ? SubscriptionPlanCatalog.Get(plan).StudentLimit : 0;
        return new SubscriptionDto
        {
            OrganizationId = s.OrganizationId,
            Status = s.Status.ToString(),
            Plan = s.Plan?.ToString(),
            Interval = s.Interval?.ToString(),
            StudentLimit = s.StudentLimit ?? studentLimit,
            Price = s.Price,
            StartDate = s.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            EndDate = s.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            MollieCustomerId = s.MollieCustomerId,
            BillingConfigured = billing.IsConfigured,
        };
    }
}
