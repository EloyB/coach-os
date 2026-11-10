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
/// Gedeeld door de handmatige verwijdering en de retentiejob (die ook kan anonimiseren).
/// Een preview neemt geen lock.
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
        => await InTransactionAsync(result => result.IsSuccess,
            () => ExecuteLockedAsync(organizationId, kind, enrollmentId, guard, ct), ct);

    /// <summary>
    /// Bewaartermijn voor één inschrijving: binnen dezelfde lock en met verse feiten opnieuw
    /// beslissen (<see cref="PlayerErasureRules.DecideRetention"/>), dan wissen of anonimiseren.
    /// Niets doen (niet verstreken / geblokkeerd) is een geslaagd resultaat zonder wijziging.
    /// </summary>
    public async Task<Result<RetentionDecision>> ExecuteRetentionAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct = default)
        => await InTransactionAsync(
            result => result.IsSuccess && result.Value is RetentionDecision.Erase or RetentionDecision.Anonymize,
            () => ExecuteRetentionLockedAsync(organizationId, kind, enrollmentId, ct), ct);

    private async Task<Result<T>> InTransactionAsync<T>(
        Func<Result<T>, bool> commit, Func<Task<Result<T>>> work, CancellationToken ct)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            Result<T> result = await work();
            if (commit(result)) await unitOfWork.CommitTransactionAsync(ct);
            else await unitOfWork.RollbackTransactionAsync(ct);
            return result;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>Lock nemen en daarna de feiten opnieuw lezen: alles wat sinds de preview veranderde telt mee.</summary>
    private async Task<Result<PlayerErasureFacts>> LockAndReadAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct)
    {
        ErasureLockOutcome lockOutcome = await repository.LockForErasureAsync(organizationId, kind, enrollmentId, ct);
        if (lockOutcome == ErasureLockOutcome.LockTimeout) return Result<PlayerErasureFacts>.Fail(PlayerErasureErrors.LockTimeout);
        if (lockOutcome == ErasureLockOutcome.NotFound) return Result<PlayerErasureFacts>.Fail(PlayerErasureErrors.NotFound);

        PlayerErasureFacts? facts = await repository.GetFactsAsync(organizationId, kind, enrollmentId, ct);
        return facts is null
            ? Result<PlayerErasureFacts>.Fail(PlayerErasureErrors.NotFound)
            : Result<PlayerErasureFacts>.Ok(facts);
    }

    private async Task<Result<PlayerErasureResultDto>> ExecuteLockedAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, ErasureGuard guard, CancellationToken ct)
    {
        Result<PlayerErasureFacts> read = await LockAndReadAsync(organizationId, kind, enrollmentId, ct);
        if (!read.IsSuccess) return Result<PlayerErasureResultDto>.Fail(read.Errors);
        PlayerErasureFacts facts = read.Value!;

        PlayerErasureEvaluation evaluation = PlayerErasureRules.Evaluate(facts, Today());
        if (!evaluation.CanErase) return Fail(PlayerErasureErrors.Blocked);

        Result<Guid?> guardResult = guard(facts, evaluation);
        if (!guardResult.IsSuccess) return Result<PlayerErasureResultDto>.Fail(guardResult.Errors);

        PlayerErasurePlan plan = new(kind, enrollmentId, facts.Target.GroupId, evaluation.GroupEffect, guardResult.Value);
        ErasureDeleteOutcome deleteOutcome = await repository.DeleteAsync(organizationId, plan, ct);
        if (deleteOutcome == ErasureDeleteOutcome.DependencyConflict) return Fail(PlayerErasureErrors.DependencyConflict);

        return Result<PlayerErasureResultDto>.Ok(
            await WriteReceiptAsync(organizationId, kind, enrollmentId, PrivacyErasureMethod.Deleted, ct));
    }

    private async Task<Result<RetentionDecision>> ExecuteRetentionLockedAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct)
    {
        Result<PlayerErasureFacts> read = await LockAndReadAsync(organizationId, kind, enrollmentId, ct);
        if (!read.IsSuccess) return Result<RetentionDecision>.Fail(read.Errors);
        PlayerErasureFacts facts = read.Value!;

        PlayerErasureEvaluation evaluation = PlayerErasureRules.Evaluate(facts, Today());
        RetentionDecision decision = PlayerErasureRules.DecideRetention(facts, evaluation, Today());
        if (decision is RetentionDecision.NotDue or RetentionDecision.Blocked) return Result<RetentionDecision>.Ok(decision);

        PlayerErasurePlan plan = new(kind, enrollmentId, facts.Target.GroupId, evaluation.GroupEffect, null);
        ErasureDeleteOutcome outcome = decision == RetentionDecision.Erase
            ? await repository.DeleteAsync(organizationId, plan, ct)
            : await repository.AnonymizeAsync(organizationId, plan, time.GetUtcNow().UtcDateTime, ct);
        if (outcome == ErasureDeleteOutcome.DependencyConflict)
            return Result<RetentionDecision>.Fail(PlayerErasureErrors.DependencyConflict);

        PrivacyErasureMethod method = decision == RetentionDecision.Erase
            ? PrivacyErasureMethod.Deleted
            : PrivacyErasureMethod.Anonymized;
        await WriteReceiptAsync(organizationId, kind, enrollmentId, method, ct);
        return Result<RetentionDecision>.Ok(decision);
    }

    private async Task<PlayerErasureResultDto> WriteReceiptAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, PrivacyErasureMethod method, CancellationToken ct)
    {
        DateTime erasedAt = time.GetUtcNow().UtcDateTime;
        await repository.AddReceiptAsync(new PrivacyErasureReceipt
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Kind = kind,
            Method = method,
            SubjectId = enrollmentId,
            ErasedAt = erasedAt,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new PlayerErasureResultDto(enrollmentId, erasedAt);
    }

    private static Result<PlayerErasureResultDto> Fail(Error error) => Result<PlayerErasureResultDto>.Fail(error);
}
