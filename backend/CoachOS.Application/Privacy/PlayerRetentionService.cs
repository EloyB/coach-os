using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Models.Privacy;

namespace CoachOS.Application.Privacy;

/// <summary>
/// Bewaartermijn van 24 maanden na de laatste deelname. Dezelfde regels als de handmatige
/// verwijdering, met twee verschillen (zie <see cref="PlayerErasureRules.DecideRetention"/>):
/// een groepsleider wordt nooit automatisch vervangen, en een speler wiens enige blokkering
/// zijn eigen voltooide betalingen zijn, wordt geanonimiseerd in plaats van overgeslagen.
/// Eén transactie per kandidaat; de beslissing wordt binnen de lock opnieuw genomen.
/// </summary>
public sealed class PlayerRetentionService(
    IPlayerErasureRepository repository,
    PlayerErasureExecutor executor) : IPlayerRetentionService
{
    public async Task<IReadOnlyList<Guid>> GetOrganizationIdsAsync(CancellationToken ct = default)
        => await repository.GetOrganizationIdsAsync(ct);

    public async Task<Result<PlayerRetentionReport>> RunAsync(
        Guid organizationId, bool dryRun, CancellationToken ct = default)
    {
        if (organizationId == Guid.Empty)
            return Result<PlayerRetentionReport>.Fail(PlayerErasureErrors.NotFound);

        IReadOnlyList<ErasureCandidate> candidates = await repository.GetCandidatesAsync(organizationId, ct);
        Tally tally = new();
        foreach (ErasureCandidate candidate in candidates)
        {
            RetentionDecision decision = await DecideAsync(organizationId, candidate, ct);
            if (decision == RetentionDecision.NotDue) continue;
            tally.Count(decision);
            if (dryRun || decision == RetentionDecision.Blocked) continue;

            Result<RetentionDecision> applied = await executor.ExecuteRetentionAsync(
                organizationId, candidate.Kind, candidate.EnrollmentId, ct);
            if (applied.IsSuccess) tally.Applied(applied.Value);
        }

        return Result<PlayerRetentionReport>.Ok(tally.ToReport(candidates.Count));
    }

    /// <summary>Beslissing zonder lock, enkel om te tellen en te kiezen; de executor beslist opnieuw.</summary>
    private async Task<RetentionDecision> DecideAsync(Guid organizationId, ErasureCandidate candidate, CancellationToken ct)
    {
        PlayerErasureFacts? facts = await repository.GetFactsAsync(organizationId, candidate.Kind, candidate.EnrollmentId, ct);
        if (facts is null) return RetentionDecision.NotDue;
        DateOnly today = executor.Today();
        return PlayerErasureRules.DecideRetention(facts, PlayerErasureRules.Evaluate(facts, today), today);
    }

    private sealed class Tally
    {
        private int _erase;
        private int _anonymize;
        private int _blocked;
        private int _erased;
        private int _anonymized;

        public void Count(RetentionDecision decision)
        {
            if (decision == RetentionDecision.Erase) _erase++;
            else if (decision == RetentionDecision.Anonymize) _anonymize++;
            else if (decision == RetentionDecision.Blocked) _blocked++;
        }

        public void Applied(RetentionDecision decision)
        {
            if (decision == RetentionDecision.Erase) _erased++;
            else if (decision == RetentionDecision.Anonymize) _anonymized++;
        }

        public PlayerRetentionReport ToReport(int candidates)
            => new(candidates, _erase, _anonymize, _blocked, _erased, _anonymized);
    }
}
