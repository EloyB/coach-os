using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken ct = default);
    Task<Payment?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
