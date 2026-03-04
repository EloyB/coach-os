using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Application.LessonSeries.Commands.DeleteLessonSeries;

public class DeleteLessonSeriesCommandHandler : IRequestHandler<DeleteLessonSeriesCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteLessonSeriesCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteLessonSeriesCommand request, CancellationToken ct)
    {
        Domain.Entities.LessonSeries? series = await _context.LessonSeries
            .Include(ls => ls.Lessons)
            .Include(ls => ls.Enrollments)
            .FirstOrDefaultAsync(ls => ls.Id == request.Id && ls.OrganizationId == request.OrganizationId, ct);

        if (series is null)
            return Result.Failure("LessonSeries niet gevonden.");

        if (series.Enrollments.Count > 0)
            return Result.Failure("Verwijderen niet mogelijk: er zijn nog inschrijvingen op deze serie.");

        _context.Lessons.RemoveRange(series.Lessons);
        _context.LessonSeries.Remove(series);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
