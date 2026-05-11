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
    Task<List<ScheduleAssignment>> GetByStudentEmailAsync(
        string email, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<ScheduleAssignment> assignments, CancellationToken ct = default);

    void RemoveRange(IEnumerable<ScheduleAssignment> assignments);

    Task RemoveProposedBySeriesAsync(Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    Task SetProposedToAwaitingConfirmationAsync(Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
