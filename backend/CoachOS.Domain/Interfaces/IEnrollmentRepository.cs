using System.Data;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Interfaces;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Zoals <see cref="GetByIdAsync"/>, maar laadt de <see cref="EnrollmentGroup"/>
    /// met al zijn leden mee. Nodig om het te betalen bedrag te berekenen: de prijs
    /// van een reeks geldt per deelnemer, dus de groepsgrootte bepaalt het totaal.
    /// </summary>
    Task<Enrollment?> GetByIdWithGroupAsync(
        Guid id, Guid organizationId, CancellationToken ct = default);

    Task<List<Enrollment>> GetBySeriesAsync(
        Guid lessonSeriesId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Staat deze persoon al in de reeks? Identiteit = contactadres + genormaliseerde
    /// naam + geboortedatum, zodat twee kinderen op het adres van hun ouder allebei
    /// mogen inschrijven maar dezelfde persoon niet twee keer.
    /// Zonder geboortedatum is de persoon niet te identificeren; dan `false`, in lijn
    /// met de partiële unique index IX_Enrollments_Participant.
    /// </summary>
    Task<bool> IsDuplicateParticipantAsync(
        Guid lessonSeriesId, string contactEmail, string studentName,
        DateOnly? dateOfBirth, CancellationToken ct = default);

    Task<int> CountActiveBySeriesAsync(Guid lessonSeriesId, CancellationToken ct = default);

    Task<Dictionary<Guid, int>> CountActiveBySeriesIdsAsync(
        IEnumerable<Guid> seriesIds, CancellationToken ct = default);

    Task<int> CountActiveByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Verplaatst alle single-lesson enrollments (LessonId-gekoppeld) van
    /// <paramref name="fromLessonId"/> naar <paramref name="toLessonId"/>. Gebruikt bij
    /// replanning. Series-gekoppelde enrollments (LessonSerieId) worden niet geraakt.
    /// </summary>
    Task<int> ReassignLessonLinkAsync(
        Guid fromLessonId, Guid toLessonId, CancellationToken ct = default);

    Task AddAsync(Enrollment enrollment, CancellationToken ct = default);

    Task AddFormResponseAsync(FormResponse response, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default);

    Task CommitTransactionAsync(CancellationToken ct = default);

    Task RollbackTransactionAsync(CancellationToken ct = default);
}
