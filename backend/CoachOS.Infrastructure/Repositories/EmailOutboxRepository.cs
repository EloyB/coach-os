using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;

namespace CoachOS.Infrastructure.Repositories;

public class EmailOutboxRepository(ApplicationDbContext db) : IEmailOutboxRepository
{
    public async Task AddRangeAsync(IEnumerable<EmailOutboxMessage> messages, CancellationToken ct = default)
        => await db.EmailOutboxMessages.AddRangeAsync(messages, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
