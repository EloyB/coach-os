using CoachOS.Domain.Entities;
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

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}
