using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Trainers.DTOs;
using CoachOS.Domain.Models;

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

    Task<Result> ResendInviteAsync(
        Guid trainerId,
        Guid organizationId,
        string inviteBaseUrl,
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

    /// <summary>
    /// Markeert het Admin-membership van de huidige user als IsTrainer = true,
    /// zodat de admin in de trainerlijst en lesson-pickers verschijnt zonder
    /// een aparte invite-flow te hoeven doorlopen. Idempotent.
    /// </summary>
    Task<Result<TrainerDto>> AddSelfAsTrainerAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken ct = default);

    /// <summary>
    /// Verwijdert de admin uit de trainerlijst (IsTrainer = false). Faalt als
    /// er nog lessen aan deze admin zijn toegewezen — wijs die eerst toe aan
    /// een andere trainer (zelfde regel als RemoveAsync).
    /// </summary>
    Task<Result> RemoveSelfAsTrainerAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken ct = default);
}
