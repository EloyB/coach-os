using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Interfaces;

public interface IEnrollmentRepository
{
    Task<List<Enrollment>> GetBySeriesAsync(
        Guid lessonSeriesId, Guid organizationId, CancellationToken ct = default);

    Task<bool> IsDuplicateAsync(
        Guid lessonSeriesId, string studentEmail, CancellationToken ct = default);

    Task<int> CountActiveBySeriesAsync(Guid lessonSeriesId, CancellationToken ct = default);

    Task AddAsync(Enrollment enrollment, CancellationToken ct = default);

    Task AddFormResponseAsync(FormResponse response, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
