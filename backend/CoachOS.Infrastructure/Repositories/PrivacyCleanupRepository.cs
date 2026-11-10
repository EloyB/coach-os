using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

/// <summary>
/// Platformbrede opruiming (bewust over alle organisaties heen, zonder tenantfilter): de records
/// worden enkel op leeftijd en status geselecteerd, nooit gelezen of teruggegeven.
/// </summary>
public sealed class PrivacyCleanupRepository(ApplicationDbContext db) : IPrivacyCleanupRepository
{
    public async Task<int> CleanupCompletedMailAsync(DateTime processedBefore, bool apply, CancellationToken ct = default)
    {
        IQueryable<EmailOutboxMessage> query = db.EmailOutboxMessages.IgnoreQueryFilters()
            .Where(x => x.Status == EmailOutboxStatuses.Completed &&
                        (x.ProcessedAt ?? x.UpdatedAt) < processedBefore);
        return apply ? await query.ExecuteDeleteAsync(ct) : await query.CountAsync(ct);
    }

    public async Task<int> CleanupExpiredMagicLinksAsync(DateTime expiredBefore, bool apply, CancellationToken ct = default)
    {
        IQueryable<MagicLinkToken> query = db.MagicLinkTokens.IgnoreQueryFilters()
            .Where(x => x.ExpiresAt < expiredBefore);
        return apply ? await query.ExecuteDeleteAsync(ct) : await query.CountAsync(ct);
    }

    public async Task<int> CleanupExpiredConfirmationTokensAsync(DateTime expiredBefore, bool apply, CancellationToken ct = default)
    {
        IQueryable<AssignmentConfirmationToken> query = db.AssignmentConfirmationTokens.IgnoreQueryFilters()
            .Where(x => x.ExpiresAt < expiredBefore)
            // Onbeantwoorde links van een toewijzing die nog op bevestiging wacht, blijven:
            // de lijst "niet gereageerd" en het afronden van de reeks lezen ze nog.
            .Where(x => x.Response != ConfirmationResponse.Pending ||
                        x.ScheduleAssignment.Status != ScheduleAssignmentStatus.AwaitingConfirmation);
        return apply ? await query.ExecuteDeleteAsync(ct) : await query.CountAsync(ct);
    }
}
