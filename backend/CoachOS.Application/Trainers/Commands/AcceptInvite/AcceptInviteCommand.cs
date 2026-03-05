using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Commands.AcceptInvite;

public record AcceptInviteCommand : IRequest<Result<AuthResponseDto>>
{
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
