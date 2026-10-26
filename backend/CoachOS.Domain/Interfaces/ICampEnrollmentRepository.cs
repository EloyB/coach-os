using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ICampEnrollmentRepository
{
    Task<CampEnrollment?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    /// <summary>Inclusief Group + Members (voor betaling: deelnemers tellen).</summary>
    Task<CampEnrollment?> GetByIdWithGroupAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deelnemers (rijen) met actieve status (Pending/Confirmed/PendingPayment) voor capaciteit.</summary>
    Task<int> CountActiveByCampAsync(Guid campId, CancellationToken ct = default);

    /// <summary>Actieve deelnemertellingen per kamp in één query (vermijdt N+1 in lijst-overzicht).</summary>
    Task<Dictionary<Guid, int>> CountActiveByCampsAsync(IEnumerable<Guid> campIds, CancellationToken ct = default);

    Task<bool> IsDuplicateAsync(Guid campId, string participantEmail, CancellationToken ct = default);

    /// <summary>Zoals <see cref="IsDuplicateAsync"/>, maar de opgegeven inschrijving telt niet mee (bewerken).</summary>
    Task<bool> IsDuplicateExceptAsync(
        Guid campId, string participantEmail, Guid exceptEnrollmentId, CancellationToken ct = default);

    /// <summary>Tracked inschrijving incl. formulierantwoorden (voor bewerken); org-gescoped.</summary>
    Task<CampEnrollment?> GetByIdWithResponsesAsync(
        Guid id, Guid organizationId, CancellationToken ct = default);

    void RemoveFormResponse(CampFormResponse response);

    Task<int> CountActiveByCampGroupsAsync(Guid campId, Guid organizationId, CancellationToken ct = default);

    /// <summary>Alle inschrijvingen van een kamp incl. FormResponses (admin-overzicht).</summary>
    Task<List<CampEnrollment>> GetByCampWithResponsesAsync(Guid campId, Guid organizationId, CancellationToken ct = default);

    /// <summary>Tracked groep incl. leden (voor annuleren); org-gescoped.</summary>
    Task<CampEnrollmentGroup?> GetGroupWithMembersAsync(Guid groupId, Guid organizationId, CancellationToken ct = default);

    /// <summary>Of minstens één inschrijving (ook geannuleerd) een van deze tarieven gekozen heeft.</summary>
    Task<bool> AnyUsingPricesAsync(IEnumerable<Guid> priceIds, CancellationToken ct = default);

    /// <summary>Of minstens één inschrijving (ook geannuleerd) een van deze pakketten gekozen heeft.</summary>
    Task<bool> AnyUsingPackagesAsync(IEnumerable<Guid> packageIds, CancellationToken ct = default);

    /// <summary>Actieve inschrijvingen van een kamp (read-only), voor de limiet per dagdeel.</summary>
    Task<List<CampEnrollment>> GetActiveByCampAsync(Guid campId, CancellationToken ct = default);

    Task AddAsync(CampEnrollment enrollment, CancellationToken ct = default);
    Task AddGroupAsync(CampEnrollmentGroup group, CancellationToken ct = default);
    Task AddFormResponseAsync(CampFormResponse response, CancellationToken ct = default);
}
