using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class TimeSlotPreferenceRepository(ApplicationDbContext context) : ITimeSlotPreferenceRepository
{
    public async Task<List<TimeSlotPreference>> GetByEnrollmentAsync(
        Guid enrollmentId, CancellationToken ct = default)
    {
        return await context.TimeSlotPreferences
            .AsNoTracking()
            .Where(p => p.EnrollmentId == enrollmentId)
            .ToListAsync(ct);
    }

    public async Task<List<TimeSlotPreference>> GetBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.TimeSlotPreferences
            .AsNoTracking()
            .Include(p => p.Enrollment)
            .Include(p => p.WeeklyTemplateEntry)
            .Where(p => p.OrganizationId == organizationId
                && p.Enrollment.LessonSerieId == lessonSerieId)
            .ToListAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<TimeSlotPreference> preferences, CancellationToken ct = default)
    {
        await context.TimeSlotPreferences.AddRangeAsync(preferences, ct);
    }

    public async Task RemoveByEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var existing = await context.TimeSlotPreferences
            .Where(p => p.EnrollmentId == enrollmentId)
            .ToListAsync(ct);

        context.TimeSlotPreferences.RemoveRange(existing);
    }

    public void RemoveRange(IEnumerable<TimeSlotPreference> preferences)
    {
        context.TimeSlotPreferences.RemoveRange(preferences);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
