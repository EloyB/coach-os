using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Privacy;

public sealed class PlayerRetentionOptions
{
    public const string Section = "PlayerRetentionPreview";
    public bool Enabled { get; set; }
    public Guid[] OrganizationIds { get; set; } = [];
}

/// <summary>Read-only inventory, NOT authorization to delete or anonymize a player.</summary>
public sealed class PlayerRetentionPreview(
    ApplicationDbContext db,
    IOptions<PlayerRetentionOptions> options,
    ILogger<PlayerRetentionPreview> logger)
{
    public async Task<int> RunAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (!options.Value.Enabled || organizationId == Guid.Empty ||
            !options.Value.OrganizationIds.Contains(organizationId)) return 0;

        DateOnly cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-24);
        // Scope BOTH sides: even a corrupt cross-tenant FK must not reveal another tenant.
        List<Guid> candidates = await (
            from enrollment in db.Enrollments.AsNoTracking()
            join series in db.LessonSeries.AsNoTracking() on enrollment.LessonSerieId equals series.Id
            where enrollment.OrganizationId == organizationId && series.OrganizationId == organizationId
                && series.EndDate < cutoff
                && enrollment.ContactEmail.Trim() != ""
                // Conservative shared-contact guard: do not infer player identity
                // from an email shared with another registration, even an old one.
                && !db.Enrollments.Any(other => other.OrganizationId == organizationId
                    && other.Id != enrollment.Id
                    && other.ContactEmail.Trim().ToLower() == enrollment.ContactEmail.Trim().ToLower())
                && !db.CampEnrollments.Any(camp => camp.OrganizationId == organizationId
                    && camp.ParticipantEmail.Trim().ToLower() == enrollment.ContactEmail.Trim().ToLower())
                && !db.Lessons.Any(lesson => lesson.OrganizationId == organizationId
                    && lesson.LessonSerieId == series.Id && lesson.Date >= cutoff)
            select enrollment.Id).ToListAsync(ct);

        // IDs/contact data are intentionally not logged. These are only old-series
        // registrations for legal review, not a complete last-participation calculation.
        logger.LogInformation("DRY-RUN organization {OrganizationId}: {Count} old-series registrations for retention review; would require approved deletion/anonymization policy. No changes made.",
            organizationId, candidates.Count);
        return candidates.Count;
    }
}
