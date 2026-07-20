using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Repositories;

public class TrainerAvailabilityRepository(ApplicationDbContext db) : ITrainerAvailabilityRepository
{
    public async Task<IReadOnlyList<TrainerAvailability>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await db.TrainerAvailabilities
            .AsNoTracking()
            .Include(a => a.TennisClub)
            .Where(a => a.OrganizationId == organizationId && a.IsActive)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToListAsync(ct);

    public async Task<TrainerAvailability?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
        => await db.TrainerAvailabilities
            .FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == organizationId && a.IsActive, ct);

    public async Task<bool> HasOverlapAsync(Guid trainerId, Guid organizationId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, CancellationToken ct = default)
        => await db.TrainerAvailabilities
            .AsNoTracking()
            .AnyAsync(a => a.TrainerId == trainerId
                && a.OrganizationId == organizationId
                && a.DayOfWeek == dayOfWeek
                && a.IsActive
                && a.StartTime < endTime
                && a.EndTime > startTime, ct);

    public async Task<IReadOnlyList<TrainerAvailability>> GetAvailableTrainersAsync(
        Guid organizationId,
        Guid? tennisClubId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken ct = default)
        => await db.TrainerAvailabilities
            .AsNoTracking()
            .Include(a => a.TennisClub)
            .Where(a => a.OrganizationId == organizationId
                && a.IsActive
                && a.DayOfWeek == dayOfWeek
                && a.StartTime <= startTime
                && a.EndTime >= endTime
                && (a.TennisClubId == null || a.TennisClubId == tennisClubId))
            .OrderBy(a => a.StartTime)
            .ToListAsync(ct);

    public async Task AddAsync(TrainerAvailability availability, CancellationToken ct = default)
        => await db.TrainerAvailabilities.AddAsync(availability, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
