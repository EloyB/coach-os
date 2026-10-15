using System.Data;
using CoachOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Persistence;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        await context.Database.BeginTransactionAsync(ct);
    }

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default)
    {
        await context.Database.BeginTransactionAsync(isolationLevel, ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (context.Database.CurrentTransaction is not null)
            await context.Database.CommitTransactionAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (context.Database.CurrentTransaction is not null)
            await context.Database.RollbackTransactionAsync(ct);
    }
}
