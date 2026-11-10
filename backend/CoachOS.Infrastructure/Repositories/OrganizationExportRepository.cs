using System.Reflection;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

/// <summary>
/// Clubexport. Elke tabel heeft een vaste kolomlijst: nieuwe (mogelijk gevoelige) kolommen
/// komen er pas in als iemand ze hier bewust toevoegt. Tokens, wachtwoorden en geheimen
/// staan er nooit in.
/// </summary>
public class OrganizationExportRepository(ApplicationDbContext db) : IOrganizationExportRepository
{
    public async Task<bool> IsActiveConfirmedAdminAsync(Guid organizationId, Guid userId, CancellationToken ct = default)
        => await db.OrganizationMemberships.AsNoTracking().AnyAsync(m =>
            m.OrganizationId == organizationId && m.UserId == userId && m.IsActive && m.Role == UserRole.Admin &&
            db.Users.Any(u => u.Id == userId && u.IsActive && u.EmailConfirmed && !u.IsSuperAdmin), ct);

    public async Task<IReadOnlyList<ExportTable>> GetTablesAsync(Guid organizationId, CancellationToken ct = default)
    {
        List<ExportTable> tables = [];
        tables.AddRange(await GetOrganizationTablesAsync(organizationId, ct));
        tables.AddRange(await GetLessonTablesAsync(organizationId, ct));
        tables.AddRange(await GetCampTablesAsync(organizationId, ct));
        tables.AddRange(await GetBillingTablesAsync(organizationId, ct));
        return tables;
    }

