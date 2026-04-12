using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IAssignmentConfirmationTokenRepository
{
    Task<AssignmentConfirmationToken?> GetByTokenHashAsync(
        string tokenHash, CancellationToken ct = default);

    Task<List<AssignmentConfirmationToken>> GetBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    Task AddRangeAsync(
        IEnumerable<AssignmentConfirmationToken> tokens, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
