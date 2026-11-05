using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoachOS.Application.Privacy;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoachOS.Infrastructure.Privacy;

/// <summary>Actual relational deletion; financial evidence is an explicit review hold.</summary>
public sealed class PlayerErasureService(ApplicationDbContext db) : IPlayerErasureService
{
    public async Task<PlayerErasureResult> ProcessAsync(Guid organizationId, Guid enrollmentId, bool camp,
        PlayerErasureRequest request, CancellationToken ct = default, Guid? actorId = null)
    {
        if (organizationId == Guid.Empty) return new(false, false, null, ["not_found"]);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // An ordinary enrollment/payment writer need not know a new advisory lock.
            // A bounded table-write lock makes eligibility + FK cleanup one serial action,
            // including concurrent new participation, payment, grouping and rescheduling.
            await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'", ct);
            await db.Database.ExecuteSqlRawAsync("""
                LOCK TABLE "Enrollments", "CampEnrollments", "Payments", "LessonSeries", "Lessons", "Camps",
                "CampDays", "EnrollmentGroups", "CampEnrollmentGroups", "ScheduleAssignments", "RescheduleRequests",
                "EmailOutboxMessages", "FormResponses", "CampFormResponses", "TimeSlotPreferences", "AssignmentConfirmationTokens",
                "OrganizationMemberships", "AspNetUsers"
                IN SHARE ROW EXCLUSIVE MODE
                """, ct);
            if (actorId is not null && !await db.OrganizationMemberships.AsNoTracking().AnyAsync(x =>
                x.OrganizationId == organizationId && x.UserId == actorId && x.IsActive && x.Role == UserRole.Admin &&
                db.Users.Any(u => u.Id == x.UserId && u.IsActive && u.EmailConfirmed && !u.IsSuperAdmin), ct))
                return new(false, false, null, ["forbidden"]);
            Enrollment? enrollment = camp ? null : await db.Enrollments.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == enrollmentId, ct);
            CampEnrollment? campEnrollment = camp ? await db.CampEnrollments.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == enrollmentId, ct) : null;
            if (enrollment is null && campEnrollment is null) return new(false, false, null, ["not_found"]);
            string name = enrollment?.StudentName ?? campEnrollment!.ParticipantName;
            // A lesson's parent contact is not necessarily the participating player.
            string email = enrollment is not null
                ? (string.IsNullOrWhiteSpace(enrollment.StudentEmail) ? enrollment.ContactEmail : enrollment.StudentEmail)
                : campEnrollment!.ParticipantEmail;
            string normalizedName = name.Trim().ToLowerInvariant();
            string normalizedEmail = email.Trim().ToLowerInvariant();
            DateOnly? birth = enrollment?.DateOfBirth;
            List<string> holds = [];
            if (enrollment?.PrivacyHold == true || campEnrollment?.PrivacyHold == true) holds.Add("privacy_hold_review_required");
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            List<Enrollment> related = await db.Enrollments.AsNoTracking().Where(x => x.OrganizationId == organizationId &&
                x.StudentName.Trim().ToLower() == normalizedName &&
                (birth != null ? x.DateOfBirth == birth :
                    (x.StudentEmail != null && x.StudentEmail.Trim() != ""
                        ? x.StudentEmail.Trim().ToLower() : x.ContactEmail.Trim().ToLower()) == normalizedEmail)).ToListAsync(ct);
            List<CampEnrollment> relatedCamps = await db.CampEnrollments.AsNoTracking().Where(x => x.OrganizationId == organizationId &&
                x.ParticipantName.Trim().ToLower() == normalizedName && x.ParticipantEmail.Trim().ToLower() == normalizedEmail).ToListAsync(ct);
            List<Guid> relatedIds = related.Select(x => x.Id).ToList();
            List<Guid> relatedCampIds = relatedCamps.Select(x => x.Id).ToList();
            List<Guid> seriesIds = related.Where(x => x.LessonSerieId != null).Select(x => x.LessonSerieId!.Value).ToList();
            List<Guid> lessonIds = related.Where(x => x.LessonId != null).Select(x => x.LessonId!.Value).ToList();
            List<DateOnly> participation = await db.LessonSeries.AsNoTracking().Where(x => x.OrganizationId == organizationId && seriesIds.Contains(x.Id)).Select(x => x.EndDate).ToListAsync(ct);
            List<Lesson> participationLessons = await db.Lessons.AsNoTracking().Where(x => x.OrganizationId == organizationId &&
                (lessonIds.Contains(x.Id) || (x.LessonSerieId != null && seriesIds.Contains(x.LessonSerieId.Value)))).OrderBy(x => x.Id).ToListAsync(ct);
            participation.AddRange(participationLessons.Select(x => x.Date));
            HashSet<Guid> visitedLessons = participationLessons.Select(x => x.Id).ToHashSet();
            List<Guid> replacements = participationLessons.Where(x => x.RescheduledToLessonId != null).Select(x => x.RescheduledToLessonId!.Value).Distinct().ToList();
            int depth = 0;
            while (replacements.Count > 0 && depth++ < 32)
            {
                replacements = replacements.Where(id => !visitedLessons.Contains(id)).ToList();
                if (replacements.Count == 0) break; // dates of already visited replacements are already included
                List<Lesson> next = await db.Lessons.AsNoTracking().Where(x => x.OrganizationId == organizationId && replacements.Contains(x.Id)).OrderBy(x => x.Id).ToListAsync(ct);
                if (next.Count != replacements.Count) { holds.Add("participation_chain_review_required"); break; }
                participation.AddRange(next.Select(x => x.Date));
                foreach (Lesson lesson in next) visitedLessons.Add(lesson.Id);
                replacements = next.Where(x => x.RescheduledToLessonId != null).Select(x => x.RescheduledToLessonId!.Value).Distinct().ToList();
            }
            if (replacements.Count > 0 && depth >= 32) holds.Add("participation_chain_review_required");
            List<Guid> campIds = relatedCamps.Select(x => x.CampId).ToList();
            participation.AddRange(await db.Camps.AsNoTracking().Where(x => x.OrganizationId == organizationId && campIds.Contains(x.Id)).Select(x => x.EndDate).ToListAsync(ct));
            participation.AddRange(await db.CampDays.AsNoTracking().Where(x => x.Camp.OrganizationId == organizationId && campIds.Contains(x.CampId)).Select(x => x.Date).ToListAsync(ct));
            DateOnly? last = participation.Count == 0 ? null : participation.Max();
            if (last is null) holds.Add("participation_review_required");
            else if (last >= today) holds.Add("active_or_future_participation");
            else if (!request.Early && last.Value.AddMonths(24) > today) holds.Add("retention_not_due");
            bool priced = camp
                ? await db.Camps.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.Id == campEnrollment!.CampId &&
                    (x.Price > 0 || x.Prices.Any(p => p.OrganizationId == organizationId && p.Amount > 0)), ct)
                : await db.LessonSeries.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId &&
                    (x.Id == enrollment!.LessonSerieId || db.Lessons.Any(l => l.OrganizationId == organizationId && l.Id == enrollment.LessonId && l.LessonSerieId == x.Id)) &&
                    (x.Price > 0 || x.Prices.Any(p => p.OrganizationId == organizationId && p.TotalPrice > 0)), ct);
            if (priced || enrollment?.Status == EnrollmentStatus.PendingPayment || campEnrollment?.Status == EnrollmentStatus.PendingPayment ||
                await db.Payments.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId &&
                (camp ? x.CampEnrollmentId == enrollmentId : x.EnrollmentId == enrollmentId), ct)) holds.Add("financial_review_required");
            if (await db.Payments.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.Status == PaymentStatus.Pending &&
                ((x.EnrollmentId != null && relatedIds.Contains(x.EnrollmentId.Value)) || (x.CampEnrollmentId != null && relatedCampIds.Contains(x.CampEnrollmentId.Value))), ct)) holds.Add("open_payment");
            Guid? groupId = enrollment?.EnrollmentGroupId ?? campEnrollment?.CampEnrollmentGroupId;
            var group = camp
                ? await db.CampEnrollmentGroups.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.Id == groupId).Select(x => new { x.Id, x.LeaderEnrollmentId, x.Name, x.UpdatedAt }).SingleOrDefaultAsync(ct)
                : await db.EnrollmentGroups.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.Id == groupId).Select(x => new { x.Id, x.LeaderEnrollmentId, x.Name, x.UpdatedAt }).SingleOrDefaultAsync(ct);
            List<Guid> groupMembers = group is null ? [] : camp
                ? await db.CampEnrollments.Where(x => x.OrganizationId == organizationId && x.CampEnrollmentGroupId == group.Id).OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(ct)
                : await db.Enrollments.Where(x => x.OrganizationId == organizationId && x.EnrollmentGroupId == group.Id).OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(ct);
            bool orphanLeader = groupId is null && (camp
                ? await db.CampEnrollmentGroups.AnyAsync(x => x.OrganizationId == organizationId && x.LeaderEnrollmentId == enrollmentId, ct)
                : await db.EnrollmentGroups.AnyAsync(x => x.OrganizationId == organizationId && x.LeaderEnrollmentId == enrollmentId, ct));
            if (orphanLeader || (groupId is not null && (group is null || !groupMembers.Contains(group.LeaderEnrollmentId)))) holds.Add("group_review_required");
            bool transfer = group?.LeaderEnrollmentId == enrollmentId && groupMembers.Count > 1;
            bool emptyGroup = group is not null && groupMembers.Count == 1 && group.LeaderEnrollmentId == enrollmentId;
            if (transfer && (request.NewGroupLeaderId == enrollmentId || request.NewGroupLeaderId is null || !groupMembers.Contains(request.NewGroupLeaderId.Value))) holds.Add("group_leader_transfer_required");
            if (group is not null && await db.Payments.AnyAsync(x => x.OrganizationId == organizationId &&
                (camp ? x.CampEnrollmentId != null && groupMembers.Contains(x.CampEnrollmentId.Value) : x.EnrollmentId != null && groupMembers.Contains(x.EnrollmentId.Value)), ct)) holds.Add("financial_group_review_required");
            if (!camp && await db.EmailOutboxMessages.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.EnrollmentId == enrollmentId && x.Status != EmailOutboxStatuses.Completed, ct)) holds.Add("delivery_pending_or_failed");
            IQueryable<Guid> assignmentIds = db.ScheduleAssignments.Where(x => x.OrganizationId == organizationId &&
                (x.EnrollmentId == enrollmentId || (emptyGroup && x.EnrollmentGroupId == groupId))).Select(x => x.Id);
            if (!camp && await db.RescheduleRequests.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId &&
                (x.EnrollmentId == enrollmentId || assignmentIds.Contains(x.ScheduleAssignmentId) || (emptyGroup && x.EnrollmentGroupId == groupId)) && x.ResolvedAt == null, ct)) holds.Add("unresolved_request");
            int answers = camp
                ? await db.CampFormResponses.CountAsync(x => x.CampEnrollmentId == enrollmentId && x.CampEnrollment.OrganizationId == organizationId && x.CampFormField.CampEnrollmentForm.OrganizationId == organizationId, ct)
                : await db.FormResponses.CountAsync(x => x.EnrollmentId == enrollmentId && x.Enrollment.OrganizationId == organizationId && x.FormField.EnrollmentForm.OrganizationId == organizationId, ct);
            string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                organizationId, enrollmentId, camp, request.Early, request.NewGroupLeaderId, enrollment, campEnrollment,
                related = related.OrderBy(x => x.Id), relatedCamps = relatedCamps.OrderBy(x => x.Id), group, groupMembers, last, answers, holds,
            }))));
            if (holds.Count > 0 || !request.Apply) return new(holds.Count == 0, false, hash, holds.ToArray(), answers + 1);
            if (!request.VerifiedRequest || request.Confirmation != name || request.PreviewHash != hash)
                return new(false, false, hash, ["verified_request_and_current_preview_confirmation_required"], answers + 1);
            if (transfer)
            {
                if (camp) await db.CampEnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId).ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaderEnrollmentId, request.NewGroupLeaderId!.Value).SetProperty(x => x.Name, "Groep"), ct);
                else await db.EnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId).ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaderEnrollmentId, request.NewGroupLeaderId!.Value).SetProperty(x => x.Name, "Groep"), ct);
            }
            if (camp)
            {
                await db.CampFormResponses.Where(x => x.CampEnrollmentId == enrollmentId && x.CampEnrollment.OrganizationId == organizationId && x.CampFormField.CampEnrollmentForm.OrganizationId == organizationId).ExecuteDeleteAsync(ct);
                if (emptyGroup)
                {
                    await db.CampEnrollments.Where(x => x.OrganizationId == organizationId && x.Id == enrollmentId).ExecuteUpdateAsync(s => s.SetProperty(x => x.CampEnrollmentGroupId, (Guid?)null), ct);
                    await db.CampEnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId).ExecuteDeleteAsync(ct);
                }
                await db.CampEnrollments.Where(x => x.OrganizationId == organizationId && x.Id == enrollmentId).ExecuteDeleteAsync(ct);
            }
            else
            {
                await db.FormResponses.Where(x => x.EnrollmentId == enrollmentId && x.Enrollment.OrganizationId == organizationId && x.FormField.EnrollmentForm.OrganizationId == organizationId).ExecuteDeleteAsync(ct);
                await db.TimeSlotPreferences.Where(x => x.OrganizationId == organizationId && x.EnrollmentId == enrollmentId).ExecuteDeleteAsync(ct);
                await db.RescheduleRequests.Where(x => x.OrganizationId == organizationId && (x.EnrollmentId == enrollmentId || assignmentIds.Contains(x.ScheduleAssignmentId) || (emptyGroup && x.EnrollmentGroupId == groupId))).ExecuteDeleteAsync(ct);
                await db.AssignmentConfirmationTokens.Where(x => x.OrganizationId == organizationId && (x.EnrollmentId == enrollmentId || assignmentIds.Contains(x.ScheduleAssignmentId))).ExecuteDeleteAsync(ct);
                await db.ScheduleAssignments.Where(x => x.OrganizationId == organizationId && (x.EnrollmentId == enrollmentId || (emptyGroup && x.EnrollmentGroupId == groupId))).ExecuteDeleteAsync(ct);
                await db.EmailOutboxMessages.Where(x => x.OrganizationId == organizationId && x.EnrollmentId == enrollmentId && x.Status == EmailOutboxStatuses.Completed).ExecuteDeleteAsync(ct);
                if (emptyGroup)
                {
                    await db.Enrollments.Where(x => x.OrganizationId == organizationId && x.Id == enrollmentId).ExecuteUpdateAsync(s => s.SetProperty(x => x.EnrollmentGroupId, (Guid?)null), ct);
                    await db.EnrollmentGroups.Where(x => x.OrganizationId == organizationId && x.Id == groupId).ExecuteDeleteAsync(ct);
                }
                await db.Enrollments.Where(x => x.OrganizationId == organizationId && x.Id == enrollmentId).ExecuteDeleteAsync(ct);
            }
            string kind = camp ? "camp_enrollment" : "enrollment";
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "PrivacyErasureReceipts" ("OrganizationId","TargetKind","TargetId","CompletedAt")
                VALUES ({organizationId},{kind},{enrollmentId},{DateTime.UtcNow})
                ON CONFLICT ("OrganizationId","TargetKind","TargetId") DO NOTHING
                """, ct);
            await transaction.CommitAsync(ct);
            return new(true, true, hash, [], answers + 1);
        }
        catch (NpgsqlException)
        {
            await transaction.RollbackAsync(ct);
            return new(false, false, null, ["concurrent_change_or_dependency_review_required"]);
        }
    }
}
