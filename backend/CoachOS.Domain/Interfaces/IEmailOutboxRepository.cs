using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface IEmailOutboxRepository
{
    Task AddRangeAsync(IEnumerable<EmailOutboxMessage> messages, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
