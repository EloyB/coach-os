using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetByIdAsync(Guid lessonId, Guid seriesId, Guid organizationId, CancellationToken ct = default);
    Task<Lesson?> GetByIdWithEnrollmentsAsync(Guid lessonId, Guid seriesId, Guid organizationId, CancellationToken ct = default);
    Task<int> CountBySeriesIdAsync(Guid seriesId, CancellationToken ct = default);
    Task<Dictionary<Guid, int>> GetLessonCountsBySeriesIdsAsync(IEnumerable<Guid> seriesIds, CancellationToken ct = default);
    Task<List<Lesson>> GetUpcomingByOrganizationAsync(Guid organizationId, DateOnly fromDate, int limit, CancellationToken ct = default);
    Task<int> CountByOrganizationAndDateRangeAsync(Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Dictionary<string, int>> CountByOrganizationWeeksAsync(Guid organizationId, int weeks, CancellationToken ct = default);
    /// <summary>
    /// Checks if a trainer has any lesson overlapping the given date + time range.
    /// Cross-organization: a trainer cannot be in two places at once.
    /// Optionally excludes a specific lesson (for updates).
    /// </summary>
    Task<Lesson?> FindTrainerConflictAsync(
        Guid trainerId, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        Guid? excludeLessonId = null, CancellationToken ct = default);

    Task AddAsync(Lesson lesson, CancellationToken ct = default);
    Task DeleteAsync(Lesson lesson, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<Lesson> lessons, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
