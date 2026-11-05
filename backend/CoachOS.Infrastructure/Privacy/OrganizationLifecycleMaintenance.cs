using CoachOS.Application.Privacy;
using CoachOS.Domain.Common;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Privacy;

public sealed class OrganizationLifecycleMaintenance(ApplicationDbContext db, OrganizationOffboardingNotices notices, OrganizationOffboardingDelivery delivery, IOrganizationErasureService erasure, IOptions<OrganizationRetentionOptions> options, TimeProvider clock, ILogger<OrganizationLifecycleMaintenance> logger)
{
    public async Task<bool> RunAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (!options.Value.Enabled || organizationId == Guid.Empty || !options.Value.OrganizationIds.Contains(organizationId)) return false;
        if (options.Value.Apply && !options.Value.DryRun)
        {
            await notices.EnqueueAsync(organizationId, clock.GetBrusselsToday(), true, ct);
            await delivery.RunAsync(organizationId, ct);
        }
        OrganizationErasureResult preview = await erasure.ProcessAsync(organizationId, new(), ct);
        if (!preview.Eligible || !options.Value.Apply || options.Value.DryRun)
        {
            logger.LogInformation("Offboarding organization {OrganizationId}: eligible {Eligible}; execution disabled or {Count} explicit review holds. Deadlines are not extended", organizationId, preview.Eligible, preview.Holds.Length);
            if (preview.Holds.Contains("notice_delivery_review_required")) logger.LogWarning("Offboarding organization {OrganizationId}: administrator notices missing/late/failed; operator intervention required before erasure, no silent legal retention extension", organizationId);
            return false;
        }
        string? name = await db.Organizations.AsNoTracking().Where(x => x.Id == organizationId).Select(x => x.Name).SingleOrDefaultAsync(ct);
        if (name is null) return false;
        OrganizationErasureResult result = await erasure.ProcessAsync(organizationId, new(Apply: true, Confirmation: name, PreviewHash: preview.PreviewHash), ct);
        return result.Deleted;
    }
}
