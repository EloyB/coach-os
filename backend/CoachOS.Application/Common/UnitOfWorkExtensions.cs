using System.Data;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Common;

public static class UnitOfWorkExtensions
{
    /// <summary>
    /// Voert <paramref name="work"/> uit in één transactie: commit bij een geslaagd resultaat,
    /// rollback bij een mislukt resultaat of een exception. De exception wordt na de rollback
    /// doorgegooid, zodat de aanroeper zelf beslist hoe hij ze vertaalt.
    /// </summary>
    public static async Task<Result<T>> ExecuteInTransactionAsync<T>(
        this IUnitOfWork unitOfWork,
        IsolationLevel isolationLevel,
        Func<Task<Result<T>>> work,
        CancellationToken ct = default)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync(isolationLevel, ct);
            var result = await work();

            if (result.IsSuccess)
                await unitOfWork.CommitTransactionAsync(ct);
            else
                await unitOfWork.RollbackTransactionAsync(ct);

            return result;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
