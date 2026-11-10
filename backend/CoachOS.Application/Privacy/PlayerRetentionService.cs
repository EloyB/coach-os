using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Models.Privacy;

namespace CoachOS.Application.Privacy;

/// <summary>
/// Dezelfde regels als de handmatige verwijdering, plus: bewaartermijn verstreken en geen
/// groepsleider-overdracht nodig (die keuze maakt enkel een beheerder). Eén transactie per kandidaat.
/// </summary>
public sealed class PlayerRetentionService(
    IPlayerErasureRepository repository,
    PlayerErasureExecutor executor) : IPlayerRetentionService
{
    public async Task<Result<PlayerRetentionReport>> RunAsync(
        Guid organizationId, bool dryRun, CancellationToken ct = default)
    {
        if (organizationId == Guid.Empty)
            return Result<PlayerRetentionReport>.Fail(PlayerErasureErrors.NotFound);

        IReadOnlyList<ErasureCandidate> candidates = await repository.GetCandidatesAsync(organizationId, ct);
        int due = 0;
        int erased = 0;
        foreach (ErasureCandidate candidate in candidates)
        {
            if (!await IsDueAsync(organizationId, candidate, ct)) continue;
            due++;
            if (dryRun) continue;

            Result<PlayerErasureResultDto> result = await executor.ExecuteAsync(
                organizationId, candidate.Kind, candidate.EnrollmentId, RetentionGuard, ct);
            if (result.IsSuccess) erased++;
        }

        return Result<PlayerRetentionReport>.Ok(new PlayerRetentionReport(candidates.Count, due, erased));
    }

    private async Task<bool> IsDueAsync(Guid organizationId, ErasureCandidate candidate, CancellationToken ct)
    {
        PlayerErasureFacts? facts = await repository.GetFactsAsync(organizationId, candidate.Kind, candidate.EnrollmentId, ct);
        if (facts is null) return false;
        PlayerErasureEvaluation evaluation = PlayerErasureRules.Evaluate(facts, executor.Today());
        return RetentionGuard(facts, evaluation).IsSuccess && evaluation.CanErase;
    }

    private Result<Guid?> RetentionGuard(PlayerErasureFacts facts, PlayerErasureEvaluation evaluation)
    {
        if (evaluation.GroupEffect == ErasureGroupEffect.TransferLeader)
            return Result<Guid?>.Fail(PlayerErasureErrors.NewGroupLeaderRequired);
        return PlayerErasureRules.IsRetentionDue(evaluation.LastParticipation, executor.Today())
            ? Result<Guid?>.Ok(null)
            : Result<Guid?>.Fail(PlayerErasureErrors.RetentionNotDue);
    }
}
