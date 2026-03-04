using CoachOS.Application.Common.Models;
using CoachOS.Application.TennisClubs.DTOs;
using MediatR;

namespace CoachOS.Application.TennisClubs.Queries.GetTennisClubs;

public record GetTennisClubsQuery : IRequest<Result<List<TennisClubDto>>>
{
    public Guid OrganizationId { get; set; }
}
