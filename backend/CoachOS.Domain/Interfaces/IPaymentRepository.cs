using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken ct = default);
    Task<Payment?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Dictionary<Guid, PaymentStatus>> GetLatestStatusByEnrollmentIdsAsync(
        IEnumerable<Guid> enrollmentIds, CancellationToken ct = default);

    /// <summary>
    /// Lookup voor de Mollie webhook. Heeft GEEN organizationId-filter omdat de
    /// webhook anoniem is en alleen de Mollie payment ID kent — de service
    /// gebruikt de gevonden <see cref="Payment.OrganizationId"/> voor verdere
    /// auth checks en token resolution.
    /// </summary>
    Task<Payment?> GetByMolliePaymentIdAsync(string molliePaymentId, CancellationToken ct = default);

    /// <summary>
    /// Meest recente payment voor een enrollment (op CreatedAt desc).
    /// Gebruikt door de publieke thank-you-page status polling.
    /// </summary>
    Task<Payment?> GetLatestByEnrollmentIdAsync(Guid enrollmentId, CancellationToken ct = default);

    /// <summary>
    /// Meest recente payment voor een kamp-inschrijving (op CreatedAt desc).
    /// Gebruikt door de publieke thank-you-page status polling.
    /// </summary>
    Task<Payment?> GetLatestByCampEnrollmentIdAsync(Guid campEnrollmentId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
