using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class LessonInvitationRepository(ApplicationDbContext context) : ILessonInvitationRepository
{
    public async Task<LessonInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        // AsNoTracking: het publieke pad muteert nooit via tracked entity (state-flips
        // gaan via ExecuteUpdate in TryClaimResponseAsync). Door tracking uit te zetten
        // krijgt een verse re-fetch na ExecuteUpdate de actuele DB-status, niet de
        // stale in-memory entity uit identity resolution.
        => await context.LessonInvitations
            .AsNoTracking()
            .Include(i => i.Lesson)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

    public async Task<LessonInvitation?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
        => await context.LessonInvitations
            .FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == organizationId, ct);

    public async Task<IReadOnlyList<LessonInvitation>> GetByLessonAsync(
        Guid lessonId, Guid organizationId, CancellationToken ct = default)
        => await context.LessonInvitations
            .AsNoTracking()
            .Where(i => i.LessonId == lessonId && i.OrganizationId == organizationId)
            .OrderBy(i => i.Email)
            .ToListAsync(ct);

    public async Task<bool> ExistsByLessonAndEmailAsync(
        Guid lessonId, string email, CancellationToken ct = default)
        => await context.LessonInvitations
            .AsNoTracking()
            .AnyAsync(i => i.LessonId == lessonId && i.Email == email, ct);

    public async Task AddAsync(LessonInvitation invitation, CancellationToken ct = default)
        => await context.LessonInvitations.AddAsync(invitation, ct);

    public async Task AddRangeAsync(IEnumerable<LessonInvitation> invitations, CancellationToken ct = default)
        => await context.LessonInvitations.AddRangeAsync(invitations, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);

    public async Task<bool> TryClaimResponseAsync(
        Guid invitationId,
        LessonInvitationStatus target,
        DateTime now,
        CancellationToken ct = default)
    {
        // Atomic claim via conditional UPDATE. PostgreSQL lockt de row tijdens UPDATE —
        // twee parallelle requests zien er exact één met affected=1, de andere 0.
        int affected = await context.LessonInvitations
            .Where(i => i.Id == invitationId && i.Status == LessonInvitationStatus.Pending)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Status, target)
                    .SetProperty(i => i.RespondedAt, now)
                    .SetProperty(i => i.UpdatedAt, now),
                ct);

        return affected == 1;
    }

    public async Task<int> ReassignToLessonAsync(
        Guid fromLessonId, Guid toLessonId, CancellationToken ct = default)
    {
        DateTime now = DateTime.UtcNow;
        return await context.LessonInvitations
            .Where(i => i.LessonId == fromLessonId)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.LessonId, toLessonId)
                    .SetProperty(i => i.UpdatedAt, now),
                ct);
    }

    public async Task<bool> AnyByLessonIdsAsync(
        IReadOnlyCollection<Guid> lessonIds, CancellationToken ct = default)
    {
        if (lessonIds.Count == 0)
            return false;

        return await context.LessonInvitations
            .AsNoTracking()
            .AnyAsync(i => lessonIds.Contains(i.LessonId), ct);
    }
}
