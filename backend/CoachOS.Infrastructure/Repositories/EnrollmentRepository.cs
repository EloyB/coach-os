using System.Data;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class EnrollmentRepository(ApplicationDbContext context) : IEnrollmentRepository
{
    public async Task<Enrollment?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == organizationId, ct);
    }

    public async Task<int> ReassignLessonLinkAsync(
        Guid fromLessonId, Guid toLessonId, CancellationToken ct = default)
    {
        DateTime now = DateTime.UtcNow;
        return await context.Enrollments
            .Where(e => e.LessonId == fromLessonId)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.LessonId, toLessonId)
                    .SetProperty(e => e.UpdatedAt, now),
                ct);
    }

    public async Task<List<Enrollment>> GetBySeriesAsync(
        Guid lessonSeriesId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Include(e => e.FormResponses)
                .ThenInclude(r => r.FormField)
            .Where(e => e.LessonSerieId == lessonSeriesId && e.OrganizationId == organizationId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);
    }

    public async Task<bool> IsDuplicateAsync(
        Guid lessonSeriesId, string studentEmail, CancellationToken ct = default)
    {
        var normalized = studentEmail.Trim().ToLower();
        return await context.Enrollments
            .AsNoTracking()
            .AnyAsync(e =>
                e.LessonSerieId == lessonSeriesId &&
                e.StudentEmail.ToLower() == normalized &&
                (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.PendingPayment), ct);
    }

    public async Task<int> CountActiveBySeriesAsync(Guid lessonSeriesId, CancellationToken ct = default)
    {
        return await context.Enrollments
            .AsNoTracking()
            .CountAsync(e =>
                e.LessonSerieId == lessonSeriesId &&
                (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.PendingPayment), ct);
    }

    public async Task<Dictionary<Guid, int>> CountActiveBySeriesIdsAsync(
        IEnumerable<Guid> seriesIds, CancellationToken ct = default)
    {
        List<Guid> ids = seriesIds.ToList();
        return await context.Enrollments
            .AsNoTracking()
            .Where(e =>
                e.LessonSerieId.HasValue &&
                ids.Contains(e.LessonSerieId.Value) &&
                (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.PendingPayment))
            .GroupBy(e => e.LessonSerieId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
    }

    public async Task<int> CountActiveByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await context.Enrollments
            .AsNoTracking()
            .CountAsync(e =>
                e.OrganizationId == organizationId &&
                (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.PendingPayment), ct);
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        await context.Enrollments.AddAsync(enrollment, ct);
    }

    public async Task AddFormResponseAsync(FormResponse response, CancellationToken ct = default)
    {
        await context.FormResponses.AddAsync(response, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        await context.Database.BeginTransactionAsync(ct);
    }

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default)
    {
        await context.Database.BeginTransactionAsync(isolationLevel, ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (context.Database.CurrentTransaction is not null)
            await context.Database.CommitTransactionAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (context.Database.CurrentTransaction is not null)
            await context.Database.RollbackTransactionAsync(ct);
    }
}
