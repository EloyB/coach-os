using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Interfaces;

public interface ILessonInvitationRepository
{
    Task<LessonInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task<LessonInvitation?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    Task<IReadOnlyList<LessonInvitation>> GetByLessonAsync(
        Guid lessonId, Guid organizationId, CancellationToken ct = default);

    Task<bool> ExistsByLessonAndEmailAsync(
        Guid lessonId, string email, CancellationToken ct = default);

    Task AddAsync(LessonInvitation invitation, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<LessonInvitation> invitations, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Atomisch de Status van <c>Pending</c> naar <paramref name="target"/> flippen.
    /// Retourneert true als de update één rij raakte (pending was), anders false
    /// (al verwerkt door een parallel request). Voorkomt double-confirm / double-decline.
    /// </summary>
    Task<bool> TryClaimResponseAsync(
        Guid invitationId,
        LessonInvitationStatus target,
        DateTime now,
        CancellationToken ct = default);

    /// <summary>
    /// Verplaatst alle invitations van <paramref name="fromLessonId"/> naar
    /// <paramref name="toLessonId"/> via ExecuteUpdate. Gebruikt bij replanning.
    /// </summary>
    Task<int> ReassignToLessonAsync(
        Guid fromLessonId, Guid toLessonId, CancellationToken ct = default);
}
