using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.LessonSeries.Commands.DeleteLesson;

public class DeleteLessonCommandHandler : IRequestHandler<DeleteLessonCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteLessonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteLessonCommand request, CancellationToken ct)
    {
        Domain.Entities.Lesson? lesson = await _context.Lessons
            .Include(l => l.Enrollments)
            .FirstOrDefaultAsync(l =>
                l.Id == request.LessonId &&
                l.LessonSeriesId == request.SeriesId &&
                l.OrganizationId == request.OrganizationId, ct);

        if (lesson is null)
            return Result.Failure("Lesmoment niet gevonden.");

        if (lesson.Enrollments.Count > 0)
            return Result.Failure("Verwijderen niet mogelijk: er zijn nog inschrijvingen op dit lesmoment.");

        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
