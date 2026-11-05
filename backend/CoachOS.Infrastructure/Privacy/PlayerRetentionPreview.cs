using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Privacy;

public sealed class PlayerRetentionOptions
{
    public const string Section = "PlayerRetentionPreview";
    public bool Enabled { get; set; }
    public bool Apply { get; set; }
    public bool DryRun { get; set; } = true;
    public Guid[] OrganizationIds { get; set; } = [];
}

/// <summary>Optional maintenance; actual execution additionally requires Apply and !DryRun.</summary>
public sealed class PlayerRetentionPreview(
    ApplicationDbContext db,
    IOptions<PlayerRetentionOptions> options,
    ILogger<PlayerRetentionPreview> logger)
{
    public async Task<int> RunAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (!options.Value.Enabled || organizationId == Guid.Empty ||
            !options.Value.OrganizationIds.Contains(organizationId)) return 0;

        if (options.Value.Apply && !options.Value.DryRun)
        {
            DateTime now = DateTime.UtcNow;
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct);
            // Magic links have no OrganizationId: this explicit platform-security cleanup
            // removes only used/expired capabilities, never a valid link or account.
            await db.MagicLinkTokens.Where(x => x.UsedAt != null || x.ExpiresAt <= now).ExecuteDeleteAsync(ct);
            // Platform account capabilities likewise have no organization FK. Clear only
            // provably expired payloads; preserve users, memberships and every abuse counter.
            await db.Users.Where(x => x.InviteToken != null && x.InviteTokenExpiry != null && x.InviteTokenExpiry <= now)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.InviteToken, (string?)null).SetProperty(x => x.InviteTokenExpiry, (DateTime?)null), ct);
            await db.Users.Where(x => x.EmailVerificationCodeHash != null && x.EmailVerificationCodeExpiry != null && x.EmailVerificationCodeExpiry <= now)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailVerificationCodeHash, (string?)null).SetProperty(x => x.EmailVerificationCodeExpiry, (DateTime?)null), ct);
            await db.AssignmentConfirmationTokens.Where(x => x.OrganizationId == organizationId &&
                (x.RespondedAt != null || x.ExpiresAt <= now)).ExecuteDeleteAsync(ct);
            await db.EmailOutboxMessages.Where(x => x.OrganizationId == organizationId &&
                x.Status == CoachOS.Domain.Entities.EmailOutboxStatuses.Completed && x.ProcessedAt != null &&
                x.ProcessedAt <= now.AddDays(-30)).ExecuteDeleteAsync(ct);
            await transaction.CommitAsync(ct);
            await transaction.DisposeAsync();
        }

        // Dry-run and apply deliberately share eligibility, including camp/standalone
        // participation and identity guards. Preview itself never mutates data.
        List<CoachOS.Domain.Entities.Enrollment> enrollments = await db.Enrollments.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct);
        List<CoachOS.Domain.Entities.CampEnrollment> camps = await db.CampEnrollments.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct);
        PlayerErasureService erasure = new(db);
        bool apply = options.Value.Apply && !options.Value.DryRun;
        int count = 0;
        foreach ((Guid id, string name, bool camp) in enrollments.Select(x => (x.Id, x.StudentName, false)).Concat(camps.Select(x => (x.Id, x.ParticipantName, true))))
        {
            CoachOS.Application.Privacy.PlayerErasureResult preview = await erasure.ProcessAsync(organizationId, id, camp, new(), ct);
            if (!preview.Eligible) continue;
            if (!apply) { count++; continue; }
            CoachOS.Application.Privacy.PlayerErasureResult result = await erasure.ProcessAsync(organizationId, id, camp, new(true, false, preview.PreviewHash, name, true), ct);
            if (result.Deleted) count++;
        }
        logger.LogInformation("Retention organization {OrganizationId}: {Count} eligible non-financial records; execution {Apply}. Blocked records require separate review", organizationId, count, apply);
        return count;
    }
}
