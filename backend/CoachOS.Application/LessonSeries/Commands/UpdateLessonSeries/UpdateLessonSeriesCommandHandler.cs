using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Application.LessonSeries.DTOs;
using CoachOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.LessonSeries.Commands.UpdateLessonSeries;

public class UpdateLessonSeriesCommandHandler : IRequestHandler<UpdateLessonSeriesCommand, Result<LessonSeriesDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserLookupService _userLookup;

    public UpdateLessonSeriesCommandHandler(IApplicationDbContext context, IUserLookupService userLookup)
    {
        _context = context;
        _userLookup = userLookup;
    }

    public async Task<Result<LessonSeriesDto>> Handle(UpdateLessonSeriesCommand request, CancellationToken ct)
    {
        Domain.Entities.LessonSeries? series = await _context.LessonSeries
            .FirstOrDefaultAsync(ls => ls.Id == request.Id && ls.OrganizationId == request.OrganizationId, ct);

        if (series is null)
            return Result<LessonSeriesDto>.Failure("LessonSeries niet gevonden.");

        bool clubExists = await _context.TennisClubs
            .AsNoTracking()
            .AnyAsync(tc => tc.Id == request.TennisClubId && tc.OrganizationId == request.OrganizationId, ct);

        if (!clubExists)
            return Result<LessonSeriesDto>.Failure("Tennisclub niet gevonden.");

        series.TrainerId = request.TrainerId;
        series.Name = request.Name;
        series.Description = request.Description;
        series.Level = (LessonLevel)request.Level;
        series.Price = request.Price;
        series.IsActive = request.IsActive;
        series.TennisClubId = request.TennisClubId;

        await _context.SaveChangesAsync(ct);

        string trainerName = await _userLookup.GetUserNameByIdAsync(series.TrainerId, ct) ?? string.Empty;

        int lessonCount = await _context.Lessons
            .AsNoTracking()
            .CountAsync(l => l.LessonSeriesId == series.Id, ct);

        Domain.Entities.TennisClub? club = await _context.TennisClubs
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.Id == series.TennisClubId, ct);

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
            LessonCount = lessonCount,
            CreatedAt = series.CreatedAt,
            TennisClubId = series.TennisClubId,
            TennisClubName = club?.Name ?? string.Empty,
            TennisClubAddress = club?.Address ?? string.Empty,
        };

        return Result<LessonSeriesDto>.Success(dto);
    }
}
