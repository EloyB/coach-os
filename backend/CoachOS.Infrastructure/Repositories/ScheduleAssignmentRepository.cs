using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class ScheduleAssignmentRepository(ApplicationDbContext context) : IScheduleAssignmentRepository
{
    public async Task<List<ScheduleAssignment>> GetBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.ScheduleAssignments
            .AsNoTracking()
            .Include(a => a.WeeklyTemplateEntry)
            .Include(a => a.Enrollment)
            .Include(a => a.EnrollmentGroup)
                .ThenInclude(g => g!.Members)
            .Where(a => a.LessonSerieId == lessonSerieId && a.OrganizationId == organizationId)
            .ToListAsync(ct);
    }

    public async Task<ScheduleAssignment?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.ScheduleAssignments
            .Include(a => a.WeeklyTemplateEntry)
            .Include(a => a.Enrollment)
            .Include(a => a.EnrollmentGroup)
                .ThenInclude(g => g!.Members)
            .FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == organizationId, ct);
    }

    public async Task<List<ScheduleAssignment>> GetByStudentEmailAsync(
        string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLower();

        return await context.ScheduleAssignments
            .AsNoTracking()
            .Include(a => a.WeeklyTemplateEntry)
            .Include(a => a.LessonSerie)
            .Include(a => a.Enrollment)
            .Include(a => a.EnrollmentGroup)
                .ThenInclude(g => g!.Members)
            .Where(a => a.Status != ScheduleAssignmentStatus.Declined
                && a.LessonSerie.IsActive
                && (
                    (a.Enrollment != null && a.Enrollment.StudentEmail.ToLower() == normalized)
                    || (a.EnrollmentGroup != null
                        && a.EnrollmentGroup.Members.Any(m => m.StudentEmail.ToLower() == normalized))
                ))
            .ToListAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<ScheduleAssignment> assignments, CancellationToken ct = default)
    {
        await context.ScheduleAssignments.AddRangeAsync(assignments, ct);
    }

    public void RemoveRange(IEnumerable<ScheduleAssignment> assignments)
    {
        context.ScheduleAssignments.RemoveRange(assignments);
    }

    public async Task RemoveProposedBySeriesAsync(Guid lessonSerieId, Guid organizationId, CancellationToken ct = default)
    {
        await context.ScheduleAssignments
            .Where(a => a.LessonSerieId == lessonSerieId
                && a.OrganizationId == organizationId
                && a.Status == ScheduleAssignmentStatus.Proposed
                && !a.IsLocked)
            .ExecuteDeleteAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
