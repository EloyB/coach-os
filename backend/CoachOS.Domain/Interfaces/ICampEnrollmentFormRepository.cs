using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ICampEnrollmentFormRepository
{
    Task<CampEnrollmentForm?> GetByCampIdWithFieldsAsync(Guid campId, CancellationToken ct = default);
    Task<CampEnrollmentForm?> GetByCampIdReadOnlyAsync(Guid campId, CancellationToken ct = default);
    Task AddAsync(CampEnrollmentForm form, CancellationToken ct = default);
    void RemoveField(CampFormField field);
    Task SaveChangesAsync(CancellationToken ct = default);
}
