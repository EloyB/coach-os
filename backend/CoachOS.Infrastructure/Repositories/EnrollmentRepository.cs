using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class EnrollmentRepository(ApplicationDbContext context) : IEnrollmentRepository
{
    public async Task<List<Enrollment>> GetBySeriesAsync(
        Guid lessonSeriesId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Include(e => e.FormResponses)
                .ThenInclude(r => r.FormField)
            .Where(e => e.LessonSeriesId == lessonSeriesId && e.OrganizationId == organizationId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);
    }

    public async Task<bool> IsDuplicateAsync(
        Guid lessonSeriesId, string studentEmail, CancellationToken ct = default)
    {
        return await context.Enrollments
            .AsNoTracking()
            .AnyAsync(e =>
                e.LessonSeriesId == lessonSeriesId &&
                e.StudentEmail == studentEmail &&
                (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Pending), ct);
    }

    public async Task<int> CountActiveBySeriesAsync(Guid lessonSeriesId, CancellationToken ct = default)
    {
        return await context.Enrollments
            .AsNoTracking()
            .CountAsync(e =>
                e.LessonSeriesId == lessonSeriesId &&
                (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Pending), ct);
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
}
