using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.LessonSeries.Commands.CreateLesson;

public class CreateLessonCommandHandler : IRequestHandler<CreateLessonCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateLessonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateLessonCommand request, CancellationToken ct)
    {
        Domain.Entities.LessonSeries? series = await _context.LessonSeries
            .AsNoTracking()
            .FirstOrDefaultAsync(ls => ls.Id == request.LessonSeriesId && ls.OrganizationId == request.OrganizationId, ct);

        if (series is null)
            return Result<Guid>.Failure("LessonSeries niet gevonden.");

        DateOnly date = DateOnly.ParseExact(request.Date, "yyyy-MM-dd");
        TimeOnly startTime = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        TimeOnly endTime = startTime.AddMinutes(series.DurationMinutes);

        Domain.Entities.Lesson lesson = new()
        {
            OrganizationId = request.OrganizationId,
            LessonSeriesId = series.Id,
            TrainerId = series.TrainerId,
            CourtName = request.CourtName,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            Level = series.Level,
            MaxStudents = 0,
            Notes = request.Notes,
            IsCancelled = false,
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync(ct);

        return Result<Guid>.Success(lesson.Id);
    }
}
