using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IScheduleAssignmentRepository
{
    Task<List<ScheduleAssignment>> GetBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    Task<ScheduleAssignment?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Returns all assignments where the student (by email) participates — either
    /// directly via the enrollment, or as a group member. Excludes declined
    /// assignments and inactive series.
    /// </summary>
    Task<List<ScheduleAssignment>> GetByContactEmailAsync(
        string email, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<ScheduleAssignment> assignments, CancellationToken ct = default);

    void RemoveRange(IEnumerable<ScheduleAssignment> assignments);

    Task RemoveProposedBySeriesAsync(Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    Task SetProposedToAwaitingConfirmationAsync(Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Ids van de deelnemers met een toewijzing die op bevestiging van de leerling wacht
    /// (<see cref="Enums.ScheduleAssignmentStatus.AwaitingConfirmation"/>): de
    /// inschrijvings-id bij een solo-toewijzing, de groeps-id bij een groepstoewijzing.
    /// </summary>
    Task<HashSet<Guid>> GetAwaitingConfirmationParticipantIdsAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);
}
