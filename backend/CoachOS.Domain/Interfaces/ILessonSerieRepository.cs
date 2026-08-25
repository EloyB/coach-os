using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ILessonSerieRepository
{
    Task<LessonSerie?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<LessonSerie?> GetByIdWithEnrollmentsAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<LessonSerie>> GetByOrganizationAsync(Guid organizationId, Guid? trainerId, IReadOnlyList<Guid> headTrainerClubIds, CancellationToken ct = default);
    Task AddAsync(LessonSerie series, CancellationToken ct = default);
    Task UpdateAsync(LessonSerie series, CancellationToken ct = default);
    Task DeleteAsync(LessonSerie series, CancellationToken ct = default);
    Task DeleteWeeklyTemplateRangeAsync(IEnumerable<WeeklyTemplateEntry> entries, CancellationToken ct = default);
    Task<LessonSerie?> GetByIdPublicAsync(Guid id, CancellationToken ct = default);
    Task<LessonSerie?> GetByIdPublicForTimeSlotsAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<bool> AnyByTennisClubAsync(Guid tennisClubId, CancellationToken ct = default);
    Task<bool> AnyByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
