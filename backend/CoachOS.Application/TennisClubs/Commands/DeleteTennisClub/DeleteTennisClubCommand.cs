using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.TennisClubs.Commands.DeleteTennisClub;

public record DeleteTennisClubCommand : IRequest<Result>
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
}
