using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Trainers.DTOs;
using CoachOS.Domain.Enums;
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
        bool asAdmin = false,
        CancellationToken ct = default);

    Task<Result<AuthResponseDto>> AcceptInviteAsync(
        string token,
        string password,
        CancellationToken ct = default);

    Task<Result<List<TrainerDto>>> GetTrainersAsync(
        Guid organizationId,
        bool includeAllAdmins = false,
        CancellationToken ct = default);

    /// <summary>
    /// Wijzigt de rol van een lid binnen de organisatie (Trainer ↔ Admin). Een extra Admin
    /// ("deputy") heeft dezelfde rechten als de oorspronkelijke Admin. De laatste actieve Admin
    /// kan niet gedemoot worden. Een rolwijziging werkt pas bij het volgende token (login/refresh).
    /// </summary>
    Task<Result> SetRoleAsync(
        Guid memberId,
        Guid organizationId,
        UserRole newRole,
        CancellationToken ct = default);

    Task<Result> UpdateAsync(
        Guid trainerId,
        Guid organizationId,
        UpdateTrainerRequest request,
        CancellationToken ct = default);

    Task<Result> SetHeadTrainerClubsAsync(
        Guid trainerId,
        Guid organizationId,
        IReadOnlyList<Guid> clubIds,
        CancellationToken ct = default);

    Task<Result> ResendInviteAsync(
        Guid trainerId,
        Guid organizationId,
        string inviteBaseUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Trekt het lidmaatschap in. Werkt ook voor Admins, met twee guards: je kan jezelf niet
    /// deactiveren en de laatste actieve Admin blijft altijd behouden.
    /// </summary>
    Task<Result> DeactivateAsync(
        Guid trainerId,
        Guid organizationId,
        Guid actingUserId,
        CancellationToken ct = default);

    /// <summary>Verwijdert het lid. Zelfde guards als <see cref="DeactivateAsync"/>.</summary>
    Task<Result> RemoveAsync(
        Guid trainerId,
        Guid organizationId,
        Guid actingUserId,
        CancellationToken ct = default);

    Task<Result> ReassignSeriesAsync(
        Guid fromTrainerId,
        Guid toTrainerId,
        Guid organizationId,
        CancellationToken ct = default);
}
