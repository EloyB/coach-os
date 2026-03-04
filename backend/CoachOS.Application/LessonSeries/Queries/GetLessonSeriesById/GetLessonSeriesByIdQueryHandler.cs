using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Application.LessonSeries.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.LessonSeries.Queries.GetLessonSeriesById;

public class GetLessonSeriesByIdQueryHandler : IRequestHandler<GetLessonSeriesByIdQuery, Result<LessonSeriesDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserLookupService _userLookup;

    public GetLessonSeriesByIdQueryHandler(IApplicationDbContext context, IUserLookupService userLookup)
    {
        _context = context;
        _userLookup = userLookup;
    }

    public async Task<Result<LessonSeriesDto>> Handle(GetLessonSeriesByIdQuery request, CancellationToken ct)
    {
        Domain.Entities.LessonSeries? series = await _context.LessonSeries
            .AsNoTracking()
            .Include(ls => ls.Lessons)
            .Include(ls => ls.TennisClub)
            .FirstOrDefaultAsync(ls => ls.Id == request.Id && ls.OrganizationId == request.OrganizationId, ct);

        if (series is null)
            return Result<LessonSeriesDto>.Failure("LessonSeries niet gevonden.");

        string trainerName = await _userLookup.GetUserNameByIdAsync(series.TrainerId, ct) ?? string.Empty;

        List<LessonDto> lessons = series.Lessons
            .OrderBy(l => l.Date)
            .ThenBy(l => l.StartTime)
            .Select(l => new LessonDto
            {
                Id = l.Id,
                LessonSeriesId = series.Id,
                Date = l.Date.ToString("yyyy-MM-dd"),
                StartTime = l.StartTime.ToString("HH:mm"),
                EndTime = l.EndTime.ToString("HH:mm"),
                CourtName = l.CourtName,
                MaxStudents = l.MaxStudents,
                Notes = l.Notes,
                IsCancelled = l.IsCancelled,
            }).ToList();

        LessonSeriesDto dto = new()
        {
            Id = series.Id,
            OrganizationId = series.OrganizationId,
            TrainerId = series.TrainerId,
            TrainerName = trainerName,
            Name = series.Name,
            Description = series.Description,
            Level = (int)series.Level,
            Price = series.Price,
            StartDate = series.StartDate.ToString("yyyy-MM-dd"),
            EndDate = series.EndDate.ToString("yyyy-MM-dd"),
            DurationMinutes = series.DurationMinutes,
            IsActive = series.IsActive,
            LessonCount = lessons.Count,
            CreatedAt = series.CreatedAt,
            Lessons = lessons,
            TennisClubId = series.TennisClubId,
            TennisClubName = series.TennisClub?.Name ?? string.Empty,
            TennisClubAddress = series.TennisClub?.Address ?? string.Empty,
        };

        return Result<LessonSeriesDto>.Success(dto);
    }
}
