using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Enrollments.Submit;

public interface IEnrollmentSubmissionService
{
    /// <summary>Publieke inschrijving voor een lessenreeks. Geeft de id van de inschrijving van de leider.</summary>
    Task<Result<Guid>> SubmitAsync(Guid lessonSeriesId, SubmitEnrollmentRequest request, CancellationToken ct = default);
}
