using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Privacy;

/// <summary>
/// Automatische verwijdering van organisaties na de bewaartermijn (90 dagen na het einde van de
/// proef of het abonnement): plannen, de club twee keer verwittigen en op de geplande dag via de
/// offboarding verwijderen. Systeemjob, cross-org — nooit vanuit een request.
/// </summary>
public interface IOrganizationRetentionService
{
    /// <summary>
    /// Eén run over alle organisaties. Idempotent: een herstart of een tweede API-instantie
    /// verstuurt niets dubbel. Bij <paramref name="dryRun"/> wordt enkel geteld.
    /// </summary>
    Task<Result<OrganizationRetentionReport>> RunAsync(bool dryRun, CancellationToken ct = default);
}
