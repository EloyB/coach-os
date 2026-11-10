using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Models.Privacy;

namespace CoachOS.Application.Privacy;

public sealed class PlayerErasureService(
    IPlayerErasureRepository repository,
    PlayerErasureExecutor executor) : IPlayerErasureService
{
    public async Task<Result<PlayerErasurePreviewDto>> PreviewAsync(
        Guid organizationId, Guid actorUserId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct = default)
    {
        if (!await repository.IsActiveAdminAsync(organizationId, actorUserId, ct))
            return Result<PlayerErasurePreviewDto>.Fail(PlayerErasureErrors.Forbidden);

        PlayerErasureFacts? facts = await repository.GetFactsAsync(organizationId, kind, enrollmentId, ct);
        if (facts is null) return Result<PlayerErasurePreviewDto>.Fail(PlayerErasureErrors.NotFound);

        DateOnly today = executor.Today();
        PlayerErasureEvaluation evaluation = PlayerErasureRules.Evaluate(facts, today);
        return Result<PlayerErasurePreviewDto>.Ok(ToPreview(facts, evaluation, today));
    }

    public async Task<Result<PlayerErasureResultDto>> EraseAsync(
        Guid organizationId, Guid actorUserId, PrivacyErasureKind kind, Guid enrollmentId,
        ErasePlayerRequest request, CancellationToken ct = default)
    {
        if (!await repository.IsActiveAdminAsync(organizationId, actorUserId, ct))
            return Result<PlayerErasureResultDto>.Fail(PlayerErasureErrors.Forbidden);

        return await executor.ExecuteAsync(organizationId, kind, enrollmentId,
            (facts, evaluation) => CheckConfirmation(facts, evaluation, request), ct);
    }

    /// <summary>Draait binnen de transactie, op de opnieuw gelezen (gelockte) gegevens.</summary>
    internal static Result<Guid?> CheckConfirmation(
        PlayerErasureFacts facts, PlayerErasureEvaluation evaluation, ErasePlayerRequest request)
    {
        if (!string.Equals(request.ConfirmationName.Trim(), facts.Target.Name.Trim(), StringComparison.Ordinal))
            return Result<Guid?>.Fail(PlayerErasureErrors.NameMismatch);

        if (request.ExpectedUpdatedAt is not null && request.ExpectedUpdatedAt.Value != facts.Target.UpdatedAt)
            return Result<Guid?>.Fail(PlayerErasureErrors.ChangedSincePreview);

        if (evaluation.GroupEffect != ErasureGroupEffect.TransferLeader) return Result<Guid?>.Ok(null);

        bool validLeader = request.NewGroupLeaderId is not null
            && evaluation.OtherGroupMembers.Any(m => m.Id == request.NewGroupLeaderId.Value);
        return validLeader
            ? Result<Guid?>.Ok(request.NewGroupLeaderId)
            : Result<Guid?>.Fail(PlayerErasureErrors.NewGroupLeaderRequired);
    }

    private static PlayerErasurePreviewDto ToPreview(
        PlayerErasureFacts facts, PlayerErasureEvaluation evaluation, DateOnly today) =>
        new(
            facts.Target.Id,
            facts.Target.Name,
            evaluation.LastParticipation,
            PlayerErasureRules.IsRetentionDue(evaluation.LastParticipation, today),
            evaluation.CanErase,
            evaluation.BlockingReasons,
            facts.FormAnswerCount,
            evaluation.GroupEffect.ToString(),
            evaluation.OtherGroupMembers.Select(m => new ErasureGroupMemberDto(m.Id, m.Name)).ToList(),
            facts.Target.UpdatedAt);
}
