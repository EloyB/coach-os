using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models.Privacy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoachOS.Infrastructure.Repositories;

/// <summary>
/// Schrijfkant: locks en definitieve deletes. Alles draait binnen de transactie van de service
/// (IUnitOfWork). Er worden enkel rijen van de doelinschrijving (en een lege groep) gewist.
/// </summary>
public sealed partial class PlayerErasureRepository
{
    private const string NeutralGroupName = "Groep";

    public async Task<ErasureLockOutcome> LockForErasureAsync(
        Guid organizationId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct = default)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'", ct);
            // Serialiseert verwijderingen binnen één club, zonder andere clubs of tabellen te blokkeren.
            string key = $"coachos:player-erasure:{organizationId}";
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", ct);
            // Rijlock: wacht op lopende wijzigingen aan deze inschrijving en blokkeert nieuwe
            // (incl. nieuwe betalingen die naar deze rij verwijzen) tot commit/rollback.
            List<Guid> locked = kind == PrivacyErasureKind.Enrollment
                ? await db.Database.SqlQuery<Guid>($"""
                    SELECT "Id" AS "Value" FROM "Enrollments"
                    WHERE "OrganizationId" = {organizationId} AND "Id" = {enrollmentId} FOR UPDATE
                    """).ToListAsync(ct)
                : await db.Database.SqlQuery<Guid>($"""
                    SELECT "Id" AS "Value" FROM "CampEnrollments"
                    WHERE "OrganizationId" = {organizationId} AND "Id" = {enrollmentId} FOR UPDATE
                    """).ToListAsync(ct);
            return locked.Count == 0 ? ErasureLockOutcome.NotFound : ErasureLockOutcome.Acquired;
        }
        catch (Exception ex) when (HasSqlState(ex, PostgresErrorCodes.LockNotAvailable))
        {
            return ErasureLockOutcome.LockTimeout;
        }
    }

    public async Task<ErasureDeleteOutcome> DeleteAsync(
        Guid organizationId, PlayerErasurePlan plan, CancellationToken ct = default)
    {
        try
        {
            if (plan.Kind == PrivacyErasureKind.CampEnrollment) await DeleteCampEnrollmentAsync(organizationId, plan, ct);
            else await DeleteLessonEnrollmentAsync(organizationId, plan, ct);
            return ErasureDeleteOutcome.Deleted;
        }
        catch (Exception ex) when (HasSqlState(ex, PostgresErrorCodes.ForeignKeyViolation))
        {
            return ErasureDeleteOutcome.DependencyConflict;
        }
    }

    /// <summary>Npgsql's execution strategy verpakt sommige fouten (bv. lock timeout) in een InvalidOperationException.</summary>
    private static bool HasSqlState(Exception ex, string sqlState)
        => (ex is PostgresException postgres && postgres.SqlState == sqlState)
           || ex.InnerException is not null && HasSqlState(ex.InnerException, sqlState);

    public async Task AddReceiptAsync(PrivacyErasureReceipt receipt, CancellationToken ct = default)
        => await db.PrivacyErasureReceipts.AddAsync(receipt, ct);

    private async Task DeleteLessonEnrollmentAsync(Guid organizationId, PlayerErasurePlan plan, CancellationToken ct)
    {
        Guid id = plan.EnrollmentId;
        Guid? groupId = plan.GroupId;
        bool removeGroup = plan.GroupEffect == ErasureGroupEffect.RemoveEmptyGroup;
        if (plan.GroupEffect == ErasureGroupEffect.TransferLeader)
            await db.EnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaderEnrollmentId, plan.NewGroupLeaderId!.Value)
                    .SetProperty(x => x.Name, NeutralGroupName), ct);

        IQueryable<Guid> assignmentIds = AssignmentIds(organizationId, id, groupId, removeGroup);
        await db.FormResponses.Where(x => x.EnrollmentId == id && x.Enrollment.OrganizationId == organizationId &&
            x.FormField.EnrollmentForm.OrganizationId == organizationId).ExecuteDeleteAsync(ct);
        await db.TimeSlotPreferences.Where(x => x.OrganizationId == organizationId && x.EnrollmentId == id).ExecuteDeleteAsync(ct);
        await db.RescheduleRequests.Where(x => x.OrganizationId == organizationId && (x.EnrollmentId == id ||
            assignmentIds.Contains(x.ScheduleAssignmentId) || (removeGroup && x.EnrollmentGroupId == groupId))).ExecuteDeleteAsync(ct);
        await db.AssignmentConfirmationTokens.Where(x => x.OrganizationId == organizationId &&
            (x.EnrollmentId == id || assignmentIds.Contains(x.ScheduleAssignmentId))).ExecuteDeleteAsync(ct);
        await db.ScheduleAssignments.Where(x => x.OrganizationId == organizationId &&
            (x.EnrollmentId == id || (removeGroup && x.EnrollmentGroupId == groupId))).ExecuteDeleteAsync(ct);
        await DeleteCompletedMailAsync(organizationId, id, ct);

        if (removeGroup)
        {
            // Circulaire FK (groep → leider → groep): eerst losmaken, dan de lege groep wissen.
            await db.Enrollments.Where(x => x.OrganizationId == organizationId && x.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrollmentGroupId, (Guid?)null), ct);
            await db.EnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId).ExecuteDeleteAsync(ct);
        }
        await db.Enrollments.Where(x => x.OrganizationId == organizationId && x.Id == id).ExecuteDeleteAsync(ct);
    }

    private async Task DeleteCampEnrollmentAsync(Guid organizationId, PlayerErasurePlan plan, CancellationToken ct)
    {
        Guid id = plan.EnrollmentId;
        Guid? groupId = plan.GroupId;
        if (plan.GroupEffect == ErasureGroupEffect.TransferLeader)
            await db.CampEnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaderEnrollmentId, plan.NewGroupLeaderId!.Value)
                    .SetProperty(x => x.Name, NeutralGroupName), ct);

        await db.CampFormResponses.Where(x => x.CampEnrollmentId == id && x.CampEnrollment.OrganizationId == organizationId &&
            x.CampFormField.CampEnrollmentForm.OrganizationId == organizationId).ExecuteDeleteAsync(ct);
        await DeleteCompletedMailAsync(organizationId, id, ct);

        if (plan.GroupEffect == ErasureGroupEffect.RemoveEmptyGroup)
        {
            await db.CampEnrollments.Where(x => x.OrganizationId == organizationId && x.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CampEnrollmentGroupId, (Guid?)null), ct);
            await db.CampEnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId).ExecuteDeleteAsync(ct);
        }
        await db.CampEnrollments.Where(x => x.OrganizationId == organizationId && x.Id == id).ExecuteDeleteAsync(ct);
    }

    private async Task DeleteCompletedMailAsync(Guid organizationId, Guid enrollmentId, CancellationToken ct)
        => await db.EmailOutboxMessages.Where(x => x.OrganizationId == organizationId &&
            x.EnrollmentId == enrollmentId && x.Status == EmailOutboxStatuses.Completed).ExecuteDeleteAsync(ct);
}
