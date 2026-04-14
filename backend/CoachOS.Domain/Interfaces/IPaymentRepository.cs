using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken ct = default);
    Task<Payment?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Dictionary<Guid, PaymentStatus>> GetLatestStatusByEnrollmentIdsAsync(
        IEnumerable<Guid> enrollmentIds, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
