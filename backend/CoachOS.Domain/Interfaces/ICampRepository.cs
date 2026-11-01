using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ICampRepository
{
    /// <summary>Alle actieve kampen van de org, met dagen (voor lijst-telling).</summary>
    Task<IReadOnlyList<Camp>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Alle kampdagen van de organisatie binnen een datumbereik (inclusief), enkel actieve kampen, met kamp en
    /// club. Op datum + starttijd gesorteerd. Voor het admin-dashboard.
    /// </summary>
    Task<IReadOnlyList<CampDay>> GetDaysByOrganizationAsync(
        Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// Kampdag-toewijzingen van <paramref name="trainerId"/> binnen een datumbereik (inclusief), enkel
    /// actieve kampen, met kampdag, kamp en club. Op datum + starttijd gesorteerd.
    /// </summary>
    Task<IReadOnlyList<CampDayTrainer>> GetTrainerDaysAsync(
        Guid trainerId, Guid organizationId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Detail incl. Days + TrainerAssignments + EnrollmentForm.Fields (tracked voor update).</summary>
    Task<Camp?> GetByIdWithDetailsAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    /// <summary>Publieke read: kamp + dagen + trainerassignments, read-only, ongeacht tenant.</summary>
    Task<Camp?> GetByIdPublicAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    Task AddAsync(Camp camp, CancellationToken ct = default);
    void Remove(Camp camp);

    /// <summary>Verwijdert dagen + hun trainerassignments (Restrict FK: geen auto-cascade).</summary>
    void RemoveDays(IEnumerable<CampDay> days);

    /// <summary>Verwijdert alleen de trainerassignments van een dag (de dag zelf blijft).</summary>
    void RemoveDayTrainers(IEnumerable<CampDayTrainer> trainers);

    /// <summary>Heeft minstens één inschrijving (ook geannuleerd) deze dag gekozen?</summary>
    Task<bool> DayHasEnrollmentsAsync(Guid campDayId, CancellationToken ct = default);

    /// <summary>Heeft minstens één inschrijving (ook geannuleerd) dit dagdeel van deze dag gekozen?</summary>
    Task<bool> AnyEnrollmentUsesPartAsync(Guid campDayId, CoachOS.Domain.Enums.CampDayPart part, CancellationToken ct = default);

    /// <summary>Heeft minstens één inschrijving van dit kamp een halve dag (één dagdeel) gekozen?</summary>
    Task<bool> AnyHalfDayEnrollmentAsync(Guid campId, CancellationToken ct = default);
}
