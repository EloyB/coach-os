using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Commands.InviteTrainer;

public record InviteTrainerCommand : IRequest<Result<Guid>>
{
    public Guid OrganizationId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string InviteBaseUrl { get; set; } = string.Empty;
}
