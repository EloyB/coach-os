using CoachOS.Application.Auth.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(
        string organizationName,
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponseDto>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Wissel naar een andere organisatie waar de user lid van is. Retourneert nieuwe JWT.</summary>
    Task<Result<AuthResponseDto>> SwitchOrganizationAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>Lijst alle (actieve) memberships van de user — voor de FE org-switcher.</summary>
    Task<Result<List<OrganizationMembershipDto>>> GetMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