    private async Task<IReadOnlyList<ExportTable>> GetOrganizationTablesAsync(Guid org, CancellationToken ct) =>
    [
        await TableAsync("organizations", db.Organizations.Where(x => x.Id == org), "Id,CreatedAt,UpdatedAt,Name,Email,Phone,Address,City,PostalCode,Country,IsActive,LogoUrl,IsEarlyBird", ct),
        await TableAsync("clubs", db.TennisClubs.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,Name,Address", ct),
        await TableAsync("settings", db.OrganizationSettings.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,AdminsActAsTrainers,DefaultPaymentMode,PlatformFeePercentage,PaymentCurrency,YouthMaxAge,OnboardingStartedAt,OnboardingDismissedAt,TrainerModeChosenAt", ct),
        await TableAsync("memberships", db.OrganizationMemberships.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,UserId,OrganizationId,Role,IsActive,WeeklyCapacityHours,Notes,JoinedAt", ct),
        await TableAsync("head-trainers", db.HeadTrainerClubs.Where(x => x.Membership.OrganizationId == org && x.TennisClub.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationMembershipId,TennisClubId", ct),
        await TableAsync("trainer-availability", db.TrainerAvailabilities.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,TrainerId,TennisClubId,DayOfWeek,StartTime,EndTime,IsActive", ct),
        await TableAsync("contacts", db.Users.Where(u => db.OrganizationMemberships.Any(m => m.OrganizationId == org && m.UserId == u.Id)), "Id,FirstName,LastName,Email,PhoneNumber", ct),
    ];

    private async Task<IReadOnlyList<ExportTable>> GetLessonTablesAsync(Guid org, CancellationToken ct) =>
    [
        await TableAsync("lesson-series", db.LessonSeries.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,Name,Description,Level,Price,StartDate,EndDate,RegistrationDeadline,IsActive,MaxRegistrations,MinAge,MaxAge,PlanningStatus,PaymentMode,AllowSoloEnrollment,AllowGroupEnrollment,AcceptOnlinePayment,AcceptManualPayment,TennisClubId", ct),
        await TableAsync("lesson-prices", db.LessonSeriePrices.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonSerieId,Label,Description,TotalPrice,SortOrder,ReusableKey", ct),
        await TableAsync("weekly-templates", db.WeeklyTemplateEntries.Where(x => x.LessonSerie.OrganizationId == org), "Id,CreatedAt,UpdatedAt,LessonSerieId,DayOfWeek,TrainerId,CourtName,StartTime,EndTime,MaxStudents", ct),
        await TableAsync("lessons", db.Lessons.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonSerieId,TennisClubId,Date,Level,Notes,IsCancelled,CancellationReason,RescheduledToLessonId,WeeklyTemplateEntryId,TrainerId,CourtName,StartTime,EndTime,MaxStudents", ct),
        await TableAsync("enrollments", db.Enrollments.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,StudentName,ContactEmail,StudentEmail,StudentPhone,DateOfBirth,Category,LessonId,LessonSerieId,Status,EnrolledAt,Notes,EnrollmentGroupId,IsOpenToGrouping,SelectedPriceOptionId", ct),
        await TableAsync("enrollment-groups", db.EnrollmentGroups.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonSerieId,Name,LeaderEnrollmentId", ct),
        await TableAsync("preferences", db.TimeSlotPreferences.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,EnrollmentId,WeeklyTemplateEntryId,Preference", ct),
        await TableAsync("planning", db.ScheduleAssignments.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonSerieId,WeeklyTemplateEntryId,EnrollmentGroupId,EnrollmentId,Status,IsAutoMerged,IsLocked", ct),
        await TableAsync("reschedules", db.RescheduleRequests.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,EnrollmentId,EnrollmentGroupId,ScheduleAssignmentId,AlternativeWeeklyTemplateEntryId,Reason,Status,ResolvedAt,ResolvedByUserId,ResolverNote", ct),
        await TableAsync("invitations", db.LessonInvitations.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonId,Email,FirstName,Status,InvitationSentAt,RespondedAt", ct),
        await TableAsync("forms", db.EnrollmentForms.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonSerieId", ct),
        await TableAsync("form-fields", db.FormFields.Where(x => x.EnrollmentForm.OrganizationId == org), "Id,CreatedAt,UpdatedAt,EnrollmentFormId,Label,Type,IsRequired,Order,Options", ct),
        await TableAsync("form-answers", db.FormResponses.Where(x => x.Enrollment.OrganizationId == org && x.FormField.EnrollmentForm.OrganizationId == org), "Id,CreatedAt,UpdatedAt,EnrollmentId,FormFieldId,Value", ct),
    ];

    private async Task<IReadOnlyList<ExportTable>> GetCampTablesAsync(Guid org, CancellationToken ct) =>
    [
        await TableAsync("camps", db.Camps.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,TennisClubId,Level,Name,Description,Price,StartDate,EndDate,RegistrationDeadline,MaxParticipants,IsActive", ct),
        await TableAsync("camp-prices", db.CampPrices.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,CampId,Label,Description,Amount,SortOrder", ct),
        await TableAsync("camp-days", db.CampDays.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,CampId,OrganizationId,Date,StartTime,EndTime", ct),
        await TableAsync("camp-trainers", db.CampDayTrainers.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,CampDayId,OrganizationId,TrainerId,StartTime,EndTime", ct),
        await TableAsync("camp-enrollments", db.CampEnrollments.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,CampId,ParticipantName,ParticipantEmail,ParticipantPhone,Status,EnrolledAt,Notes,CampEnrollmentGroupId,CampPriceId", ct),
        await TableAsync("camp-groups", db.CampEnrollmentGroups.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,CampId,Name,LeaderEnrollmentId", ct),
        await TableAsync("camp-forms", db.CampEnrollmentForms.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,CampId", ct),
        await TableAsync("camp-fields", db.CampFormFields.Where(x => x.CampEnrollmentForm.OrganizationId == org), "Id,CreatedAt,UpdatedAt,CampEnrollmentFormId,Label,Type,IsRequired,Order,Options", ct),
        await TableAsync("camp-answers", db.CampFormResponses.Where(x => x.CampEnrollment.OrganizationId == org && x.CampFormField.CampEnrollmentForm.OrganizationId == org), "Id,CreatedAt,UpdatedAt,CampEnrollmentId,CampFormFieldId,Value", ct),
    ];

    private async Task<IReadOnlyList<ExportTable>> GetBillingTablesAsync(Guid org, CancellationToken ct) =>
    [
        await TableAsync("payments", db.Payments.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,EnrollmentId,CampEnrollmentId,Amount,Status,Method,MolliePaymentId,PaidAt,Description,Currency,PlatformFee,FailureReason", ct),
        await TableAsync("subscriptions", db.Subscriptions.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,Plan,Status,Interval,StartDate,EndDate,TrialEndsAt,IsActive,Price,PaymentMethod,CurrentPeriodStart,CurrentPeriodEnd,CancelAt,IsDeal,StudentLimit,OverageMarginPercent,OveragePricePerStudent", ct),
        await TableAsync("subscription-payments", db.SubscriptionPayments.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,SubscriptionId,Kind,Method,Status,Plan,Interval,PeriodStart,PeriodEnd,AmountExclVat,VatRate,AmountInclVat,MolliePaymentId,PaidAt,DueDate,InvoiceStatus,InvoiceId,InvoiceNumber,InvoiceMarkedPaid", ct),
        await TableAsync("billing-profiles", db.BillingProfiles.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,Name,Street,PostalCode,City,Country,InvoiceEmail,VatNumber,VatVerified", ct),
        await TableAsync("terms-acceptances", db.TermsAcceptances.Where(x => x.OrganizationId == org), "Id,CreatedAt,UpdatedAt,OrganizationId,UserId,DocumentType,Version,AcceptedAt", ct),
    ];

    private static async Task<ExportTable> TableAsync<T>(string name, IQueryable<T> query, string columnList, CancellationToken ct)
        where T : class
    {
        string[] columns = columnList.Split(',');
        PropertyInfo[] properties = columns
            .Select(c => typeof(T).GetProperty(c) ?? throw new InvalidOperationException($"Onbekende exportkolom {typeof(T).Name}.{c}"))
            .ToArray();
        List<T> records = await query.AsNoTracking().ToListAsync(ct);
        List<IReadOnlyList<object?>> rows = records
            .Select(record => (IReadOnlyList<object?>)properties.Select(p => FormatValue(p.GetValue(record))).ToArray())
            .ToList();
        return new ExportTable(name, columns, rows);
    }

    /// <summary>Enums als naam (leesbaarder dan het getal in CSV/JSON).</summary>
    private static object? FormatValue(object? value) => value is Enum e ? e.ToString() : value;
}
