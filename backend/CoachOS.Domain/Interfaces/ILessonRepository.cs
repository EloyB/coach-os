using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ILessonRepository
{
    Task<Lesson?> GetByIdAsync(Guid lessonId, Guid seriesId, Guid organizationId, CancellationToken ct = default);
    Task<Lesson?> GetByIdWithEnrollmentsAsync(Guid lessonId, Guid seriesId, Guid organizationId, CancellationToken ct = default);
    Task<int> CountBySeriesIdAsync(Guid seriesId, CancellationToken ct = default);

    /// <summary>
    /// Telt actieve (niet-gecancelde) lessen vanaf <paramref name="fromDate"/>
    /// waarvan de trainer de gegeven user is, binnen de organisatie.
    /// Gebruikt om de admin te waarschuwen vóór toggle-off van AdminsActAsTrainers.
    /// </summary>
    Task<int> CountUpcomingForTrainerAsync(
        Guid trainerId, Guid organizationId, DateOnly fromDate, CancellationToken ct = default);
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

    /// <summary>
    /// Checks if a court is already occupied by another lesson overlapping the given date + time range.
    /// Scoped to a single organization AND, when <paramref name="tennisClubId"/> is given, to that
    /// club: court names are free text chosen per club, so "Baan 2" at club A is a different court
    /// than "Baan 2" at club B even within the same organization. When <paramref name="tennisClubId"/>
    /// is null (e.g. a standalone lesson with no club), the check falls back to organization-wide.
    /// CourtName is free text, so comparison is trimmed + case-insensitive.
    /// Optionally excludes a specific lesson (for updates).
    /// </summary>
    Task<Lesson?> FindCourtConflictAsync(
        Guid organizationId, string courtName, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        Guid? excludeLessonId = null, Guid? tennisClubId = null, CancellationToken ct = default);

    Task AddAsync(Lesson lesson, CancellationToken ct = default);
    Task DeleteAsync(Lesson lesson, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<Lesson> lessons, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Lesson-by-id binnen een organisatie, ongeacht of deze tot een serie behoort.
    /// Tracking ingeschakeld zodat de service mutaties (cancel) kan doorvoeren.
    /// </summary>
    Task<Lesson?> GetByIdInOrganizationAsync(
        Guid lessonId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Alle losse lessen (LessonSerieId == null) van een organisatie, gesorteerd op datum + starttijd.
    /// </summary>
    Task<IReadOnlyList<Lesson>> GetStandaloneByOrganizationAsync(
        Guid organizationId, CancellationToken ct = default);
}
