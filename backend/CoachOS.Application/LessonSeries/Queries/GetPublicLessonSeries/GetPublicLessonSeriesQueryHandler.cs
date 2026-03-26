using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Application.LessonSeries.DTOs;
using CoachOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.LessonSeries.Queries.GetPublicLessonSeries;

public class GetPublicLessonSeriesQueryHandler : IRequestHandler<GetPublicLessonSeriesQuery, Result<PublicLessonSeriesDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserLookupService _userLookup;

    public GetPublicLessonSeriesQueryHandler(IApplicationDbContext context, IUserLookupService userLookup)
    {
        _context = context;
        _userLookup = userLookup;
    }

    public async Task<Result<PublicLessonSeriesDto>> Handle(GetPublicLessonSeriesQuery request, CancellationToken ct)
    {
        Domain.Entities.LessonSeries? series = await _context.LessonSeries
            .AsNoTracking()
            .Include(ls => ls.Lessons)
            .Include(ls => ls.TennisClub)
            .FirstOrDefaultAsync(ls => ls.Id == request.LessonSeriesId && ls.IsActive, ct);

        if (series is null)
            return Result<PublicLessonSeriesDto>.Failure("Lessenreeks niet gevonden.");

        string trainerName = await _userLookup.GetUserNameByIdAsync(series.TrainerId, ct) ?? string.Empty;

        int enrollmentCount = await _context.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.LessonSeriesId == series.Id
                && (e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Pending), ct);

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

        PublicLessonSeriesDto dto = new()
        {
            Id = series.Id,
            Name = series.Name,
            Description = series.Description,
            TrainerName = trainerName,
            Level = (int)series.Level,
            Price = series.Price,
            StartDate = series.StartDate.ToString("yyyy-MM-dd"),
            EndDate = series.EndDate.ToString("yyyy-MM-dd"),
            DurationMinutes = series.DurationMinutes,
            TennisClubName = series.TennisClub?.Name ?? string.Empty,
            EnrollmentCount = enrollmentCount,
            Lessons = lessons,
        };

        return Result<PublicLessonSeriesDto>.Success(dto);
    }
}
