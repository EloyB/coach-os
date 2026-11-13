using CoachOS.Application.Abstractions;
using CoachOS.Domain.Entities;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Email;

internal sealed class OrganizationDeletionNoticeStatus(ApplicationDbContext db) : IOrganizationDeletionNoticeStatus
{
    private static readonly string NoticeType = OutboxEmails.KeyOf(typeof(OrganizationDeletionNoticeMail));

    public async Task<EmailDeliveryStatus> GetAsync(
        Guid organizationId, DateTime queuedSinceUtc, CancellationToken ct = default)
    {
        // Bewust cross-tenant: de retentiejob draait zonder tenant.
        List<string> statuses = await db.EmailOutboxMessages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.OrganizationId == organizationId
                        && m.Type == NoticeType
                        && m.CreatedAt >= queuedSinceUtc)
            .Select(m => m.Status)
            .ToListAsync(ct);

        if (statuses.Contains(EmailOutboxStatuses.Completed)) return EmailDeliveryStatus.Sent;
        if (statuses.Contains(EmailOutboxStatuses.Pending) || statuses.Contains(EmailOutboxStatuses.Processing))
            return EmailDeliveryStatus.Pending;
        if (statuses.Contains(EmailOutboxStatuses.Failed)) return EmailDeliveryStatus.Failed;
        return EmailDeliveryStatus.Missing;
    }
}
