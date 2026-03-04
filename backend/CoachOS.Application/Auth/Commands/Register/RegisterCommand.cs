using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Auth.Commands.Register;

public record RegisterCommand : IRequest<Result<AuthResponseDto>>
{
    public string OrganizationName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
