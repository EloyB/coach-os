using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IEnrollmentGroupRepository
{
    Task<List<EnrollmentGroup>> GetBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    Task<EnrollmentGroup?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default);

    Task<int> CountBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    Task AddAsync(EnrollmentGroup group, CancellationToken ct = default);

    void Delete(EnrollmentGroup group);

    Task SaveChangesAsync(CancellationToken ct = default);
}
