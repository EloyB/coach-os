using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Models;
using CoachOS.Application.Trainers.Queries.GetTrainers;

namespace CoachOS.Application.Trainers;

public interface ITrainerService
{
    Task<Result<Guid>> InviteAsync(
        Guid organizationId,
        string firstName,
        string lastName,
        string email,
        string inviteBaseUrl,
        CancellationToken ct = default);

    Task<Result<AuthResponseDto>> AcceptInviteAsync(
        string token,
        string password,
        CancellationToken ct = default);

    Task<Result<List<TrainerDto>>> GetTrainersAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<Result> DeactivateAsync(
        Guid trainerId,
        Guid organizationId,
        CancellationToken ct = default);

    Task<Result> RemoveAsync(
        Guid trainerId,
        Guid organizationId,
        CancellationToken ct = default);

    Task<Result> ReassignSeriesAsync(
        Guid fromTrainerId,
        Guid toTrainerId,
        Guid organizationId,
        CancellationToken ct = default);
}
