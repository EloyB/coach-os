using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Models.Privacy;

namespace CoachOS.Application.Privacy;

/// <summary>
/// Extra controle die de aanroeper binnen de transactie uitvoert (bv. naamsbevestiging).
/// Geeft bij succes de eventuele nieuwe groepsleider terug.
/// </summary>
public delegate Result<Guid?> ErasureGuard(PlayerErasureFacts facts, PlayerErasureEvaluation evaluation);

/// <summary>
/// Transactionele kern: lock → feiten opnieuw lezen → regels → guard → wissen → receipt.
/// Gedeeld door de handmatige verwijdering en de retentiejob. Een preview neemt geen lock.
/// </summary>
public sealed class PlayerErasureExecutor(
    IPlayerErasureRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public DateOnly Today() => DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

    public async Task<Result<PlayerErasureResultDto>> ExecuteAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, ErasureGuard guard,
        CancellationToken ct = default)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            Result<PlayerErasureResultDto> result =
                await ExecuteLockedAsync(organizationId, kind, enrollmentId, guard, ct);
            if (result.IsSuccess) await unitOfWork.CommitTransactionAsync(ct);
            else await unitOfWork.RollbackTransactionAsync(ct);
            return result;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<Result<PlayerErasureResultDto>> ExecuteLockedAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, ErasureGuard guard, CancellationToken ct)
    {
        ErasureLockOutcome lockOutcome = await repository.LockForErasureAsync(organizationId, kind, enrollmentId, ct);
        if (lockOutcome == ErasureLockOutcome.LockTimeout) return Fail(PlayerErasureErrors.LockTimeout);
        if (lockOutcome == ErasureLockOutcome.NotFound) return Fail(PlayerErasureErrors.NotFound);

        // Na de lock opnieuw lezen: alles wat sinds de preview veranderde telt mee.
        PlayerErasureFacts? facts = await repository.GetFactsAsync(organizationId, kind, enrollmentId, ct);
        if (facts is null) return Fail(PlayerErasureErrors.NotFound);

        PlayerErasureEvaluation evaluation = PlayerErasureRules.Evaluate(facts, Today());
        if (!evaluation.CanErase) return Fail(PlayerErasureErrors.Blocked);

        Result<Guid?> guardResult = guard(facts, evaluation);
        if (!guardResult.IsSuccess) return Result<PlayerErasureResultDto>.Fail(guardResult.Errors);

        PlayerErasurePlan plan = new(kind, enrollmentId, facts.Target.GroupId, evaluation.GroupEffect, guardResult.Value);
        ErasureDeleteOutcome deleteOutcome = await repository.DeleteAsync(organizationId, plan, ct);
        if (deleteOutcome == ErasureDeleteOutcome.DependencyConflict) return Fail(PlayerErasureErrors.DependencyConflict);

        return Result<PlayerErasureResultDto>.Ok(await WriteReceiptAsync(organizationId, kind, enrollmentId, ct));
    }

    private async Task<PlayerErasureResultDto> WriteReceiptAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct)
    {
        DateTime erasedAt = time.GetUtcNow().UtcDateTime;
        await repository.AddReceiptAsync(new PrivacyErasureReceipt
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Kind = kind,
            SubjectId = enrollmentId,
            ErasedAt = erasedAt,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new PlayerErasureResultDto(enrollmentId, erasedAt);
    }

    private static Result<PlayerErasureResultDto> Fail(Error error) => Result<PlayerErasureResultDto>.Fail(error);
}
