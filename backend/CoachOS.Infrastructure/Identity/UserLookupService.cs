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
}
