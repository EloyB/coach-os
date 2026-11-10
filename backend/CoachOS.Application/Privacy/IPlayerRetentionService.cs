using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Privacy;

/// <summary>
/// Automatische bewaartermijn (24 maanden na laatste deelname): wissen, of anonimiseren als er
/// betalingen aan hangen. Met <c>dryRun</c> wordt enkel geteld.
/// </summary>
public interface IPlayerRetentionService
{
    /// <summary>Alle organisaties; de worker gebruikt dit wanneer er geen filter ingesteld is.</summary>
    Task<IReadOnlyList<Guid>> GetOrganizationIdsAsync(CancellationToken ct = default);

    Task<Result<PlayerRetentionReport>> RunAsync(Guid organizationId, bool dryRun, CancellationToken ct = default);
}
