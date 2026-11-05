namespace CoachOS.Application.Privacy;

public sealed record PlayerErasureRequest(bool Apply = false, bool Early = false, string? PreviewHash = null, string? Confirmation = null, bool VerifiedRequest = false, Guid? NewGroupLeaderId = null);
public sealed record PlayerErasureResult(bool Eligible, bool Deleted, string? PreviewHash, string[] Holds, int PersonalRecords = 0);

public interface IPlayerErasureService
{
    Task<PlayerErasureResult> ProcessAsync(Guid organizationId, Guid enrollmentId, bool camp, PlayerErasureRequest request, CancellationToken ct = default, Guid? actorId = null);
}
