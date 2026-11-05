using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Privacy;

public sealed class OrganizationRetentionOptions
{
    public const string Section = "OrganizationRetentionPreview";
    public bool Enabled { get; set; }
    public Guid[] OrganizationIds { get; set; } = [];
}

/// <summary>Review signal only. Does not authorize deletion or change access.</summary>
public sealed class OrganizationRetentionPreview(ApplicationDbContext db,
    IOptions<OrganizationRetentionOptions> options, ILogger<OrganizationRetentionPreview> logger)
{
    public async Task<bool> RunAsync(Guid organizationId, DateOnly today, CancellationToken ct = default)
    {
        if (!options.Value.Enabled || organizationId == Guid.Empty ||
            !options.Value.OrganizationIds.Contains(organizationId)) return false;
        Subscription? subscription = await db.Subscriptions.AsNoTracking()
            .SingleOrDefaultAsync(s => s.OrganizationId == organizationId, ct);
        if (subscription is null) return false;

        // Never use historical TrialEndsAt for Active/Legacy/Locked. Ambiguous
        // cancellations are skipped; choose no legal precedence for conflicting dates.
        DateOnly? ended = subscription.Status switch
        {
            SubscriptionStatus.Trialing when subscription.Plan is null
                && subscription.CurrentPeriodEnd is null && subscription.MollieSubscriptionId is null
                => subscription.TrialEndsAt,
            SubscriptionStatus.Cancelled when subscription.CancelAt is not null
                && subscription.CancelAt == subscription.CurrentPeriodEnd => subscription.CancelAt,
            _ => null
        };
        if (ended is null || today.DayNumber - ended.Value.DayNumber < 90) return false;

        logger.LogInformation("DRY-RUN organization {OrganizationId}: ended {EndDate}; 90-day retention review due. Intended deletion requires approved financial archive, notice and export policy; NO data changed.",
            organizationId, ended.Value);
        return true;
    }
}
