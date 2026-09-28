using CoachOS.Application.StudentConfirmation.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.StudentConfirmation;

public interface IStudentConfirmationService
{
    Task<Result<AssignmentDetailsDto>> GetByTokenAsync(string rawToken, CancellationToken ct = default);

    Task<Result<ConfirmResultDto>> ConfirmAsync(
        string rawToken, ConfirmRequest request, CancellationToken ct = default);

    Task<Result<List<AvailableSlotDto>>> DeclineAsync(string rawToken, CancellationToken ct = default);

    Task<Result<List<AvailableSlotDto>>> GetAvailableSlotsAsync(
        string rawToken, CancellationToken ct = default);

    Task<Result<ConfirmResultDto>> PickAlternativeAsync(
        string rawToken, PickAlternativeRequest request, CancellationToken ct = default);

    Task<Result<string>> GenerateCalendarAsync(string rawToken, CancellationToken ct = default);

    /// <summary>Markeert enkel deze inschrijving (lid of solo) als betaald.</summary>
    Task<Result> MarkEnrollmentCashPaidAsync(
        Guid enrollmentId, Guid organizationId, CancellationToken ct = default);

    /// <summary>Markeert de hele groep (of solo) als betaald in één keer.</summary>
    Task<Result> MarkGroupCashPaidAsync(
        Guid enrollmentId, Guid organizationId, CancellationToken ct = default);
}
