using CoachOS.Application.Abstractions;
using CoachOS.Application.Mappings;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.TrainerAvailabilities;

public class TrainerAvailabilityService(
    ITrainerAvailabilityRepository repo,
    ITennisClubRepository clubRepo,
    IUnitOfWork unitOfWork,
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
        Error? error = await ValidateAsync(organizationId, request.TrainerId, request.TennisClubId, request.DayOfWeek,
            request.StartTime, request.EndTime, excludeId: null, ct);
        if (error is not null)
            return Result<Guid>.Fail(error);

        TrainerAvailability availability = mapper.ToTrainerAvailability(request, organizationId);
        await repo.AddAsync(availability, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Ok(availability.Id);
    }

    // Een tijdvak kan niet naar een andere trainer verhuizen: een afwijkend TrainerId geeft NotFound.
    public Task<Result> UpdateAsync(Guid id, Guid organizationId, CreateTrainerAvailabilityRequest request, CancellationToken ct = default)
        => UpdateForTrainerAsync(id, organizationId, request.TrainerId,
            new SaveOwnTrainerAvailabilityRequest(request.TennisClubId, request.DayOfWeek, request.StartTime, request.EndTime),
            ct);

    public async Task<Result<List<TrainerAvailabilityDto>>> GetForTrainerAsync(Guid organizationId, Guid trainerId, CancellationToken ct = default)
    {
        IReadOnlyList<TrainerAvailability> availabilities = await repo.GetByTrainerAsync(organizationId, trainerId, ct);
        return Result<List<TrainerAvailabilityDto>>.Ok(availabilities.Select(mapper.ToTrainerAvailabilityDto).ToList());
    }

    public Task<Result<Guid>> CreateForTrainerAsync(Guid organizationId, Guid trainerId, SaveOwnTrainerAvailabilityRequest request, CancellationToken ct = default)
        => CreateAsync(organizationId,
            new CreateTrainerAvailabilityRequest(trainerId, request.TennisClubId, request.DayOfWeek, request.StartTime, request.EndTime),
            ct);

    public async Task<Result> UpdateForTrainerAsync(Guid id, Guid organizationId, Guid trainerId, SaveOwnTrainerAvailabilityRequest request, CancellationToken ct = default)
    {
        TrainerAvailability? availability = await repo.GetByIdAsync(id, organizationId, ct);
        if (availability is null || availability.TrainerId != trainerId)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Beschikbaarheid niet gevonden"));

        Error? error = await ValidateAsync(organizationId, trainerId, request.TennisClubId, request.DayOfWeek,
            request.StartTime, request.EndTime, excludeId: id, ct);
        if (error is not null)
            return Result.Fail(error);

        availability.TennisClubId = request.TennisClubId;
        availability.DayOfWeek = request.DayOfWeek;
        availability.StartTime = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        availability.EndTime = TimeOnly.ParseExact(request.EndTime, "HH:mm");
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteForTrainerAsync(Guid id, Guid organizationId, Guid trainerId, CancellationToken ct = default)
    {
        TrainerAvailability? availability = await repo.GetByIdAsync(id, organizationId, ct);
        if (availability is null || availability.TrainerId != trainerId)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Beschikbaarheid niet gevonden"));

        availability.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        TrainerAvailability? availability = await repo.GetByIdAsync(id, organizationId, ct);
        if (availability is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Beschikbaarheid niet gevonden"));

        availability.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    /// <summary>Gedeelde controles voor aanmaken en wijzigen: club, actieve trainer en overlap.</summary>
    private async Task<Error?> ValidateAsync(
        Guid organizationId, Guid trainerId, Guid? tennisClubId, int dayOfWeek,
        string startTime, string endTime, Guid? excludeId, CancellationToken ct)
    {
        // Club is optioneel (null = eender welke club). Alleen valideren wanneer opgegeven.
        if (tennisClubId.HasValue)
        {
            bool clubExists = await clubRepo.ExistsAsync(tennisClubId.Value, organizationId, ct);
            if (!clubExists)
                return new Error(ErrorCodes.NotFound, "Club niet gevonden");
        }

        bool isActiveTrainer = await userLookup.IsActiveTrainerAsync(trainerId, organizationId, ct);
        if (!isActiveTrainer)
            return new Error(ErrorCodes.NotFound, "Trainer niet gevonden");

        TimeOnly start = TimeOnly.ParseExact(startTime, "HH:mm");
        TimeOnly end = TimeOnly.ParseExact(endTime, "HH:mm");

        bool overlaps = await repo.HasOverlapAsync(trainerId, organizationId, dayOfWeek, start, end, excludeId, ct);
        if (overlaps)
            return new Error(ErrorCodes.Conflict, "Deze trainer heeft op deze dag al een beschikbaarheid die overlapt");

        return null;
    }
}
