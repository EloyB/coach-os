using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Privacy;

/// <summary>Handmatige verwijdering van één (kamp)inschrijving door een clubbeheerder.</summary>
public interface IPlayerErasureService
{
    /// <summary>Leest alleen; neemt geen locks.</summary>
    Task<Result<PlayerErasurePreviewDto>> PreviewAsync(
        Guid organizationId, Guid actorUserId, PrivacyErasureKind kind, Guid enrollmentId, CancellationToken ct = default);

    Task<Result<PlayerErasureResultDto>> EraseAsync(
        Guid organizationId, Guid actorUserId, PrivacyErasureKind kind, Guid enrollmentId,
        ErasePlayerRequest request, CancellationToken ct = default);
}
