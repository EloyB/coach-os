using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Identity;

public class UserLookupService(ApplicationDbContext context) : IUserLookupService
{
    public async Task<Dictionary<Guid, string>> GetUserNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        List<Guid> idList = ids.ToList();
        return await context.Users
            .AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => (u.FirstName + " " + u.LastName).Trim(), ct);
    }

    public async Task<string?> GetUserNameByIdAsync(Guid id, CancellationToken ct = default)
    {
        string? result = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => u.FirstName + " " + u.LastName)
            .FirstOrDefaultAsync(ct);

        return result?.Trim();
    }

    public async Task<List<(Guid Id, string FullName)>> GetOrganizationMembersAsync(Guid organizationId, CancellationToken ct = default)
    {
        List<ApplicationUser> users = await context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == organizationId && u.IsActive && u.Role == UserRole.Trainer)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(ct);

        return users.Select(u => (u.Id, (u.FirstName + " " + u.LastName).Trim())).ToList();
    }

    public async Task<bool> IsActiveTrainerAsync(Guid trainerId, Guid organizationId, CancellationToken ct = default)
    {
        return await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == trainerId
                && u.OrganizationId == organizationId
                && u.Role == UserRole.Trainer
                && u.IsActive, ct);
    }

    public async Task<Dictionary<Guid, (string FullName, string Email)>> GetUserNamesAndEmailsByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        List<Guid> idList = ids.ToList();
        List<ApplicationUser> users = await _context.Users
            .AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .ToListAsync(ct);

        return users.ToDictionary(
            u => u.Id,
            u => ((u.FirstName + " " + u.LastName).Trim(), u.Email ?? string.Empty));
    }

    public async Task<(string FullName, string Email)?> GetUserInfoByIdAsync(Guid id, CancellationToken ct = default)
    {
        ApplicationUser? user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return null;

        return ((user.FirstName + " " + user.LastName).Trim(), user.Email ?? string.Empty);
    }
}
