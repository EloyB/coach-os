using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
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

    public async Task<bool> TryClaimResponseAsync(
        Guid tokenId,
        ConfirmationResponse target,
        DateTime now,
        CancellationToken ct = default)
    {
        var affected = await context.AssignmentConfirmationTokens
            .Where(t => t.Id == tokenId && t.Response == ConfirmationResponse.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Response, target)
                .SetProperty(t => t.RespondedAt, now), ct);
        return affected == 1;
    }

    public async Task<bool> TryTransitionResponseAsync(
        Guid tokenId,
        ConfirmationResponse from,
        ConfirmationResponse to,
        DateTime now,
        CancellationToken ct = default)
    {
        var affected = await context.AssignmentConfirmationTokens
            .Where(t => t.Id == tokenId && t.Response == from)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Response, to)
                .SetProperty(t => t.RespondedAt, now), ct);
        return affected == 1;
    }
}
