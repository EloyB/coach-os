using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ITimeSlotPreferenceRepository
{
    Task<List<TimeSlotPreference>> GetByEnrollmentAsync(
        Guid enrollmentId, CancellationToken ct = default);

    Task<List<TimeSlotPreference>> GetBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<TimeSlotPreference> preferences, CancellationToken ct = default);

    Task RemoveByEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default);

    void RemoveRange(IEnumerable<TimeSlotPreference> preferences);

    Task SaveChangesAsync(CancellationToken ct = default);
}
