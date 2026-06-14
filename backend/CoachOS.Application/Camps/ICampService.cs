using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Camps;

public interface ICampService
{
    Task<Result<List<CampDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<CampDetailDto>> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateCampRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid id, Guid organizationId, UpdateCampRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> SaveFormAsync(Guid campId, Guid organizationId, SaveCampFormRequest request, CancellationToken ct = default);
    Task<Result<CampEnrollmentFormDto?>> GetFormAsync(Guid campId, CancellationToken ct = default);
    Task<Result<List<CampEnrollmentDto>>> GetEnrollmentsAsync(Guid campId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Markeert de cash-betaling van een kamp-inschrijving als betaald (door admin/trainer),
    /// wat de inschrijving bevestigt. Controleert eerst of het kamp bij de organisatie hoort.
    /// </summary>
    Task<Result> MarkEnrollmentCashPaidAsync(
        Guid campId, Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default);
}
