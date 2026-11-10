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
    private const string AnonymizedName = "Geanonimiseerd";
    private const string AnonymizedPaymentDescription = "Inschrijving (geanonimiseerd)";

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

    public async Task<ErasureDeleteOutcome> AnonymizeAsync(
        Guid organizationId, PlayerErasurePlan plan, DateTime anonymizedAt, CancellationToken ct = default)
    {
        // Enkel voor een inschrijving buiten een groep: groepen met betalingen blokkeren altijd.
        if (plan.GroupId is not null || plan.GroupEffect != ErasureGroupEffect.None)
            throw new InvalidOperationException("Anonimiseren van een groepslid wordt niet ondersteund.");
        try
        {
            if (plan.Kind == PrivacyErasureKind.CampEnrollment) await AnonymizeCampEnrollmentAsync(organizationId, plan.EnrollmentId, anonymizedAt, ct);
            else await AnonymizeLessonEnrollmentAsync(organizationId, plan.EnrollmentId, anonymizedAt, ct);
            return ErasureDeleteOutcome.Deleted;
        }
        catch (Exception ex) when (HasSqlState(ex, PostgresErrorCodes.ForeignKeyViolation))
        {
            return ErasureDeleteOutcome.DependencyConflict;
        }
    }

    public async Task<IReadOnlyList<Guid>> GetOrganizationIdsAsync(CancellationToken ct = default)
        => await db.Organizations.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(ct);

    /// <summary>Uniek per rij: unieke indexen op (kamp, e-mail) mogen niet botsen tussen geanonimiseerde rijen.</summary>
    private static string AnonymizedEmail(Guid id) => $"geanonimiseerd-{id:N}@anonymized.invalid";

    private async Task AnonymizeLessonEnrollmentAsync(Guid organizationId, Guid id, DateTime anonymizedAt, CancellationToken ct)
    {
        await DeleteLessonSideRecordsAsync(organizationId, id, null, removeGroup: false, ct);
        string email = AnonymizedEmail(id);
        await db.Enrollments.Where(x => x.OrganizationId == organizationId && x.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.StudentName, AnonymizedName)
                .SetProperty(x => x.ContactEmail, email)
                .SetProperty(x => x.StudentEmail, (string?)null)
                .SetProperty(x => x.StudentPhone, (string?)null)
                .SetProperty(x => x.DateOfBirth, (DateOnly?)null)
                .SetProperty(x => x.Notes, (string?)null)
                .SetProperty(x => x.AnonymizedAt, anonymizedAt)
                .SetProperty(x => x.UpdatedAt, anonymizedAt), ct);
        // De Mollie-omschrijving bevat de namen van de deelnemers.
        await db.Payments.Where(x => x.OrganizationId == organizationId && x.EnrollmentId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Description, AnonymizedPaymentDescription)
                .SetProperty(x => x.UpdatedAt, anonymizedAt), ct);
    }

    private async Task AnonymizeCampEnrollmentAsync(Guid organizationId, Guid id, DateTime anonymizedAt, CancellationToken ct)
    {
        await DeleteCampSideRecordsAsync(organizationId, id, ct);
        string email = AnonymizedEmail(id);
        await db.CampEnrollments.Where(x => x.OrganizationId == organizationId && x.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ParticipantName, AnonymizedName)
                .SetProperty(x => x.ParticipantEmail, email)
                .SetProperty(x => x.ParticipantPhone, (string?)null)
                .SetProperty(x => x.Notes, (string?)null)
                .SetProperty(x => x.AnonymizedAt, anonymizedAt)
                .SetProperty(x => x.UpdatedAt, anonymizedAt), ct);
        await db.Payments.Where(x => x.OrganizationId == organizationId && x.CampEnrollmentId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Description, AnonymizedPaymentDescription)
                .SetProperty(x => x.UpdatedAt, anonymizedAt), ct);
    }

    private async Task DeleteLessonEnrollmentAsync(Guid organizationId, PlayerErasurePlan plan, CancellationToken ct)
    {
        Guid id = plan.EnrollmentId;
        Guid? groupId = plan.GroupId;
        bool removeGroup = plan.GroupEffect == ErasureGroupEffect.RemoveEmptyGroup;
        if (plan.GroupEffect == ErasureGroupEffect.TransferLeader)
            await db.EnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaderEnrollmentId, plan.NewGroupLeaderId!.Value)
                    .SetProperty(x => x.Name, NeutralGroupName), ct);

        await DeleteLessonSideRecordsAsync(organizationId, id, groupId, removeGroup, ct);

        if (removeGroup)
        {
            // Circulaire FK (groep → leider → groep): eerst losmaken, dan de lege groep wissen.
            await db.Enrollments.Where(x => x.OrganizationId == organizationId && x.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrollmentGroupId, (Guid?)null), ct);
            await db.EnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId).ExecuteDeleteAsync(ct);
        }
        await db.Enrollments.Where(x => x.OrganizationId == organizationId && x.Id == id).ExecuteDeleteAsync(ct);
    }

    /// <summary>Persoonlijke nevenrecords van een lesinschrijving (gedeeld door wissen en anonimiseren).</summary>
    private async Task DeleteLessonSideRecordsAsync(
        Guid organizationId, Guid id, Guid? groupId, bool removeGroup, CancellationToken ct)
    {
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
    }

    private async Task DeleteCampEnrollmentAsync(Guid organizationId, PlayerErasurePlan plan, CancellationToken ct)
    {
        Guid id = plan.EnrollmentId;
        Guid? groupId = plan.GroupId;
        if (plan.GroupEffect == ErasureGroupEffect.TransferLeader)
            await db.CampEnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaderEnrollmentId, plan.NewGroupLeaderId!.Value)
                    .SetProperty(x => x.Name, NeutralGroupName), ct);

        await DeleteCampSideRecordsAsync(organizationId, id, ct);

        if (plan.GroupEffect == ErasureGroupEffect.RemoveEmptyGroup)
        {
            await db.CampEnrollments.Where(x => x.OrganizationId == organizationId && x.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CampEnrollmentGroupId, (Guid?)null), ct);
            await db.CampEnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId).ExecuteDeleteAsync(ct);
        }
        await db.CampEnrollments.Where(x => x.OrganizationId == organizationId && x.Id == id).ExecuteDeleteAsync(ct);
    }

    private async Task DeleteCampSideRecordsAsync(Guid organizationId, Guid id, CancellationToken ct)
    {
        await db.CampFormResponses.Where(x => x.CampEnrollmentId == id && x.CampEnrollment.OrganizationId == organizationId &&
            x.CampFormField.CampEnrollmentForm.OrganizationId == organizationId).ExecuteDeleteAsync(ct);
        await DeleteCompletedMailAsync(organizationId, id, ct);
    }

    private async Task DeleteCompletedMailAsync(Guid organizationId, Guid enrollmentId, CancellationToken ct)
        => await db.EmailOutboxMessages.Where(x => x.OrganizationId == organizationId &&
            x.EnrollmentId == enrollmentId && x.Status == EmailOutboxStatuses.Completed).ExecuteDeleteAsync(ct);
}
