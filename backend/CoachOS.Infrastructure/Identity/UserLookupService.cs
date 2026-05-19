using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Identity;

public class UserLookupService(
    ApplicationDbContext context,
    IOrganizationSettingsRepository settingsRepo) : IUserLookupService
{
    public async Task<Dictionary<Guid, string>> GetUserNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await context.Users
            .AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => (u.FirstName + " " + u.LastName).Trim(), ct);
    }

    public async Task<string?> GetUserNameByIdAsync(Guid id, CancellationToken ct = default)
    {
        var result = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => u.FirstName + " " + u.LastName)
            .FirstOrDefaultAsync(ct);

        return result?.Trim();
    }

    public async Task<List<(Guid Id, string FullName)>> GetOrganizationMembersAsync(Guid organizationId, CancellationToken ct = default)
    {
        // Tenant-scoped via OrganizationMembership ipv de legacy user.OrganizationId.
        // Admin telt alleen mee als trainer wanneer de org-setting AdminsActAsTrainers aanstaat.
        bool includeAdmins = await AdminsActAsTrainersAsync(organizationId, ct);

        var query =
            from m in context.OrganizationMemberships.AsNoTracking()
            join u in context.Users.AsNoTracking() on m.UserId equals u.Id
            where m.OrganizationId == organizationId
                  && m.IsActive
                  && u.IsActive
                  && (m.Role == UserRole.Trainer || (includeAdmins && m.Role == UserRole.Admin))
            orderby u.FirstName, u.LastName
            select new { u.Id, u.FirstName, u.LastName };

        var users = await query.ToListAsync(ct);
        return users.Select(u => (u.Id, (u.FirstName + " " + u.LastName).Trim())).ToList();
    }

    public async Task<bool> IsActiveTrainerAsync(Guid trainerId, Guid organizationId, CancellationToken ct = default)
    {
        // Tenant-scoped membership check. Admin telt alleen mee als trainer wanneer de
        // org-setting AdminsActAsTrainers aanstaat.
        bool includeAdmins = await AdminsActAsTrainersAsync(organizationId, ct);

        return await context.OrganizationMemberships
            .AsNoTracking()
            .AnyAsync(m => m.UserId == trainerId
                && m.OrganizationId == organizationId
                && (m.Role == UserRole.Trainer || (includeAdmins && m.Role == UserRole.Admin))
                && m.IsActive, ct);
    }

    /// <summary>
    /// Leest de AdminsActAsTrainers vlag. Bij ontbrekende settings-rij default true,
    /// zo blijft historisch gedrag bewaard tot de migratie de backfill heeft gedaan.
    /// </summary>
    private async Task<bool> AdminsActAsTrainersAsync(Guid organizationId, CancellationToken ct)
    {
        Domain.Entities.OrganizationSettings? settings =
            await settingsRepo.GetByOrganizationReadOnlyAsync(organizationId, ct);
        return settings?.AdminsActAsTrainers ?? true;
    }

    public async Task<int> CountActiveTrainersAsync(Guid organizationId, CancellationToken ct = default)
    {
        var includeAdmins = await AdminsActAsTrainersAsync(organizationId, ct);

        return await context.OrganizationMemberships
            .AsNoTracking()
            .CountAsync(m => m.OrganizationId == organizationId
                && (m.Role == UserRole.Trainer || (includeAdmins && m.Role == UserRole.Admin))
                && m.IsActive, ct);
    }

    public async Task<Dictionary<Guid, (string FullName, string Email)>> GetUserNamesAndEmailsByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var users = await context.Users
            .AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .ToListAsync(ct);

        return users.ToDictionary(
            u => u.Id,
            u => ((u.FirstName + " " + u.LastName).Trim(), u.Email ?? string.Empty));
    }

    public async Task<(string FullName, string Email)?> GetUserInfoByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return null;

        return ((user.FirstName + " " + user.LastName).Trim(), user.Email ?? string.Empty);
    }
}
