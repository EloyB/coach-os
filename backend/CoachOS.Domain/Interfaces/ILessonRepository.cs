using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetByIdWithEnrollmentsAsync(Guid lessonId, Guid seriesId, Guid organizationId, CancellationToken ct = default);
    Task<int> CountBySeriesIdAsync(Guid seriesId, CancellationToken ct = default);
    Task<Dictionary<Guid, int>> GetLessonCountsBySeriesIdsAsync(IEnumerable<Guid> seriesIds, CancellationToken ct = default);
    Task AddAsync(Lesson lesson, CancellationToken ct = default);
    Task DeleteAsync(Lesson lesson, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<Lesson> lessons, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
