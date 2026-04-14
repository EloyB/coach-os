using CoachOS.Application.StudentAuth.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.StudentAuth;

public interface IStudentMagicLinkService
{
    Task<Result> RequestAsync(string email, CancellationToken ct = default);
    Task<Result<StudentAuthResponseDto>> RedeemAsync(string rawToken, CancellationToken ct = default);
}
