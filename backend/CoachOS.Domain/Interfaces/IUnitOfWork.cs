using System.Data;

namespace CoachOS.Domain.Interfaces;

/// <summary>
/// Eén plek om wijzigingen weg te schrijven en transacties te sturen. Alle repositories
/// delen dezelfde scoped DbContext, dus een save flusht altijd de volledige change-set —
/// services roepen daarom deze interface aan i.p.v. SaveChanges op een willekeurige repository.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Zoals <see cref="SaveChangesAsync"/>, maar geeft <c>false</c> als een andere schrijver voor
    /// was: een concurrency-conflict of een schending van een unieke index. De openstaande
    /// wijzigingen worden dan verworpen. Voor idempotente verwerking (webhooks, jobs).
    /// </summary>
    Task<bool> TrySaveChangesAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default);

    /// <summary>No-op wanneer er geen actieve transactie is.</summary>
    Task CommitTransactionAsync(CancellationToken ct = default);

    /// <summary>No-op wanneer er geen actieve transactie is.</summary>
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
