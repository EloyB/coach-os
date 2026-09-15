using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Planning;

public interface ITrainerPlanningService
{
    Task<Result<List<TrainerPlanningDto>>> GetAllAsync(
        Guid organizationId,
        CancellationToken ct = default);
}
