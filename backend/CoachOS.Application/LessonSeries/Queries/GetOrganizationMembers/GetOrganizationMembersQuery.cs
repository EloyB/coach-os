using CoachOS.Application.Common.Models;
using CoachOS.Application.LessonSeries.DTOs;
using MediatR;

namespace CoachOS.Application.LessonSeries.Queries.GetOrganizationMembers;

public record GetOrganizationMembersQuery : IRequest<Result<List<LessonSeriesMemberDto>>>
{
    public Guid OrganizationId { get; set; }
}
