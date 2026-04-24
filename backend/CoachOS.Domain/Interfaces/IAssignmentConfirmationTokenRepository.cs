using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IAssignmentConfirmationTokenRepository
{
    Task<AssignmentConfirmationToken?> GetByTokenHashAsync(
        string tokenHash, CancellationToken ct = default);

    Task<List<AssignmentConfirmationToken>> GetBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    Task<List<AssignmentConfirmationToken>> GetPendingByOrganizationAsync(
        Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// No-tracking variant voor read-only paden die moeten lezen nadat
    /// <see cref="TryClaimResponseAsync"/> of <see cref="TryTransitionResponseAsync"/>
    /// via <c>ExecuteUpdateAsync</c> de DB heeft gemuteerd. De tracking-versie zou
    /// via identity resolution een stale in-memory instance teruggeven.
    /// </summary>
    Task<List<AssignmentConfirmationToken>> GetBySeriesAsNoTrackingAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default);

    Task AddRangeAsync(
        IEnumerable<AssignmentConfirmationToken> tokens, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Atomisch de Response van <c>Pending</c> naar <paramref name="target"/> flippen.
    /// Retourneert true als de update één rij raakte (pending was), anders false
    /// (al verwerkt door een parallel request). Voorkomt double-confirm / double-decline.
    /// </summary>
    Task<bool> TryClaimResponseAsync(
        Guid tokenId,
        Domain.Enums.ConfirmationResponse target,
        DateTime now,
        CancellationToken ct = default);

    /// <summary>
    /// Atomisch de Response van een specifieke waarde (<paramref name="from"/>) naar
    /// <paramref name="to"/> flippen. Gebruikt voor "pick alternative" waar de
    /// transition Declined → Confirmed is. Retourneert true bij één rij affected.
    /// </summary>
    Task<bool> TryTransitionResponseAsync(
        Guid tokenId,
        Domain.Enums.ConfirmationResponse from,
        Domain.Enums.ConfirmationResponse to,
        DateTime now,
        CancellationToken ct = default);
}
