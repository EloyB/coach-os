using CoachOS.Application.StandaloneLessons.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.StandaloneLessons;

/// <summary>
/// Publieke flow voor uitnodigingen: deelnemers openen de link uit hun mail
/// en bevestigen / weigeren. Geen auth — uitsluitend via raw token.
/// </summary>
public interface IInvitationPublicService
{
    Task<Result<PublicInvitationDto>> GetByTokenAsync(string rawToken, CancellationToken ct = default);
    Task<Result<PublicInvitationDto>> AcceptAsync(string rawToken, CancellationToken ct = default);
    Task<Result<PublicInvitationDto>> DeclineAsync(string rawToken, CancellationToken ct = default);
}
