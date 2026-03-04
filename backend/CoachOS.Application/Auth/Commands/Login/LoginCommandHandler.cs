using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        return await _authService.LoginAsync(request.Email, request.Password, ct);
    }
}
