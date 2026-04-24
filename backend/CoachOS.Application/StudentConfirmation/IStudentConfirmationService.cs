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
}
