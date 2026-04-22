using CoachOS.Application.TennisClubs.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.TennisClubs;

public interface ITennisClubService
{
    Task<Result<List<TennisClubDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateTennisClubRequest request, CancellationToken ct = default);
    Task<Result<TennisClubDto>> UpdateAsync(Guid id, Guid organizationId, UpdateTennisClubRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
}
