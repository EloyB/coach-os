using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Camps;

public interface ICampEnrollmentService
{
    Task<Result<PublicCampDto>> GetPublicCampAsync(Guid campId, CancellationToken ct = default);
    Task<Result<CampEnrollmentFormDto?>> GetPublicFormAsync(Guid campId, CancellationToken ct = default);
    Task<Result<SubmitCampEnrollmentResultDto>> SubmitAsync(Guid campId, SubmitCampEnrollmentRequest request, CancellationToken ct = default);
}
