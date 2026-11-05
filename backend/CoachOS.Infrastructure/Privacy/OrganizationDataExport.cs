using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Common;
using CoachOS.Domain.Subscriptions;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Privacy;

/// <summary>Consistent tenant database export. Fixed column lists fail closed for future schema additions.</summary>
public sealed class OrganizationDataExport(ApplicationDbContext db, TimeProvider clock) : CoachOS.Application.Privacy.IOrganizationDataExport
{
    public async Task<byte[]?> ExportAsync(Guid organizationId, Guid actorId, CancellationToken ct = default)
    {
        if (organizationId == Guid.Empty || actorId == Guid.Empty) return null;
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        bool authorized = await db.OrganizationMemberships.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.UserId == actorId &&
            x.IsActive && x.Role == UserRole.Admin && db.Users.Any(u => u.Id == actorId && u.IsActive && u.EmailConfirmed && !u.IsSuperAdmin), ct);
        if (!authorized) return null;
        DateOnly? ended = OrganizationRetentionClock.EndedOn(await db.Subscriptions.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organizationId, ct));
        if (ended is not null && clock.GetBrusselsToday() > ended.Value.AddDays(30)) return null;
        using MemoryStream output = new();
        using (ZipArchive zip = new(output, ZipArchiveMode.Create, true))
        {
            Add(zip, "organizations", await db.Organizations.AsNoTracking().Where(x => x.Id == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,Name,Email,Phone,Address,City,PostalCode,Country,IsActive,LogoUrl,IsEarlyBird");
            Add(zip, "clubs", await db.TennisClubs.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,Name,Address");
            Add(zip, "lesson-series", await db.LessonSeries.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,Name,Description,Level,Price,StartDate,EndDate,RegistrationDeadline,IsActive,MaxRegistrations,MinAge,MaxAge,PlanningStatus,PaymentMode,AllowSoloEnrollment,AllowGroupEnrollment,AcceptOnlinePayment,AcceptManualPayment,TennisClubId");
            Add(zip, "lesson-prices", await db.LessonSeriePrices.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonSerieId,Label,Description,TotalPrice,SortOrder,ReusableKey");
            Add(zip, "lessons", await db.Lessons.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonSerieId,TennisClubId,Date,Level,Notes,IsCancelled,CancellationReason,RescheduledToLessonId,WeeklyTemplateEntryId,TrainerId,CourtName,StartTime,EndTime,MaxStudents");
            Add(zip, "enrollments", await db.Enrollments.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,StudentName,ContactEmail,StudentEmail,StudentPhone,DateOfBirth,Category,LessonId,LessonSerieId,Status,EnrolledAt,Notes,EnrollmentGroupId,IsOpenToGrouping,SelectedPriceOptionId");
            Add(zip, "enrollment-groups", await db.EnrollmentGroups.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonSerieId,Name,LeaderEnrollmentId");
            Add(zip, "preferences", await db.TimeSlotPreferences.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,EnrollmentId,WeeklyTemplateEntryId,Preference");
            Add(zip, "planning", await db.ScheduleAssignments.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonSerieId,WeeklyTemplateEntryId,EnrollmentGroupId,EnrollmentId,Status,IsAutoMerged,IsLocked");
            Add(zip, "weekly-templates", await db.WeeklyTemplateEntries.AsNoTracking().Where(x => x.LessonSerie.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,LessonSerieId,DayOfWeek,TrainerId,CourtName,StartTime,EndTime,MaxStudents");
            Add(zip, "reschedules", await db.RescheduleRequests.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,EnrollmentId,EnrollmentGroupId,ScheduleAssignmentId,AlternativeWeeklyTemplateEntryId,Reason,Status,ResolvedAt,ResolvedByUserId,ResolverNote");
            Add(zip, "invitations", await db.LessonInvitations.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonId,Email,FirstName,Status,InvitationSentAt,RespondedAt");
            Add(zip, "forms", await db.EnrollmentForms.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,LessonSerieId");
            Add(zip, "form-fields", await db.FormFields.AsNoTracking().Where(x => x.EnrollmentForm.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,EnrollmentFormId,Label,Type,IsRequired,Order,Options");
            Add(zip, "form-answers", await db.FormResponses.AsNoTracking().Where(x => x.Enrollment.OrganizationId == organizationId && x.FormField.EnrollmentForm.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,EnrollmentId,FormFieldId,Value");
            Add(zip, "camps", await db.Camps.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,TennisClubId,Level,Name,Description,Price,StartDate,EndDate,RegistrationDeadline,MaxParticipants,IsActive");
            Add(zip, "camp-prices", await db.CampPrices.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,CampId,Label,Description,Amount,SortOrder");
            Add(zip, "camp-days", await db.CampDays.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,CampId,OrganizationId,Date,StartTime,EndTime");
            Add(zip, "camp-trainers", await db.CampDayTrainers.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,CampDayId,OrganizationId,TrainerId,StartTime,EndTime");
            Add(zip, "camp-enrollments", await db.CampEnrollments.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,CampId,ParticipantName,ParticipantEmail,ParticipantPhone,Status,EnrolledAt,Notes,CampEnrollmentGroupId,CampPriceId");
            Add(zip, "camp-groups", await db.CampEnrollmentGroups.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,CampId,Name,LeaderEnrollmentId");
            Add(zip, "camp-forms", await db.CampEnrollmentForms.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,CampId");
            Add(zip, "camp-fields", await db.CampFormFields.AsNoTracking().Where(x => x.CampEnrollmentForm.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,CampEnrollmentFormId,Label,Type,IsRequired,Order,Options");
            Add(zip, "camp-answers", await db.CampFormResponses.AsNoTracking().Where(x => x.CampEnrollment.OrganizationId == organizationId && x.CampFormField.CampEnrollmentForm.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,CampEnrollmentId,CampFormFieldId,Value");
            Add(zip, "payments", await db.Payments.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,EnrollmentId,CampEnrollmentId,Amount,Status,Method,MolliePaymentId,PaidAt,Description,Currency,PlatformFee,FailureReason");
            Add(zip, "subscriptions", await db.Subscriptions.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,Plan,Status,Interval,StartDate,EndDate,TrialEndsAt,LastTrialReminderDay,IsActive,Price,PaymentMethod,CurrentPeriodStart,CurrentPeriodEnd,CancelAt,IsDeal,StudentLimit,OverageMarginPercent,OveragePricePerStudent");
            Add(zip, "subscription-payments", await db.SubscriptionPayments.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,SubscriptionId,Kind,Method,Status,Plan,Interval,PeriodStart,PeriodEnd,AmountExclVat,VatRate,AmountInclVat,MolliePaymentId,PaidAt,DueDate,InvoiceStatus,InvoiceId,InvoiceNumber,InvoiceMarkedPaid");
            Add(zip, "billing-profiles", await db.BillingProfiles.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,Name,Street,PostalCode,City,Country,InvoiceEmail,VatNumber,VatVerified");
            Add(zip, "terms-acceptances", await db.TermsAcceptances.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,UserId,DocumentType,Version,AcceptedAt");
            Add(zip, "memberships", await db.OrganizationMemberships.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,UserId,OrganizationId,Role,IsActive,WeeklyCapacityHours,Notes,JoinedAt");
            Add(zip, "head-trainers", await db.HeadTrainerClubs.AsNoTracking().Where(x => x.Membership.OrganizationId == organizationId && x.TennisClub.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationMembershipId,TennisClubId");
            Add(zip, "trainer-availability", await db.TrainerAvailabilities.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,TrainerId,TennisClubId,DayOfWeek,StartTime,EndTime,IsActive");
            Add(zip, "settings", await db.OrganizationSettings.AsNoTracking().Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Id).ToListAsync(ct), "Id,CreatedAt,UpdatedAt,OrganizationId,AdminsActAsTrainers,DefaultPaymentMode,PlatformFeePercentage,PaymentCurrency,YouthMaxAge,OnboardingStartedAt,OnboardingDismissedAt,TrainerModeChosenAt");
            Add(zip, "contacts", await db.Users.AsNoTracking().Where(u => db.OrganizationMemberships.Any(m => m.OrganizationId == organizationId && m.UserId == u.Id)).OrderBy(u => u.Id).ToListAsync(ct), "Id,FirstName,LastName,Email,PhoneNumber");
            Write(zip, "manifest.json", JsonSerializer.Serialize(new
            {
                organizationId,
                formatVersion = 1,
                scope = "current tenant database rows, explicit safe columns; no credentials or cross-club memberships",
                invoiceArtifacts = "Invoice references/status are included. PDF/attachment bytes are not stored by this schema; external provider retrieval/export must be verified separately. This is not proof of a complete Billit artifact archive.",
                exclusions = new[] { "authentication/capability tokens", "passwords/security stamps", "OAuth secrets", "internal delivery payloads", "other organizations" }
            }));
        }
        await transaction.CommitAsync(ct);
        return output.ToArray();
    }

    private static void Add<T>(ZipArchive zip, string name, IReadOnlyCollection<T> records, string columnList)
    {
        string[] columns = columnList.Split(',');
        System.Reflection.PropertyInfo[] properties = columns.Select(c => typeof(T).GetProperty(c) ?? throw new InvalidOperationException("Unknown export column")).ToArray();
        List<Dictionary<string, object?>> rows = records.Select(row => properties.ToDictionary(p => p.Name, p => p.GetValue(row))).ToList();
        Write(zip, name + ".json", JsonSerializer.Serialize(rows));
        StringBuilder csv = new(); csv.AppendLine(string.Join(',', columns.Select(Csv)));
        foreach (Dictionary<string, object?> row in rows)
            csv.AppendLine(string.Join(',', columns.Select(column => Csv(Scalar(row[column])))));
        Write(zip, name + ".csv", "\uFEFF" + csv);
    }

    private static string Scalar(object? value) => value switch
    {
        null => "",
        string text => text,
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable formatted => formatted.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };

    private static string Csv(string value)
    {
        string trimmed = value.TrimStart();
        if ((trimmed.Length > 0 && "=+-@".Contains(trimmed[0])) || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    private static void Write(ZipArchive zip, string name, string text)
    {
        using StreamWriter writer = new(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(text);
    }
}
