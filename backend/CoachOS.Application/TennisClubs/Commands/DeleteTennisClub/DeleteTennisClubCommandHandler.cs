using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.TennisClubs.Commands.DeleteTennisClub;

public class DeleteTennisClubCommandHandler : IRequestHandler<DeleteTennisClubCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteTennisClubCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteTennisClubCommand request, CancellationToken ct)
    {
        TennisClub? club = await _context.TennisClubs
            .FirstOrDefaultAsync(tc => tc.Id == request.Id && tc.OrganizationId == request.OrganizationId, ct);

        if (club is null)
            return Result.Failure("Tennisclub niet gevonden.");

        bool inUse = await _context.LessonSeries
            .AsNoTracking()
            .AnyAsync(ls => ls.TennisClubId == request.Id, ct);

        if (inUse)
            return Result.Failure("Deze tennisclub kan niet worden verwijderd omdat er lesreeksen aan gekoppeld zijn.");

        _context.TennisClubs.Remove(club);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
