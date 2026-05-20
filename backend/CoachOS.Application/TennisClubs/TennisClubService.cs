using CoachOS.Application.Mappings;
using CoachOS.Application.TennisClubs.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.TennisClubs;

public class TennisClubService(
    ITennisClubRepository tennisClubRepo,
    ILessonSerieRepository lessonSeriesRepo,
    ApplicationMapper mapper) : ITennisClubService
{
    public async Task<Result<List<TennisClubDto>>> GetAllAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var clubs =
            await tennisClubRepo.GetByOrganizationAsync(organizationId, ct);

        var dtos = clubs
            .Select(mapper.ToTennisClubDto)
            .ToList();

        return Result<List<TennisClubDto>>.Ok(dtos);
    }

    public async Task<Result<Guid>> CreateAsync(
        Guid organizationId, CreateTennisClubRequest request, CancellationToken ct = default)
    {
        var nameTaken = await tennisClubRepo.NameExistsAsync(request.Name, organizationId, null, ct);
        if (nameTaken)
            return Result<Guid>.Fail(new Error(ErrorCodes.Conflict, "Er bestaat al een tennisclub met deze naam."));

        TennisClub club = new()
        {
            OrganizationId = organizationId,
            Name = request.Name,
            Address = request.Address,
        };

        await tennisClubRepo.AddAsync(club, ct);
        await tennisClubRepo.SaveChangesAsync(ct);

        return Result<Guid>.Ok(club.Id);
    }

    public async Task<Result<TennisClubDto>> UpdateAsync(
        Guid id, Guid organizationId, UpdateTennisClubRequest request, CancellationToken ct = default)
    {
        TennisClub? club = await tennisClubRepo.GetByIdAsync(id, organizationId, ct);

        if (club is null)
            return Result<TennisClubDto>.Fail(new Error(ErrorCodes.NotFound, "Tennisclub niet gevonden."));

        if (request.Name is not null)
        {
            var nameTaken = await tennisClubRepo.NameExistsAsync(request.Name, organizationId, id, ct);
            if (nameTaken)
                return Result<TennisClubDto>.Fail(new Error(ErrorCodes.Conflict, "Er bestaat al een tennisclub met deze naam."));

            club.Name = request.Name;
        }

        if (request.Address is not null)
            club.Address = request.Address;

        await tennisClubRepo.SaveChangesAsync(ct);

        return Result<TennisClubDto>.Ok(mapper.ToTennisClubDto(club));
    }

    public async Task<Result> DeleteAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        var club = await tennisClubRepo.GetByIdAsync(id, organizationId, ct);

        if (club is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Tennisclub niet gevonden."));

        var inUse = await lessonSeriesRepo.AnyByTennisClubAsync(id, ct);
        if (inUse)
            return Result.Fail(new Error(ErrorCodes.Conflict, "Deze tennisclub kan niet worden verwijderd omdat er lesreeksen aan gekoppeld zijn."));

        await tennisClubRepo.DeleteAsync(club, ct);
        await tennisClubRepo.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
