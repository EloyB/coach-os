using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Models;

namespace CoachOS.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(
        string organizationName,
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponseDto>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
