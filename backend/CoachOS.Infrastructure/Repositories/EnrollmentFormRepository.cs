using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class EnrollmentFormRepository(ApplicationDbContext context) : IEnrollmentFormRepository
{
    public async Task<EnrollmentForm?> GetBySeriesIdAsync(Guid lessonSeriesId, CancellationToken ct = default)
    {
        return await context.EnrollmentForms
            .FirstOrDefaultAsync(f => f.LessonSeriesId == lessonSeriesId, ct);
    }

    public async Task<EnrollmentForm?> GetBySeriesIdWithFieldsAsync(Guid lessonSeriesId, CancellationToken ct = default)
    {
        return await context.EnrollmentForms
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.LessonSeriesId == lessonSeriesId, ct);
    }

    public async Task<EnrollmentForm?> GetBySeriesIdReadOnlyAsync(Guid lessonSeriesId, CancellationToken ct = default)
    {
        return await context.EnrollmentForms
            .AsNoTracking()
            .Include(f => f.Fields.OrderBy(ff => ff.Order))
            .FirstOrDefaultAsync(f => f.LessonSeriesId == lessonSeriesId, ct);
    }

    public async Task AddAsync(EnrollmentForm form, CancellationToken ct = default)
    {
        await context.EnrollmentForms.AddAsync(form, ct);
    }

    public void RemoveField(FormField field)
    {
        context.FormFields.Remove(field);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
