using CoachOS.Application.Mappings;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.TrainerAvailabilities;

public class TrainerAvailabilityService(
    ITrainerAvailabilityRepository repo,
    ITennisClubRepository clubRepo,
    IUserLookupService userLookup,
    ApplicationMapper mapper) : ITrainerAvailabilityService
{
    public async Task<Result<List<TrainerAvailabilityDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default)
    {
        IReadOnlyList<TrainerAvailability> availabilities = await repo.GetByOrganizationAsync(organizationId, ct);
        return Result<List<TrainerAvailabilityDto>>.Ok(availabilities.Select(mapper.ToTrainerAvailabilityDto).ToList());
    }

    public async Task<Result<Guid>> CreateAsync(Guid organizationId, CreateTrainerAvailabilityRequest request, CancellationToken ct = default)
    {
        // Club is optioneel (null = eender welke club). Alleen valideren wanneer opgegeven.
        if (request.TennisClubId.HasValue)
        {
            bool clubExists = await clubRepo.ExistsAsync(request.TennisClubId.Value, organizationId, ct);
            if (!clubExists)
                return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Club niet gevonden"));
        }

        bool isActiveTrainer = await userLookup.IsActiveTrainerAsync(request.TrainerId, organizationId, ct);
        if (!isActiveTrainer)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Trainer niet gevonden"));

        TimeOnly startTime = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        TimeOnly endTime = TimeOnly.ParseExact(request.EndTime, "HH:mm");

        bool overlaps = await repo.HasOverlapAsync(request.TrainerId, organizationId, request.DayOfWeek, startTime, endTime, ct);
        if (overlaps)
            return Result<Guid>.Fail(new Error(ErrorCodes.Conflict, "Deze trainer heeft op deze dag al een beschikbaarheid die overlapt"));

        TrainerAvailability availability = mapper.ToTrainerAvailability(request, organizationId);
        await repo.AddAsync(availability, ct);
        await repo.SaveChangesAsync(ct);
        return Result<Guid>.Ok(availability.Id);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        TrainerAvailability? availability = await repo.GetByIdAsync(id, organizationId, ct);
        if (availability is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Beschikbaarheid niet gevonden"));

        availability.IsActive = false;
        await repo.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
