using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ILessonSeriesRepository
{
    Task<LessonSeries?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<LessonSeries?> GetByIdWithEnrollmentsAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<LessonSeries>> GetByOrganizationAsync(Guid organizationId, Guid? trainerId = null, CancellationToken ct = default);
    Task AddAsync(LessonSeries series, CancellationToken ct = default);
    Task UpdateAsync(LessonSeries series, CancellationToken ct = default);
    Task DeleteAsync(LessonSeries series, CancellationToken ct = default);
    Task<LessonSeries?> GetByIdPublicAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<bool> AnyByTennisClubAsync(Guid tennisClubId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
