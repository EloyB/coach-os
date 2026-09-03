using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Enrollments;

public interface IEnrollmentService
{
    Task<Result<PublicLessonSerieDto>> GetPublicLessonSerieAsync(
        Guid lessonSeriesId, CancellationToken ct = default);

    Task<Result<EnrollmentFormDto?>> GetEnrollmentFormAsync(
        Guid lessonSeriesId, CancellationToken ct = default);

    Task<Result<List<LessonSerieEnrollmentDto>>> GetSeriesEnrollmentsAsync(
        Guid lessonSeriesId, Guid organizationId, CancellationToken ct = default);

    Task<Result<Guid>> SaveFormAsync(
        Guid lessonSeriesId, Guid organizationId, SaveEnrollmentFormRequest request, CancellationToken ct = default);

    Task<Result<Guid>> SubmitEnrollmentAsync(
        Guid lessonSeriesId, SubmitEnrollmentRequest request, CancellationToken ct = default);

    Task<Result<Guid>> CreateManualEnrollmentAsync(
        Guid lessonSeriesId, CreateManualEnrollmentRequest request, Guid organizationId,
        CancellationToken ct = default);

    Task<Result<List<PublicTimeSlotDto>>> GetPublicTimeSlotsAsync(
        Guid lessonSeriesId, CancellationToken ct = default);

    Task<Result<List<EnrollmentWithPreferencesDto>>> GetSeriesEnrollmentsWithPreferencesAsync(
        Guid lessonSeriesId, Guid organizationId, CancellationToken ct = default);

    Task<Result<LessonSerieEnrollmentDto>> UpdateBasicEnrollmentAsync(
        Guid lessonSeriesId, Guid enrollmentId, Guid organizationId,
        UpdateBasicEnrollmentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Annuleert een inschrijving (soft-cancel: status wordt
    /// <see cref="Domain.Enums.EnrollmentStatus.Cancelled"/>). Er wordt bewust niet hard
    /// verwijderd — een inschrijving hangt via DeleteBehavior.Restrict vast aan
    /// formulierantwoorden, groepen en planning-toewijzingen. De capaciteitstellingen
    /// filteren geannuleerde inschrijvingen al weg, dus de plaats komt automatisch vrij.
    /// </summary>
    Task<Result<bool>> CancelEnrollmentAsync(
        Guid lessonSeriesId, Guid enrollmentId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Annuleert een volledige groep atomair: alle nog-actieve leden worden in één
    /// transactie (één SaveChanges) op <see cref="EnrollmentStatus.Cancelled"/> gezet.
    /// Alles-of-niets — voorkomt een half-geannuleerde groep bij een tussentijdse fout.
    /// </summary>
    Task<Result<bool>> CancelGroupAsync(
        Guid lessonSeriesId, Guid groupId, Guid organizationId, CancellationToken ct = default);
}
