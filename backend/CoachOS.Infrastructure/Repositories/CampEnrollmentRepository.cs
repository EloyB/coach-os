using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class CampEnrollmentRepository(ApplicationDbContext context) : ICampEnrollmentRepository
{
    public async Task<CampEnrollment?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.CampEnrollments
            .FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == organizationId, ct);
    }

    public async Task<CampEnrollment?> GetByIdWithGroupAsync(Guid id, CancellationToken ct = default)
    {
        // Publieke webhook-context heeft geen tenant; tracked omdat de caller zowel
        // het betaalbedrag leest als de status muteert.
        return await context.CampEnrollments
            .IgnoreQueryFilters()
            .Include(e => e.Group!)
                .ThenInclude(g => g.Members)
            .Include(e => e.Camp)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<int> CountActiveByCampAsync(Guid campId, CancellationToken ct = default)
    {
        return await context.CampEnrollments
            .AsNoTracking()
            .CountAsync(e =>
                e.CampId == campId &&
                (e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.PendingPayment), ct);
    }

    public async Task<Dictionary<Guid, int>> CountActiveByCampsAsync(
        IEnumerable<Guid> campIds, CancellationToken ct = default)
    {
        List<Guid> ids = campIds.ToList();
        return await context.CampEnrollments
            .AsNoTracking()
            .Where(e =>
                ids.Contains(e.CampId) &&
                (e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.PendingPayment))
            .GroupBy(e => e.CampId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);
    }

    public async Task<bool> IsDuplicateAsync(
        Guid campId, string participantEmail, CancellationToken ct = default)
    {
        string normalized = participantEmail.Trim().ToLower();
        return await context.CampEnrollments
            .AsNoTracking()
            .AnyAsync(e =>
                e.CampId == campId &&
                e.ParticipantEmail.ToLower() == normalized &&
                (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.PendingPayment), ct);
    }

    public async Task<bool> IsDuplicateExceptAsync(
        Guid campId, string participantEmail, Guid exceptEnrollmentId, CancellationToken ct = default)
    {
        string normalized = participantEmail.Trim().ToLower();
        return await context.CampEnrollments
            .AsNoTracking()
            .AnyAsync(e =>
                e.CampId == campId &&
                e.Id != exceptEnrollmentId &&
                e.ParticipantEmail.ToLower() == normalized &&
                (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.PendingPayment), ct);
    }

    public async Task<CampEnrollment?> GetByIdWithResponsesAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        return await context.CampEnrollments
            .Include(e => e.FormResponses)
            .FirstOrDefaultAsync(e => e.Id == id && e.OrganizationId == organizationId, ct);
    }

    public void RemoveFormResponse(CampFormResponse response)
    {
        context.CampFormResponses.Remove(response);
    }

    public async Task<int> CountActiveByCampGroupsAsync(
        Guid campId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.CampEnrollmentGroups
            .AsNoTracking()
            .CountAsync(g => g.CampId == campId && g.OrganizationId == organizationId, ct);
    }

    public async Task<List<CampEnrollment>> GetByCampWithResponsesAsync(
        Guid campId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.CampEnrollments
            .AsNoTracking()
            .Include(e => e.FormResponses)
                .ThenInclude(r => r.CampFormField)
            .Include(e => e.Group)
            .Include(e => e.Price)
            .Include(e => e.Package)
            .Where(e => e.CampId == campId && e.OrganizationId == organizationId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);
    }

    public async Task<CampEnrollmentGroup?> GetGroupWithMembersAsync(
        Guid groupId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.CampEnrollmentGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.OrganizationId == organizationId, ct);
    }

    public async Task<bool> AnyUsingPricesAsync(IEnumerable<Guid> priceIds, CancellationToken ct = default)
    {
        List<Guid> ids = priceIds.ToList();
        return ids.Count > 0
            && await context.CampEnrollments.AnyAsync(e => e.CampPriceId != null && ids.Contains(e.CampPriceId.Value), ct);
    }

    public async Task<bool> AnyUsingPackagesAsync(IEnumerable<Guid> packageIds, CancellationToken ct = default)
    {
        List<Guid> ids = packageIds.ToList();
        return ids.Count > 0
            && await context.CampEnrollments.AnyAsync(e => e.CampPackageId != null && ids.Contains(e.CampPackageId.Value), ct);
    }

    public async Task<List<CampEnrollment>> GetActiveByCampAsync(Guid campId, CancellationToken ct = default)
    {
        return await context.CampEnrollments
            .AsNoTracking()
            .Where(e =>
                e.CampId == campId &&
                (e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.PendingPayment))
            .ToListAsync(ct);
    }

    public async Task AddAsync(CampEnrollment enrollment, CancellationToken ct = default)
    {
        await context.CampEnrollments.AddAsync(enrollment, ct);
    }

    public async Task AddGroupAsync(CampEnrollmentGroup group, CancellationToken ct = default)
    {
        await context.CampEnrollmentGroups.AddAsync(group, ct);
    }

    public async Task AddFormResponseAsync(CampFormResponse response, CancellationToken ct = default)
    {
        await context.CampFormResponses.AddAsync(response, ct);
    }
}
