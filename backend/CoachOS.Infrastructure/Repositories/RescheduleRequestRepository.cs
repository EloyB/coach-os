using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class RescheduleRequestRepository(ApplicationDbContext context) : IRescheduleRequestRepository
{
    public async Task<RescheduleRequest?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.RescheduleRequests
            .Include(r => r.Enrollment)
            .Include(r => r.ScheduleAssignment)
                .ThenInclude(a => a.WeeklyTemplateEntry)
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == organizationId, ct);
    }

    public async Task<List<RescheduleRequest>> GetPendingByOrganizationAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        return await context.RescheduleRequests
            .AsNoTracking()
            .Include(r => r.Enrollment)
            .Include(r => r.ScheduleAssignment)
                .ThenInclude(a => a.WeeklyTemplateEntry)
            .Where(r => r.OrganizationId == organizationId && r.Status == RescheduleRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> CountPendingByOrganizationAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        return await context.RescheduleRequests
            .AsNoTracking()
            .CountAsync(r => r.OrganizationId == organizationId && r.Status == RescheduleRequestStatus.Pending, ct);
    }

    public async Task<bool> HasPendingForAssignmentAsync(
        Guid assignmentId, CancellationToken ct = default)
    {
        return await context.RescheduleRequests
            .AsNoTracking()
            .AnyAsync(r => r.ScheduleAssignmentId == assignmentId && r.Status == RescheduleRequestStatus.Pending, ct);
    }

    public async Task AddAsync(RescheduleRequest request, CancellationToken ct = default)
    {
        await context.RescheduleRequests.AddAsync(request, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
