using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class PaymentRepository(ApplicationDbContext context) : IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken ct = default)
        => await context.Payments.AddAsync(payment, ct);

    public async Task<Payment?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
        => await context.Payments.FirstOrDefaultAsync(
            p => p.Id == id && p.OrganizationId == organizationId, ct);

    public async Task<Dictionary<Guid, PaymentStatus>> GetLatestStatusByEnrollmentIdsAsync(
        IEnumerable<Guid> enrollmentIds, CancellationToken ct = default)
    {
        var ids = enrollmentIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, PaymentStatus>();

        var rows = await context.Payments
            .AsNoTracking()
            .Where(p => ids.Contains(p.EnrollmentId))
            .Select(p => new { p.EnrollmentId, p.Status, p.CreatedAt })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.EnrollmentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.CreatedAt).First().Status);
    }

    public async Task<Payment?> GetByMolliePaymentIdAsync(
        string molliePaymentId, CancellationToken ct = default)
    {
        // Webhook heeft geen tenant-context; tenant query filter is "loose" en
        // staat anonieme reads toe wanneer er geen tenant is ingesteld (zie
        // ApplicationDbContext.ApplyTenantFilters).
        return await context.Payments
            .FirstOrDefaultAsync(p => p.MolliePaymentId == molliePaymentId, ct);
    }

    public async Task<Payment?> GetLatestByEnrollmentIdAsync(
        Guid enrollmentId, CancellationToken ct = default)
    {
        return await context.Payments
            .AsNoTracking()
            .Where(p => p.EnrollmentId == enrollmentId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}
