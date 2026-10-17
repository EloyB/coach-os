using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class SubscriptionPaymentRepository(ApplicationDbContext context) : ISubscriptionPaymentRepository
{
    public async Task AddAsync(SubscriptionPayment payment, CancellationToken ct = default)
        => await context.SubscriptionPayments.AddAsync(payment, ct);

    public async Task<SubscriptionPayment?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
        => await context.SubscriptionPayments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId, ct);

    public async Task<SubscriptionPayment?> GetByMolliePaymentIdAsync(string molliePaymentId, CancellationToken ct = default)
        => await context.SubscriptionPayments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.MolliePaymentId == molliePaymentId, ct);

    public async Task<IReadOnlyList<SubscriptionPayment>> ListByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await context.SubscriptionPayments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<SubscriptionPayment?> GetLatestOpenFirstAsync(Guid organizationId, CancellationToken ct = default)
        => await context.SubscriptionPayments
            .IgnoreQueryFilters()
            .Where(p => p.OrganizationId == organizationId
                        && p.Kind == SubscriptionPaymentKind.First
                        && p.Status == SubscriptionPaymentStatus.Open)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<DateOnly?> GetLatestBilledPeriodEndAsync(Guid subscriptionId, CancellationToken ct = default)
        => await context.SubscriptionPayments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.SubscriptionId == subscriptionId
                        && (p.Status == SubscriptionPaymentStatus.Paid
                            || (p.Kind == SubscriptionPaymentKind.Recurring
                                && (p.Status == SubscriptionPaymentStatus.Open || p.Status == SubscriptionPaymentStatus.Failed))))
            .MaxAsync(p => (DateOnly?)p.PeriodEnd, ct);

    public async Task<IReadOnlyList<SubscriptionPayment>> ListInvoiceDueAsync(
        DateTime nowUtc, int maxAttempts, int take, CancellationToken ct = default)
        => await context.SubscriptionPayments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => (p.InvoiceStatus == InvoiceStatus.Pending || p.InvoiceStatus == InvoiceStatus.Failed)
                        && p.InvoiceAttempts < maxAttempts
                        && (p.InvoiceNextAttemptAt == null || p.InvoiceNextAttemptAt <= nowUtc))
            .OrderBy(p => p.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SubscriptionPayment>> ListBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default)
        => await context.SubscriptionPayments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.SubscriptionId == subscriptionId)
            .ToListAsync(ct);
}
