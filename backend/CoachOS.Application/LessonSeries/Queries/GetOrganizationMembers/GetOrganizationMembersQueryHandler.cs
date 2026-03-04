using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Application.LessonSeries.DTOs;
using MediatR;

namespace CoachOS.Application.LessonSeries.Queries.GetOrganizationMembers;

public class GetOrganizationMembersQueryHandler : IRequestHandler<GetOrganizationMembersQuery, Result<List<LessonSeriesMemberDto>>>
{
    private readonly IUserLookupService _userLookup;

    public GetOrganizationMembersQueryHandler(IUserLookupService userLookup)
    {
        _userLookup = userLookup;
    }

    public async Task<Result<List<LessonSeriesMemberDto>>> Handle(GetOrganizationMembersQuery request, CancellationToken ct)
    {
        List<(Guid Id, string FullName)> members = await _userLookup.GetOrganizationMembersAsync(request.OrganizationId, ct);

        List<LessonSeriesMemberDto> dtos = members
            .Select(m => new LessonSeriesMemberDto { Id = m.Id, FullName = m.FullName })
            .ToList();

        return Result<List<LessonSeriesMemberDto>>.Success(dtos);
    }
}
