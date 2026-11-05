using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Subscriptions;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Privacy;

/// <summary>Durable lifecycle events. Contains user IDs and deadlines, never secret capabilities or arbitrary mail payloads.</summary>
public sealed class OrganizationOffboardingNotices(ApplicationDbContext db)
{
    public async Task<int> EnqueueAsync(Guid organizationId, DateOnly today, bool apply, CancellationToken ct = default)
    {
        if (organizationId == Guid.Empty) return 0;
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'", ct);
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE \"Subscriptions\",\"OrganizationMemberships\",\"AspNetUsers\" IN SHARE ROW EXCLUSIVE MODE", ct);
        DateOnly? end = OrganizationRetentionClock.EndedOn(await db.Subscriptions.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organizationId, ct));
        if (end is null || today < end) return 0;
        List<Guid> admins = await db.OrganizationMemberships.AsNoTracking().Where(m => m.OrganizationId == organizationId && m.Role == UserRole.Admin && m.IsActive && db.Users.Any(u => u.Id == m.UserId && u.IsActive && u.EmailConfirmed && !u.IsSuperAdmin)).Select(m => m.UserId).Distinct().ToListAsync(ct);
        int count = 0;
        foreach (int kind in today >= end.Value.AddDays(83) ? new[] { 0, 1 } : new[] { 0 })
            foreach (Guid user in admins)
            {
                if (!apply)
                {
                    int existing = await db.Database.SqlQuery<int>($"""SELECT count(*)::int AS "Value" FROM "OrganizationOffboardingNotices" WHERE "OrganizationId"={organizationId} AND "EndDate"={end.Value} AND "Kind"={kind} AND "UserId"={user}""").SingleAsync(ct);
                    if (existing == 0) count++; continue;
                }
                count += await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "OrganizationOffboardingNotices" ("OrganizationId","EndDate","Kind","UserId")
                VALUES ({organizationId},{end.Value},{kind},{user}) ON CONFLICT DO NOTHING
                """, ct);
            }
        if (apply) await tx.CommitAsync(ct);
        return count;
    }
}
