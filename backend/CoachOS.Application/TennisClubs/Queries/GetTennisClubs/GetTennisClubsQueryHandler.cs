using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Application.TennisClubs.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.TennisClubs.Queries.GetTennisClubs;

public class GetTennisClubsQueryHandler : IRequestHandler<GetTennisClubsQuery, Result<List<TennisClubDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetTennisClubsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<TennisClubDto>>> Handle(GetTennisClubsQuery request, CancellationToken ct)
    {
        List<TennisClubDto> dtos = await _context.TennisClubs
            .AsNoTracking()
            .Where(tc => tc.OrganizationId == request.OrganizationId)
            .OrderBy(tc => tc.Name)
            .Select(tc => new TennisClubDto
            {
                Id = tc.Id,
                Name = tc.Name,
                Address = tc.Address,
            })
            .ToListAsync(ct);

        return Result<List<TennisClubDto>>.Success(dtos);
    }
}
