using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.TennisClubs.Commands.CreateTennisClub;

public record CreateTennisClubCommand : IRequest<Result<Guid>>
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
