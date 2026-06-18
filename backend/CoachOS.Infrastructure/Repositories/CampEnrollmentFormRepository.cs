using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class CampEnrollmentFormRepository(ApplicationDbContext context) : ICampEnrollmentFormRepository
{
    public async Task<CampEnrollmentForm?> GetByCampIdWithFieldsAsync(Guid campId, CancellationToken ct = default)
    {
        return await context.CampEnrollmentForms
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.CampId == campId, ct);
    }

    public async Task<CampEnrollmentForm?> GetByCampIdReadOnlyAsync(Guid campId, CancellationToken ct = default)
    {
        return await context.CampEnrollmentForms
            .AsNoTracking()
            .Include(f => f.Fields.OrderBy(ff => ff.Order))
            .FirstOrDefaultAsync(f => f.CampId == campId, ct);
    }

    public async Task AddAsync(CampEnrollmentForm form, CancellationToken ct = default)
    {
        await context.CampEnrollmentForms.AddAsync(form, ct);
    }

    public void RemoveField(CampFormField field)
    {
        context.CampFormFields.Remove(field);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
