using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.TrainerAvailabilities;

public interface ITrainerAvailabilityService
{
    Task<Result<List<TrainerAvailabilityDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateTrainerAvailabilityRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid id, Guid organizationId, CreateTrainerAvailabilityRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    // Eigen beschikbaarheid van de ingelogde trainer: de trainer komt uit het token, nooit uit de aanvraag.
    Task<Result<List<TrainerAvailabilityDto>>> GetForTrainerAsync(Guid organizationId, Guid trainerId, CancellationToken ct = default);
    Task<Result<Guid>> CreateForTrainerAsync(Guid organizationId, Guid trainerId, SaveOwnTrainerAvailabilityRequest request, CancellationToken ct = default);
    Task<Result> UpdateForTrainerAsync(Guid id, Guid organizationId, Guid trainerId, SaveOwnTrainerAvailabilityRequest request, CancellationToken ct = default);
    Task<Result> DeleteForTrainerAsync(Guid id, Guid organizationId, Guid trainerId, CancellationToken ct = default);
}
