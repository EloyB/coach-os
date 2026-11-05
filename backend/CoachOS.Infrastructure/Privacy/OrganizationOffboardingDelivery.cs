using CoachOS.Application.Abstractions;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Subscriptions;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Privacy;

public sealed class OrganizationOffboardingDelivery(ApplicationDbContext db, IEmailService email, TimeProvider clock, ILogger<OrganizationOffboardingDelivery> logger)
{
    private sealed class Claim
    {
        public DateOnly EndDate { get; set; }
        public int Kind { get; set; }
        public Guid UserId { get; set; }
        public int Attempts { get; set; }
    }
    public async Task<int> RunAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (organizationId == Guid.Empty) return 0;
        DateTime now = clock.GetUtcNow().UtcDateTime;
        int exhausted = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "OrganizationOffboardingNotices" SET "Status"='failed',"LastErrorCode"='delivery_attempts_exhausted'
            WHERE "OrganizationId"={organizationId} AND "Status"='processing' AND "Attempts">=8 AND "AvailableAt"<={now}
            """, ct);
        if (exhausted > 0) logger.LogWarning("Offboarding organization {OrganizationId}: {Count} exhausted delivery leases need operator follow-up; deadlines were not extended", organizationId, exhausted);
        await EnsureWarningsAsync(organizationId, ct);
        int sent = 0;
        for (int batch = 0; batch < 64; batch++)
        {
            now = clock.GetUtcNow().UtcDateTime;
            List<Claim> claims = await db.Database.SqlQuery<Claim>($"""
                UPDATE "OrganizationOffboardingNotices" n SET "Status"='processing',"Attempts"=n."Attempts"+1,"AvailableAt"={now.AddMinutes(15)}
                WHERE (n."OrganizationId",n."EndDate",n."Kind",n."UserId") = (
                    SELECT "OrganizationId","EndDate","Kind","UserId" FROM "OrganizationOffboardingNotices"
                    WHERE "OrganizationId"={organizationId} AND "Status" IN ('pending','processing') AND "Attempts"<8 AND "AvailableAt"<={now}
                    ORDER BY "EndDate","Kind","UserId" LIMIT 1 FOR UPDATE SKIP LOCKED
                ) RETURNING n."EndDate",n."Kind",n."UserId",n."Attempts"
                """).ToListAsync(ct);
            if (claims.Count == 0) break;
            Claim claim = claims[0];
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'", ct);
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE \"Subscriptions\",\"OrganizationMemberships\",\"AspNetUsers\" IN SHARE ROW EXCLUSIVE MODE", ct);
            Subscription? subscription = await db.Subscriptions.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organizationId, ct);
            DateOnly? end = OrganizationRetentionClock.EndedOn(subscription);
            string? name = await db.Organizations.AsNoTracking().Where(x => x.Id == organizationId).Select(x => x.Name).SingleOrDefaultAsync(ct);
            string? recipient = await db.Users.AsNoTracking().Where(u => u.Id == claim.UserId && u.IsActive && u.EmailConfirmed &&
                (claim.Kind == 2 ? u.IsSuperAdmin : !u.IsSuperAdmin && db.OrganizationMemberships.Any(m => m.OrganizationId == organizationId && m.UserId == u.Id && m.IsActive && m.Role == UserRole.Admin))).Select(u => u.Email).SingleOrDefaultAsync(ct);
            string status = "cancelled"; string? error = null; DateTime? completed = null;
            if (end == claim.EndDate && name is not null && !string.IsNullOrWhiteSpace(recipient))
            {
                try
                {
                    if (claim.Kind == 2) await email.SendBillingSuperAdminNoticeAsync(recipient, "Offboarding: verzendfout vereist opvolging", organizationId.ToString(), $"Clubmelding na acht pogingen mislukt. Exportdeadline {claim.EndDate.AddDays(30):dd/MM/yyyy}; verwijderdatum {claim.EndDate.AddDays(90):dd/MM/yyyy}. Controleer de beheerdersadressen en verzorg tijdige handmatige opvolging. Termijnen zijn niet verlengd; verwijdering blijft geblokkeerd zolang vereiste meldingen ontbreken.", ct);
                    else await email.SendOrganizationOffboardingNoticeAsync(recipient, name, claim.EndDate, claim.Kind == 1, ct);
                    status = "completed"; completed = clock.GetUtcNow().UtcDateTime; sent++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    status = claim.Attempts >= 8 ? "failed" : "pending"; error = "smtp_delivery_failed";
                    if (status == "failed") logger.LogWarning("Offboarding organization {OrganizationId}: notice kind {Kind} failed after {Attempts} attempts; CoachOS operator must follow up before the unchanged deadline", organizationId, claim.Kind, claim.Attempts);
                }
            }
            DateTime retry = clock.GetUtcNow().UtcDateTime.AddMinutes(Math.Min(1440, Math.Pow(2, claim.Attempts)));
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE "OrganizationOffboardingNotices" SET "Status"={status},"CompletedAt"={completed},"LastErrorCode"={error},"AvailableAt"={retry}
                WHERE "OrganizationId"={organizationId} AND "EndDate"={claim.EndDate} AND "Kind"={claim.Kind} AND "UserId"={claim.UserId} AND "Attempts"={claim.Attempts} AND "Status"='processing'
                """, ct);
            await EnsureWarningsAsync(organizationId, ct);
            await tx.CommitAsync(ct);
        }
        return sent;
    }

    private async Task EnsureWarningsAsync(Guid organizationId, CancellationToken ct)
    {
        List<DateOnly> ends = await db.Database.SqlQuery<DateOnly>($"""SELECT DISTINCT "EndDate" AS "Value" FROM "OrganizationOffboardingNotices" WHERE "OrganizationId"={organizationId} AND "Kind" IN (0,1) AND "Status"='failed'""").ToListAsync(ct);
        if (ends.Count == 0) return;
        List<Guid> admins = await db.Users.AsNoTracking().Where(u => u.IsSuperAdmin && u.IsActive && u.EmailConfirmed && u.Email != null && u.Email != "").Select(u => u.Id).ToListAsync(ct);
        if (admins.Count == 0) logger.LogWarning("Offboarding organization {OrganizationId}: no verified active CoachOS superadmin mailbox exists; persistent failed notice requires manual operator follow-up", organizationId);
        foreach (DateOnly end in ends)
            foreach (Guid admin in admins)
                await db.Database.ExecuteSqlInterpolatedAsync($"""INSERT INTO "OrganizationOffboardingNotices" ("OrganizationId","EndDate","Kind","UserId") VALUES ({organizationId},{end},2,{admin}) ON CONFLICT DO NOTHING""", ct);
    }
}
