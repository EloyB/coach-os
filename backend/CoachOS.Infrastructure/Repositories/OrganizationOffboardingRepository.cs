using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

/// <summary>
/// Verwijdering van één organisatie. Elke query scopet expliciet op de organisatie en negeert de
/// tenant query filter (de superadmin werkt cross-org). Volgorde volgt de foreign keys (allemaal
/// Restrict): kinderen eerst, organisatie als laatste.
/// </summary>
public class OrganizationOffboardingRepository(ApplicationDbContext db) : IOrganizationOffboardingRepository
{
    public async Task<OrganizationDataCounts> CountDataAsync(Guid organizationId, CancellationToken ct = default)
    {
        Guid org = organizationId;
        return new OrganizationDataCounts(
            TennisClubs: await db.TennisClubs.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == org, ct),
            LessonSeries: await db.LessonSeries.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == org, ct),
            Lessons: await db.Lessons.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == org, ct),
            Enrollments: await db.Enrollments.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == org, ct),
            Camps: await db.Camps.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == org, ct),
            CampEnrollments: await db.CampEnrollments.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == org, ct),
            Payments: await db.Payments.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == org, ct),
            Memberships: await db.OrganizationMemberships.CountAsync(x => x.OrganizationId == org, ct),
            AccountsToDelete: await RemovableAccountIds(org).CountAsync(ct),
            SubscriptionPayments: await db.SubscriptionPayments.IgnoreQueryFilters().CountAsync(x => x.OrganizationId == org, ct));
    }

    public async Task LockOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("LockOrganizationAsync vereist een actieve transactie.");

        // Advisory lock per organisatie: twee verwijderingen van dezelfde organisatie lopen na
        // elkaar, andere organisaties worden niet geblokkeerd. Geen LOCK TABLE.
        string lockKey = $"coachos:organization-offboarding:{organizationId:N}";
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);

        // Rijlocks: een gelijktijdige heractivatie (abonnement) wacht op ons, en nieuwe rijen die
        // naar deze organisatie verwijzen (FK-check = KEY SHARE) ook. Daarna lezen we de actuele toestand.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM "Organizations" WHERE "Id" = {organizationId} FOR UPDATE""", ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM "Subscriptions" WHERE "OrganizationId" = {organizationId} FOR UPDATE""", ct);
    }

    public async Task<int> DeleteOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        // Vóór de lidmaatschappen weg zijn bepalen welke accounts enkel bij deze organisatie hoorden.
        List<Guid> removableAccounts = await RemovableAccountIds(organizationId).ToListAsync(ct);

        await BreakCircularReferencesAsync(organizationId, ct);
        await DeleteParticipationAsync(organizationId, ct);
        await DeleteCatalogAsync(organizationId, ct);
        await DeleteOrganizationRowsAsync(organizationId, ct);

        // Dubbele check: enkel accounts die nu nergens meer lid zijn en geen superadmin zijn.
        return await db.Users
            .Where(u => removableAccounts.Contains(u.Id) && !u.IsSuperAdmin &&
                        !db.OrganizationMemberships.Any(m => m.UserId == u.Id))
            .ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Accounts met een (niet-leerling-)lidmaatschap bij deze organisatie en geen enkel spoor bij
    /// een andere organisatie: geen lidmaatschap en geen trainerrol in lessen, sjablonen,
    /// beschikbaarheden of kampdagen. Superadmins nooit.
    /// </summary>
    private IQueryable<Guid> RemovableAccountIds(Guid org) => db.Users
        .Where(u => !u.IsSuperAdmin &&
            db.OrganizationMemberships.Any(m => m.OrganizationId == org && m.UserId == u.Id && m.Role != UserRole.Student) &&
            !db.OrganizationMemberships.Any(m => m.OrganizationId != org && m.UserId == u.Id) &&
            !db.Lessons.IgnoreQueryFilters().Any(l => l.OrganizationId != org && l.TrainerId == u.Id) &&
            !db.WeeklyTemplateEntries.IgnoreQueryFilters().Any(w => w.LessonSerie.OrganizationId != org && w.TrainerId == u.Id) &&
            !db.TrainerAvailabilities.IgnoreQueryFilters().Any(a => a.OrganizationId != org && a.TrainerId == u.Id) &&
            !db.CampDayTrainers.IgnoreQueryFilters().Any(c => c.OrganizationId != org && c.TrainerId == u.Id))
        .Select(u => u.Id);

    private async Task BreakCircularReferencesAsync(Guid org, CancellationToken ct)
    {
        await db.Enrollments.IgnoreQueryFilters().Where(x => x.OrganizationId == org)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrollmentGroupId, (Guid?)null), ct);
        await db.CampEnrollments.IgnoreQueryFilters().Where(x => x.OrganizationId == org)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.CampEnrollmentGroupId, (Guid?)null), ct);
        await db.Lessons.IgnoreQueryFilters().Where(x => x.OrganizationId == org)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RescheduledToLessonId, (Guid?)null), ct);
    }

    private async Task DeleteParticipationAsync(Guid org, CancellationToken ct)
    {
        await db.EmailOutboxMessages.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.AssignmentConfirmationTokens.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.RescheduleRequests.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.ScheduleAssignments.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.TimeSlotPreferences.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.FormResponses.IgnoreQueryFilters().Where(x => x.Enrollment.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.CampFormResponses.IgnoreQueryFilters().Where(x => x.CampEnrollment.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.Payments.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.EnrollmentGroups.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.CampEnrollmentGroups.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.Enrollments.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.CampEnrollments.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.LessonInvitations.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
    }

    private async Task DeleteCatalogAsync(Guid org, CancellationToken ct)
    {
        await db.FormFields.IgnoreQueryFilters().Where(x => x.EnrollmentForm.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.CampFormFields.IgnoreQueryFilters().Where(x => x.CampEnrollmentForm.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.EnrollmentForms.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.CampEnrollmentForms.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.CampDayTrainers.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.CampDays.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.CampPrices.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.Camps.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.Lessons.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.WeeklyTemplateEntries.IgnoreQueryFilters().Where(x => x.LessonSerie.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.LessonSeriePrices.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.LessonSeries.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.HeadTrainerClubs.IgnoreQueryFilters().Where(x => x.Membership.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.TrainerAvailabilities.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.TennisClubs.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Abonnementsbetalingen (facturen) worden bewust niet verwijderd: bestaan ze toch, dan faalt
    /// het verwijderen van het abonnement op de foreign key en rolt alles terug.
    /// </summary>
    private async Task DeleteOrganizationRowsAsync(Guid org, CancellationToken ct)
    {
        await db.OAuthStates.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.MollieConnections.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.OrganizationSettings.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.BillingProfiles.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.TermsAcceptances.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.Subscriptions.IgnoreQueryFilters().Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.OrganizationMemberships.Where(x => x.OrganizationId == org).ExecuteDeleteAsync(ct);
        await db.Organizations.Where(x => x.Id == org).ExecuteDeleteAsync(ct);
    }
}
