using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Domain.Entities;
using MediatR;

namespace CoachOS.Application.TennisClubs.Commands.CreateTennisClub;

public class CreateTennisClubCommandHandler : IRequestHandler<CreateTennisClubCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateTennisClubCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateTennisClubCommand request, CancellationToken ct)
    {
        TennisClub club = new()
        {
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            Address = request.Address,
        };

        _context.TennisClubs.Add(club);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(club.Id);
    }
}
