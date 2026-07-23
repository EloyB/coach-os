using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ITrainerAvailabilityRepository
{
    /// <summary>Alle actieve beschikbaarheden van de organisatie, incl. TennisClub navigatie.</summary>
    Task<IReadOnlyList<TrainerAvailability>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Tracked fetch voor soft delete. Enkel actieve records.</summary>
    Task<TrainerAvailability?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// True wanneer de trainer op deze weekdag al een actieve beschikbaarheid heeft
    /// die overlapt met [startTime, endTime) - over alle clubs heen (een trainer kan
    /// niet op twee plekken tegelijk staan).
    /// </summary>
    Task<bool> HasOverlapAsync(Guid trainerId, Guid organizationId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, CancellationToken ct = default);

    /// <summary>
    /// Alle actieve beschikbaarheden waarvan het tijdvak [startTime, endTime] VOLLEDIG
    /// omvat wordt. Een beschikbaarheid met TennisClubId == null geldt voor elke club
    /// en matcht dus altijd.
    /// </summary>
    Task<IReadOnlyList<TrainerAvailability>> GetAvailableTrainersAsync(
        Guid organizationId,
        Guid? tennisClubId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken ct = default);

    Task AddAsync(TrainerAvailability availability, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
