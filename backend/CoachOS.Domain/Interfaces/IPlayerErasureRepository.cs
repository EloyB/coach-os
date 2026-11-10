using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models.Privacy;

namespace CoachOS.Domain.Interfaces;

/// <summary>
/// Datatoegang voor het definitief wissen van een (kamp)inschrijving. Elke methode filtert op
/// <c>organizationId</c>; gerelateerde inschrijvingen worden enkel gelezen (blokkeren), nooit gewist.
/// </summary>
public interface IPlayerErasureRepository
{
    Task<bool> IsActiveAdminAsync(Guid organizationId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Vereist een actieve transactie. Neemt een transactie-scoped advisory lock per organisatie
    /// (serialiseert verwijderingen binnen één club) en <c>SELECT ... FOR UPDATE</c> op de
    /// doelinschrijving. Wacht maximaal enkele seconden.
    /// </summary>
    Task<ErasureLockOutcome> LockForErasureAsync(Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct = default);

    Task<PlayerErasureFacts?> GetFactsAsync(Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct = default);

    Task<IReadOnlyList<ErasureCandidate>> GetCandidatesAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Wist de doelinschrijving en haar persoonlijke nevenrecords. Vereist een actieve transactie.</summary>
    Task<ErasureDeleteOutcome> DeleteAsync(Guid organizationId, PlayerErasurePlan plan, CancellationToken ct = default);

    Task AddReceiptAsync(PrivacyErasureReceipt receipt, CancellationToken ct = default);
}
