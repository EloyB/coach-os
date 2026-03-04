using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken ct)
    {
        return await _authService.RegisterAsync(
            request.OrganizationName,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            ct);
    }
}
