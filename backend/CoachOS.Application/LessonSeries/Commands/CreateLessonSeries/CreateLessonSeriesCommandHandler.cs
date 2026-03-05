using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.LessonSeries.Commands.CreateLessonSeries;

public class CreateLessonSeriesCommandHandler : IRequestHandler<CreateLessonSeriesCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserLookupService _userLookupService;

    public CreateLessonSeriesCommandHandler(IApplicationDbContext context, IUserLookupService userLookupService)
    {
        _context = context;
        _userLookupService = userLookupService;
    }

    public async Task<Result<Guid>> Handle(CreateLessonSeriesCommand request, CancellationToken ct)
    {
        bool clubExists = await _context.TennisClubs
            .AsNoTracking()
            .AnyAsync(tc => tc.Id == request.TennisClubId && tc.OrganizationId == request.OrganizationId, ct);

        if (!clubExists)
            return Result<Guid>.Failure("Tennisclub niet gevonden.");

        bool trainerValid = await _userLookupService.IsActiveTrainerAsync(request.TrainerId, request.OrganizationId, ct);
        if (!trainerValid)
            return Result<Guid>.Failure("Trainer niet gevonden of niet actief in deze organisatie.");

        DateOnly startDate = DateOnly.ParseExact(request.StartDate, "yyyy-MM-dd");
        DateOnly endDate = DateOnly.ParseExact(request.EndDate, "yyyy-MM-dd");

        Domain.Entities.LessonSeries series = new()
        {
            OrganizationId = request.OrganizationId,
            TrainerId = request.TrainerId,
            Name = request.Name,
            Description = request.Description,
            Level = (LessonLevel)request.Level,
            Price = request.Price,
            StartDate = startDate,
            EndDate = endDate,
            DurationMinutes = request.DurationMinutes,
            TennisClubId = request.TennisClubId,
            IsActive = true,
        };

        _context.LessonSeries.Add(series);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(series.Id);
    }
}
