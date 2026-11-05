using CoachOS.Application.Privacy;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Subscriptions;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoachOS.Infrastructure.Privacy;

public sealed class OrganizationErasureService(ApplicationDbContext db, TimeProvider clock) : IOrganizationErasureService
{
    public async Task<OrganizationErasureResult> ProcessAsync(Guid organizationId, OrganizationErasureRequest request, CancellationToken ct = default, Guid? superAdminId = null)
    {
        if (organizationId == Guid.Empty) return new(false, false, null, ["not_found"]);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'", ct);
            // Rare whole-tenant removal must serialize with ordinary writers too.
            string tables = string.Join(',', db.Model.GetEntityTypes().Select(x => x.GetTableName()).Where(x => x != null).Distinct().Order().Select(x => "\"" + x!.Replace("\"", "\"\"") + "\""));
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE " + tables + " IN SHARE ROW EXCLUSIVE MODE", ct);
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE \"OrganizationOffboardingNotices\" IN SHARE ROW EXCLUSIVE MODE", ct);
            if (superAdminId is not null && !await db.Users.AsNoTracking().AnyAsync(x => x.Id == superAdminId && x.IsSuperAdmin && x.IsActive && x.EmailConfirmed, ct))
                return new(false, false, null, ["forbidden"]);
            Organization? organization = await db.Organizations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == organizationId, ct);
            if (organization is null) return new(false, false, null, ["not_found"]);
            Subscription? subscription = await db.Subscriptions.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organizationId, ct);
            DateOnly today = clock.GetBrusselsToday(); DateOnly? end = OrganizationRetentionClock.EndedOn(subscription);
            List<string> holds = [];
            // PR290's player holds may be added before or after this independently deployable migration.
            // to_jsonb -> absent key is NULL, so no unverified schema assumption or broad deletion is needed.
            bool playerHold = await db.Database.SqlQuery<bool>($"""
                SELECT (
                    EXISTS (SELECT 1 FROM "Enrollments" e WHERE e."OrganizationId"={organizationId} AND COALESCE((to_jsonb(e)->>'PrivacyHold')::boolean,false)) OR
                    EXISTS (SELECT 1 FROM "CampEnrollments" e WHERE e."OrganizationId"={organizationId} AND COALESCE((to_jsonb(e)->>'PrivacyHold')::boolean,false))
                ) AS "Value"
                """).SingleAsync(ct);
            if (organization.PrivacyHold || playerHold) holds.Add("privacy_hold_review_required");
            if (request.Early)
            {
                if (superAdminId is null || !request.VerifiedRequest || request.RequestingAdminId is null ||
                    !await db.OrganizationMemberships.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.UserId == request.RequestingAdminId && x.IsActive && x.Role == UserRole.Admin && db.Users.Any(u => u.Id == x.UserId && u.IsActive && u.EmailConfirmed), ct))
                    holds.Add("verified_club_admin_request_required");
            }
            else if (end is null || today < end.Value.AddDays(90)) holds.Add("retention_not_due_or_reactivated");
            if (await db.LessonSeries.AnyAsync(x => x.OrganizationId == organizationId && (x.Price > 0 || x.Prices.Any(p => p.OrganizationId == organizationId && p.TotalPrice > 0)), ct) ||
                await db.Camps.AnyAsync(x => x.OrganizationId == organizationId && (x.Price > 0 || x.Prices.Any(p => p.OrganizationId == organizationId && p.Amount > 0)), ct) ||
                subscription?.Plan is not null || subscription?.Price > 0 || await db.Payments.AnyAsync(x => x.OrganizationId == organizationId, ct) ||
                await db.SubscriptionPayments.AnyAsync(x => x.OrganizationId == organizationId, ct) ||
                await db.TermsAcceptances.AnyAsync(x => x.OrganizationId == organizationId, ct)) holds.Add("financial_or_legal_evidence_review_required");
            if (await db.LessonSeries.AnyAsync(x => x.OrganizationId == organizationId && x.EndDate >= today, ct) ||
                await db.Lessons.AnyAsync(x => x.OrganizationId == organizationId && x.Date >= today, ct) ||
                await db.Camps.AnyAsync(x => x.OrganizationId == organizationId && x.EndDate >= today, ct) ||
                await db.CampDays.AnyAsync(x => x.OrganizationId == organizationId && x.Date >= today, ct)) holds.Add("active_or_future_participation");
            if (await db.EmailOutboxMessages.AnyAsync(x => x.OrganizationId == organizationId && x.Status != EmailOutboxStatuses.Completed, ct)) holds.Add("delivery_pending_or_failed");
            if (await db.RescheduleRequests.AnyAsync(x => x.OrganizationId == organizationId && x.ResolvedAt == null, ct)) holds.Add("unresolved_request");
            if (!request.Early && end is not null && today >= end.Value.AddDays(90))
            {
                List<Guid> admins = await db.OrganizationMemberships.AsNoTracking().Where(m => m.OrganizationId == organizationId && m.Role == UserRole.Admin && m.IsActive && db.Users.Any(u => u.Id == m.UserId && u.IsActive && !u.IsSuperAdmin)).Select(m => m.UserId).Distinct().ToListAsync(ct);
                bool complete = admins.Count > 0;
                foreach (Guid admin in admins)
                {
                    int count = await db.Database.SqlQuery<int>($"""
                        SELECT count(*)::int AS "Value" FROM "OrganizationOffboardingNotices"
                        WHERE "OrganizationId"={organizationId} AND "EndDate"={end.Value} AND "UserId"={admin} AND "Kind" IN (0,1) AND "Status"='completed'
                        AND ("CompletedAt" AT TIME ZONE 'Europe/Brussels')::date <= CASE WHEN "Kind"=0 THEN {end.Value} ELSE {end.Value.AddDays(83)} END
                        """).SingleAsync(ct);
                    if (count != 2) complete = false;
                }
                if (!complete) holds.Add("notice_delivery_review_required");
            }
            int enrollments = await db.Enrollments.CountAsync(x => x.OrganizationId == organizationId, ct);
            int camps = await db.CampEnrollments.CountAsync(x => x.OrganizationId == organizationId, ct);
            int memberships = await db.OrganizationMemberships.CountAsync(x => x.OrganizationId == organizationId, ct);
            string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { organization, subscription, request.Early, request.RequestingAdminId, end, enrollments, camps, memberships, holds }))));
            if (holds.Count > 0 || !request.Apply) return new(holds.Count == 0, false, hash, holds.ToArray(), enrollments, camps, memberships);
            if (request.Confirmation != organization.Name || request.PreviewHash != hash) return new(false, false, hash, ["current_preview_and_exact_name_required"]);
            List<Guid> removableAccounts = await db.Users.AsNoTracking().Where(u => !u.IsSuperAdmin &&
                db.OrganizationMemberships.Any(m => m.OrganizationId == organizationId && m.UserId == u.Id && m.Role != UserRole.Student) &&
                !db.OrganizationMemberships.Any(m => m.OrganizationId != organizationId && m.UserId == u.Id) &&
                !db.Lessons.Any(l => l.OrganizationId != organizationId && l.TrainerId == u.Id) &&
                !db.WeeklyTemplateEntries.Any(l => l.LessonSerie.OrganizationId != organizationId && l.TrainerId == u.Id) &&
                !db.TrainerAvailabilities.Any(l => l.OrganizationId != organizationId && l.TrainerId == u.Id) &&
                !db.CampDayTrainers.Any(l => l.OrganizationId != organizationId && l.TrainerId == u.Id)).Select(u => u.Id).ToListAsync(ct);
            await Sql("""UPDATE "Enrollments" SET "EnrollmentGroupId"=NULL WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""UPDATE "CampEnrollments" SET "CampEnrollmentGroupId"=NULL WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""UPDATE "Lessons" SET "RescheduledToLessonId"=NULL WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "EmailOutboxMessages" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "AssignmentConfirmationTokens" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "RescheduleRequests" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "ScheduleAssignments" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "TimeSlotPreferences" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "FormResponses" WHERE "EnrollmentId" IN (SELECT "Id" FROM "Enrollments" WHERE "OrganizationId"=@org) AND "FormFieldId" IN (SELECT f."Id" FROM "FormFields" f JOIN "EnrollmentForms" p ON p."Id"=f."EnrollmentFormId" WHERE p."OrganizationId"=@org)""", organizationId, ct);
            await Sql("""DELETE FROM "CampFormResponses" WHERE "CampEnrollmentId" IN (SELECT "Id" FROM "CampEnrollments" WHERE "OrganizationId"=@org) AND "CampFormFieldId" IN (SELECT f."Id" FROM "CampFormFields" f JOIN "CampEnrollmentForms" p ON p."Id"=f."CampEnrollmentFormId" WHERE p."OrganizationId"=@org)""", organizationId, ct);
            await Sql("""DELETE FROM "FormFields" WHERE "EnrollmentFormId" IN (SELECT "Id" FROM "EnrollmentForms" WHERE "OrganizationId"=@org)""", organizationId, ct);
            await Sql("""DELETE FROM "CampFormFields" WHERE "CampEnrollmentFormId" IN (SELECT "Id" FROM "CampEnrollmentForms" WHERE "OrganizationId"=@org)""", organizationId, ct);
            await Sql("""DELETE FROM "EnrollmentGroups" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "CampEnrollmentGroups" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "Enrollments" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "CampEnrollments" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "EnrollmentForms" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "CampEnrollmentForms" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "LessonInvitations" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "CampDayTrainers" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "CampDays" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "CampPrices" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "Camps" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "Lessons" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "WeeklyTemplateEntries" WHERE "LessonSerieId" IN (SELECT "Id" FROM "LessonSeries" WHERE "OrganizationId"=@org)""", organizationId, ct);
            await Sql("""DELETE FROM "LessonSeriePrices" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "LessonSeries" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "HeadTrainerClubs" WHERE "OrganizationMembershipId" IN (SELECT "Id" FROM "OrganizationMemberships" WHERE "OrganizationId"=@org) AND "TennisClubId" IN (SELECT "Id" FROM "TennisClubs" WHERE "OrganizationId"=@org)""", organizationId, ct);
            await Sql("""DELETE FROM "TrainerAvailabilities" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "TennisClubs" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "OAuthStates" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "MollieConnections" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "OrganizationSettings" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "BillingProfiles" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "TermsAcceptances" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "Subscriptions" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "OrganizationMemberships" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "OrganizationOffboardingNotices" WHERE "OrganizationId"=@org""", organizationId, ct);
            await Sql("""DELETE FROM "Organizations" WHERE "Id"=@org""", organizationId, ct);
            await db.Users.Where(u => removableAccounts.Contains(u.Id) && !u.IsSuperAdmin && !db.OrganizationMemberships.Any(m => m.UserId == u.Id)).ExecuteDeleteAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "PrivacyErasureReceipts" ("OrganizationId","TargetKind","TargetId","CompletedAt")
                VALUES ({organizationId},'organization',{organizationId},{clock.GetUtcNow().UtcDateTime})
                ON CONFLICT ("OrganizationId","TargetKind","TargetId") DO NOTHING
                """, ct);
            await tx.CommitAsync(ct); return new(true, true, hash, []);
        }
        catch (NpgsqlException)
        {
            await tx.RollbackAsync(ct); return new(false, false, null, ["concurrent_change_or_dependency_review_required"]);
        }
    }
    private Task<int> Sql(string sql, Guid organizationId, CancellationToken ct) => db.Database.ExecuteSqlRawAsync(sql, new object[] { new NpgsqlParameter("org", organizationId) }, ct);
}
