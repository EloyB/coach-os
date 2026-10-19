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

    /// <summary>
    /// Niet-geannuleerde lessen waarvan <paramref name="trainerId"/> de trainer is, binnen een
    /// organisatie en datumbereik (inclusief), op datum + starttijd gesorteerd. Enkel "officieel
    /// ingeplande" lessen: losse lessen, of lessen van een reeks waarvan de planning door de admin
    /// bevestigd is (<c>AwaitingConfirmation</c> of <c>Scheduled</c>). Reeksen die nog in de
    /// inschrijf- of voorstelfase zitten, hebben nog geen vaste lessen voor de trainer.
    /// Inclusief reeks en club zodat de aanroeper geen extra queries nodig heeft.
    /// </summary>
    Task<List<Lesson>> GetByTrainerAndDateRangeAsync(
        Guid trainerId, Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// Niet-geannuleerde, officieel ingeplande lessen (zelfde regel als
    /// <see cref="GetByTrainerAndDateRangeAsync"/>) van alle trainers in de opgegeven clubs, binnen een
    /// datumbereik (inclusief), op datum + starttijd gesorteerd. De club van een reeksles is die van de
    /// reeks, die van een losse les is de eigen club. Voor het hoofdtrainer-dashboard.
    /// </summary>
    Task<List<Lesson>> GetByClubsAndDateRangeAsync(
        IReadOnlyList<Guid> clubIds, Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// Niet-geannuleerde, officieel ingeplande lessen (zelfde regel als
    /// <see cref="GetByTrainerAndDateRangeAsync"/>) van alle trainers en clubs van de organisatie, binnen een
    /// datumbereik (inclusief), op datum + starttijd gesorteerd. Voor het admin-dashboard.
    /// </summary>
    Task<List<Lesson>> GetByOrganizationAndDateRangeAsync(
        Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// Eén niet-geannuleerde, officieel ingeplande les van de organisatie, ongeacht de trainer. Inclusief reeks
    /// en club. Null als de les niet bestaat, geannuleerd of nog niet ingepland is. Voor het admin-dashboard.
    /// </summary>
    Task<Lesson?> GetOfficialByIdAsync(Guid lessonId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Eén niet-geannuleerde, officieel ingeplande les (zelfde regel als
    /// <see cref="GetByTrainerAndDateRangeAsync"/>) die de gebruiker mag zien: de eigen les van
    /// <paramref name="trainerId"/>, of een les in een van de <paramref name="headTrainerClubIds"/>.
    /// Inclusief reeks en club. Geeft null terug als de les niet bestaat, geannuleerd of nog niet ingepland is,
    /// of niet zichtbaar is voor deze gebruiker.
    /// </summary>
    Task<Lesson?> GetForTrainerAsync(
        Guid lessonId, Guid trainerId, IReadOnlyList<Guid> headTrainerClubIds, Guid organizationId,
        CancellationToken ct = default);

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
    /// than "Baan 2" at club B even within the same organization. A candidate lesson's own club is its
    /// series' TennisClubId when it belongs to a series, otherwise its own (nullable) TennisClubId for
    /// standalone lessons. A candidate with an unknown club (legacy standalone lesson, TennisClubId
    /// null) is treated as a conflict rather than silently allowed — safer than risking a double
    /// booking. When <paramref name="tennisClubId"/> itself is null, the check falls back to
    /// organization-wide (used when the lesson being checked has no known club either).
    /// CourtName is free text, so comparison is trimmed + case-insensitive.
    /// Optionally excludes a specific lesson (for updates).
    /// </summary>
    Task<Lesson?> FindCourtConflictAsync(
        Guid organizationId, string courtName, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        Guid? excludeLessonId = null, Guid? tennisClubId = null, CancellationToken ct = default);

    Task AddAsync(Lesson lesson, CancellationToken ct = default);
    Task DeleteAsync(Lesson lesson, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<Lesson> lessons, CancellationToken ct = default);

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
