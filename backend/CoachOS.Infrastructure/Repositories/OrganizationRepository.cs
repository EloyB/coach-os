using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class OrganizationRepository(ApplicationDbContext context) : IOrganizationRepository
{
    public async Task<Organization?> GetByIdAsync(Guid id, bool tracked = false, CancellationToken ct = default)
    {
        IQueryable<Organization> query = context.Organizations;
        if (!tracked)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(o => o.Id == id, ct);
    }
}
