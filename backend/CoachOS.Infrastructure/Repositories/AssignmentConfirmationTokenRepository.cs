using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class AssignmentConfirmationTokenRepository(ApplicationDbContext context)
    : IAssignmentConfirmationTokenRepository
{
    public async Task<AssignmentConfirmationToken?> GetByTokenHashAsync(
        string tokenHash, CancellationToken ct = default)
    {
        return await context.AssignmentConfirmationTokens
            .Include(t => t.ScheduleAssignment).ThenInclude(a => a.Enrollment)
            .Include(t => t.ScheduleAssignment).ThenInclude(a => a.EnrollmentGroup!).ThenInclude(g => g.Members)
            .Include(t => t.Enrollment)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
    }

    public async Task<List<AssignmentConfirmationToken>> GetBySeriesAsync(
        Guid lessonSerieId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.AssignmentConfirmationTokens
            .Include(t => t.ScheduleAssignment).ThenInclude(a => a.Enrollment)
            .Include(t => t.ScheduleAssignment).ThenInclude(a => a.EnrollmentGroup!).ThenInclude(g => g.Members)
            .Include(t => t.Enrollment)
            .Where(t => t.OrganizationId == organizationId
                && t.ScheduleAssignment.LessonSerieId == lessonSerieId)
            .ToListAsync(ct);
    }

    public async Task AddRangeAsync(
        IEnumerable<AssignmentConfirmationToken> tokens, CancellationToken ct = default)
    {
        await context.AssignmentConfirmationTokens.AddRangeAsync(tokens, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
