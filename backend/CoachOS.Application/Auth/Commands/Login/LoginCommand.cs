using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Auth.Commands.Login;

public record LoginCommand : IRequest<Result<AuthResponseDto>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
