using System.Data;
using CoachOS.Domain.Entities;

namespace CoachOS.Domain.Interfaces;

public interface ICampEnrollmentRepository
{
    Task<CampEnrollment?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);

    /// <summary>Inclusief Group + Members (voor betaling: deelnemers tellen).</summary>
    Task<CampEnrollment?> GetByIdWithGroupAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deelnemers (rijen) met actieve status (Pending/Confirmed/PendingPayment) voor capaciteit.</summary>
    Task<int> CountActiveByCampAsync(Guid campId, CancellationToken ct = default);

    Task<bool> IsDuplicateAsync(Guid campId, string participantEmail, CancellationToken ct = default);

    Task<int> CountActiveByCampGroupsAsync(Guid campId, Guid organizationId, CancellationToken ct = default);

    /// <summary>Alle inschrijvingen van een kamp incl. FormResponses (admin-overzicht).</summary>
    Task<List<CampEnrollment>> GetByCampWithResponsesAsync(Guid campId, Guid organizationId, CancellationToken ct = default);

    Task AddAsync(CampEnrollment enrollment, CancellationToken ct = default);
    Task AddGroupAsync(CampEnrollmentGroup group, CancellationToken ct = default);
    Task AddFormResponseAsync(CampFormResponse response, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
