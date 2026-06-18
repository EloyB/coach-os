using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.TrainerAvailabilities;

public interface ITrainerAvailabilityService
{
    Task<Result<List<TrainerAvailabilityDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateTrainerAvailabilityRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
}
