using CoachOS.Application.AccountEmail.DTOs;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.AccountEmail;

public interface IAccountEmailService
{
    Task<Result> RequestAsync(Guid userId, RequestAccountEmailChange request, CancellationToken ct = default);
    Task<Result<AuthResponseDto>> ConfirmAsync(Guid userId, Guid organizationId, ConfirmAccountEmailChange request, CancellationToken ct = default);
}
