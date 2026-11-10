using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Privacy;

/// <summary>
/// Automatische bewaartermijn (24 maanden na laatste deelname). Met <c>dryRun</c> wordt enkel geteld.
/// </summary>
public interface IPlayerRetentionService
{
    Task<Result<PlayerRetentionReport>> RunAsync(Guid organizationId, bool dryRun, CancellationToken ct = default);
}
