using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IEnrollmentFormRepository
{
    Task<EnrollmentForm?> GetBySeriesIdAsync(Guid lessonSeriesId, CancellationToken ct = default);

    Task<EnrollmentForm?> GetBySeriesIdWithFieldsAsync(Guid lessonSeriesId, CancellationToken ct = default);

    Task<EnrollmentForm?> GetBySeriesIdReadOnlyAsync(Guid lessonSeriesId, CancellationToken ct = default);

    Task AddAsync(EnrollmentForm form, CancellationToken ct = default);

    void RemoveField(FormField field);

    Task SaveChangesAsync(CancellationToken ct = default);
}
