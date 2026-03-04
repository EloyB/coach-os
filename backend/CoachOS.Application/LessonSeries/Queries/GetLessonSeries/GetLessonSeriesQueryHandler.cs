using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Application.LessonSeries.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.LessonSeries.Queries.GetLessonSeries;

public class GetLessonSeriesQueryHandler : IRequestHandler<GetLessonSeriesQuery, Result<List<LessonSeriesDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserLookupService _userLookup;

    public GetLessonSeriesQueryHandler(IApplicationDbContext context, IUserLookupService userLookup)
    {
        _context = context;
        _userLookup = userLookup;
    }

    public async Task<Result<List<LessonSeriesDto>>> Handle(GetLessonSeriesQuery request, CancellationToken ct)
    {
        List<Domain.Entities.LessonSeries> series = await _context.LessonSeries
            .AsNoTracking()
            .Include(ls => ls.TennisClub)
            .Where(ls => ls.OrganizationId == request.OrganizationId)
            .OrderBy(ls => ls.StartDate)
            .ToListAsync(ct);

        if (series.Count == 0)
            return Result<List<LessonSeriesDto>>.Success([]);

        List<Guid> trainerIds = series.Select(ls => ls.TrainerId).Distinct().ToList();
        Dictionary<Guid, string> trainerNames = await _userLookup.GetUserNamesByIdsAsync(trainerIds, ct);

        List<Guid> seriesIds = series.Select(s => s.Id).ToList();
        Dictionary<Guid, int> lessonCounts = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.LessonSeriesId.HasValue && seriesIds.Contains(l.LessonSeriesId!.Value))
            .GroupBy(l => l.LessonSeriesId!.Value)
            .Select(g => new { SeriesId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SeriesId, x => x.Count, ct);

        List<LessonSeriesDto> dtos = series.Select(ls => new LessonSeriesDto
        {
            Id = ls.Id,
            OrganizationId = ls.OrganizationId,
            TrainerId = ls.TrainerId,
            TrainerName = trainerNames.TryGetValue(ls.TrainerId, out string? name) ? name : string.Empty,
            Name = ls.Name,
            Description = ls.Description,
            Level = (int)ls.Level,
            Price = ls.Price,
            StartDate = ls.StartDate.ToString("yyyy-MM-dd"),
            EndDate = ls.EndDate.ToString("yyyy-MM-dd"),
            DurationMinutes = ls.DurationMinutes,
            IsActive = ls.IsActive,
            LessonCount = lessonCounts.TryGetValue(ls.Id, out int count) ? count : 0,
            CreatedAt = ls.CreatedAt,
            TennisClubId = ls.TennisClubId,
            TennisClubName = ls.TennisClub?.Name ?? string.Empty,
            TennisClubAddress = ls.TennisClub?.Address ?? string.Empty,
        }).ToList();

        return Result<List<LessonSeriesDto>>.Success(dtos);
    }
}
